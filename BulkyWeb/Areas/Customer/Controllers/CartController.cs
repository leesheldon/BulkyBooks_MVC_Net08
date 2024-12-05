using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController(IUnitOfWork unitOfWork) : Controller
{
    [BindProperty]
    public ShoppingCartVM cartVM { get; set; }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        cartVM = new() {
            ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(x => 
                                x.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new()
        };

        foreach(var cart in cartVM.ShoppingCartList) {
            cart.Product.ProductImages = unitOfWork.ProductImageRepository.GetAll(x => x.ProductId == cart.ProductId).ToList();
            cart.Price = GetPriceBasedOnQuantity(cart);
            cartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(cartVM);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        cartVM = new() {
            ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(x => 
                x.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new()
        };

        cartVM.OrderHeader.ApplicationUser = unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId);

        cartVM.OrderHeader.Name = cartVM.OrderHeader.ApplicationUser.Name;
        cartVM.OrderHeader.PhoneNumber = cartVM.OrderHeader.ApplicationUser.PhoneNumber;
        cartVM.OrderHeader.StreetAddress = cartVM.OrderHeader.ApplicationUser.StreetAddress;
        cartVM.OrderHeader.City = cartVM.OrderHeader.ApplicationUser.City;
        cartVM.OrderHeader.State = cartVM.OrderHeader.ApplicationUser.State;
        cartVM.OrderHeader.PostalCode = cartVM.OrderHeader.ApplicationUser.PostalCode;

        // Calculate Order Total
        foreach(var cart in cartVM.ShoppingCartList) {
            cart.Price = GetPriceBasedOnQuantity(cart);
            cartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(cartVM);
    }

    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPOST()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        cartVM.ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(x => 
                x.ApplicationUserId == userId, includeProperties: "Product");
        
        cartVM.OrderHeader.OrderDate = DateTime.Now;
        cartVM.OrderHeader.ApplicationUserId = userId;
        
        ApplicationUser appUser = unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId);

        // Calculate Order Total
        foreach(var cart in cartVM.ShoppingCartList) {
            cart.Price = GetPriceBasedOnQuantity(cart);
            cartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        if (appUser.CompanyId.GetValueOrDefault() == 0) {
            // It is a regular customer account
            cartVM.OrderHeader.PaymentStatus = StaticDetails.Payment_Status_Pending;
            cartVM.OrderHeader.OrderStatus = StaticDetails.Status_Pending;
        }
        else {
            // It is a company user
            cartVM.OrderHeader.PaymentStatus = StaticDetails.Payment_Status_Delayed_Payment;
            cartVM.OrderHeader.OrderStatus = StaticDetails.Status_Approved;
        }

        // Create a new Order Header into Database
        unitOfWork.OrderHeaderRepository.Add(cartVM.OrderHeader);
        unitOfWork.Save();

        // Create new Order Details into Database
        foreach (var cart in cartVM.ShoppingCartList) {
            OrderDetail orderDetail = new() {
                ProductId = cart.ProductId,
                OrderHeaderId = cartVM.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };

            unitOfWork.OrderDetailRepository.Add(orderDetail);
            unitOfWork.Save();
        }

        if (appUser.CompanyId.GetValueOrDefault() == 0) {
            // It is a regular customer account and we need to capture payment
            // Stripe logic
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={cartVM.OrderHeader.Id}",
                CancelUrl = domain + "customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment"
            };

            foreach (var item in cartVM.ShoppingCartList) {
                var sessionLineItem = new SessionLineItemOptions {
                    PriceData = new SessionLineItemPriceDataOptions {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            // Update Stripe payment Id
            unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(cartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            unitOfWork.Save();

            // Redirect to Stripe to process payment
            Response.Headers.Append("Location", session.Url);            
            return new StatusCodeResult(303);
        }

        return RedirectToAction(nameof(OrderConfirmation), new { id = cartVM.OrderHeader.Id });
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = unitOfWork.OrderHeaderRepository.Get(
            x => x.Id == id, includeProperties: "ApplicationUser");

        if (orderHeader.PaymentStatus != StaticDetails.Payment_Status_Delayed_Payment) {
            // This is an order made by a regular customer
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid") {
                unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                unitOfWork.OrderHeaderRepository.UpdateStatus(id, StaticDetails.Status_Approved, StaticDetails.Payment_Status_Approved);
                unitOfWork.Save();
            }

            HttpContext.Session.Clear();
        }

        List<ShoppingCart> cartList = unitOfWork.ShoppingCartRepository.GetAll(
            x => x.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        
        unitOfWork.ShoppingCartRepository.RemoveRange(cartList);
        unitOfWork.Save();

        return View(id);
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId);
        cartFromDb.Count += 1;
        unitOfWork.ShoppingCartRepository.Update(cartFromDb);
        unitOfWork.Save();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId, tracked: true);
        if (cartFromDb.Count <= 1) {
            // Update cart session value
            IEnumerable<ShoppingCart> cartsByUser = unitOfWork.ShoppingCartRepository.GetAll(
                x => x.ApplicationUserId == cartFromDb.ApplicationUserId);

            HttpContext.Session.SetInt32(StaticDetails.SessionCart, cartsByUser.Count() - 1);
            
            // Remove this item from cart
            unitOfWork.ShoppingCartRepository.Remove(cartFromDb);
        }
        else {
            cartFromDb.Count -= 1;
            unitOfWork.ShoppingCartRepository.Update(cartFromDb);
        }

        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId, tracked: true);
        
        // Update cart session value
        IEnumerable<ShoppingCart> cartsByUser = unitOfWork.ShoppingCartRepository.GetAll(
            x => x.ApplicationUserId == cartFromDb.ApplicationUserId);

        HttpContext.Session.SetInt32(StaticDetails.SessionCart, cartsByUser.Count() - 1);

        // Remove this item from cart
        unitOfWork.ShoppingCartRepository.Remove(cartFromDb);
        unitOfWork.Save();

        return RedirectToAction(nameof(Index));
    }

    private double GetPriceBasedOnQuantity(ShoppingCart cart)
    {
        if (cart.Count <= 50) {
            return cart.Product.Price;
        }
        else {
            if (cart.Count <= 100) {
                return cart.Product.Price50;
            }
            else {
                return cart.Product.Price100;
            }
        }
    }
}
