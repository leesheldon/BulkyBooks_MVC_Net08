using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController(IUnitOfWork unitOfWork) : Controller
{
    [BindProperty]
    public OrderVM orderVM { get; set; }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        orderVM = new() {
            OrderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetail = unitOfWork.OrderDetailRepository.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
        };

        return View(orderVM);
    }

    [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
    [HttpPost]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderVM.OrderHeader.Id);

        orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = orderVM.OrderHeader.City;
        orderHeaderFromDb.State = orderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier)) {
            orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber)) {
            orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
        }

        unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
        unitOfWork.Save();
        TempData["success"] = "Order Details updated successfully.";

        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }

    [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
    [HttpPost]
    public IActionResult StartProcessing()
    {
        unitOfWork.OrderHeaderRepository.UpdateStatus(orderVM.OrderHeader.Id, StaticDetails.Status_In_Process);
        unitOfWork.Save();
        TempData["success"] = "Order Details updated successfully.";

        return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
    }

    [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
    [HttpPost]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderVM.OrderHeader.Id);
        orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
        orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
        orderHeaderFromDb.OrderStatus = StaticDetails.Status_Shipped;
        orderHeaderFromDb.ShippingDate = DateTime.Now;

        if (orderHeaderFromDb.PaymentStatus == StaticDetails.Payment_Status_Delayed_Payment) {
            // It is a company user. This will give them the next 30 days to make the payment.
            orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
        unitOfWork.Save();
        TempData["success"] = "Order shipped successfully.";

        return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
    }

    [Authorize(Roles = StaticDetails.Role_Admin + "," + StaticDetails.Role_Employee)]
    [HttpPost]
    public IActionResult CancelOrder()
    {
        var orderHeaderFromDb = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderVM.OrderHeader.Id);

        if (orderHeaderFromDb.PaymentStatus == StaticDetails.Payment_Status_Approved) {
            var options = new RefundCreateOptions {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeaderFromDb.PaymentIntentId
            };

            var service = new RefundService();
            Refund refund = service.Create(options);

            unitOfWork.OrderHeaderRepository.UpdateStatus(
                orderHeaderFromDb.Id, StaticDetails.Status_Cancelled, StaticDetails.Status_Refunded);
        }
        else {
            unitOfWork.OrderHeaderRepository.UpdateStatus(
                orderHeaderFromDb.Id, StaticDetails.Status_Cancelled, StaticDetails.Status_Cancelled);
        }

        unitOfWork.Save();
        TempData["success"] = "Order cancelled successfully.";

        return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
    }

    [HttpPost]
    [ActionName("Details")]
    public IActionResult Details_PAY_NOW()
    {
        orderVM.OrderHeader = unitOfWork.OrderHeaderRepository.Get(
            x => x.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
        orderVM.OrderDetail = unitOfWork.OrderDetailRepository.GetAll(
            x => x.OrderHeaderId == orderVM.OrderHeader.Id, includeProperties: "Product");
        
        // Stripe logic
        var domain = Request.Scheme + "://" + Request.Host.Value + "/";
        var options = new SessionCreateOptions {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={orderVM.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment"
        };

        foreach (var item in orderVM.OrderDetail) {
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
        unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        unitOfWork.Save();

        // Redirect to Stripe to process payment
        Response.Headers.Append("Location", session.Url);            
        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = unitOfWork.OrderHeaderRepository.Get(x => x.Id == orderHeaderId);

        if (orderHeader.PaymentStatus == StaticDetails.Payment_Status_Delayed_Payment) {
            // This is an order made by a company customer
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid") {
                unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(orderHeaderId, 
                    session.Id, session.PaymentIntentId);
                unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, 
                    orderHeader.OrderStatus, StaticDetails.Payment_Status_Approved);
                
                unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API Calls

    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> orderHeaders;

        if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee)) {
            orderHeaders = unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            orderHeaders = unitOfWork.OrderHeaderRepository
                .GetAll(x => x.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
        }

        switch (status) {
            case "pending":
                orderHeaders = orderHeaders.Where(x => 
                    x.PaymentStatus == StaticDetails.Payment_Status_Pending || 
                    x.PaymentStatus == StaticDetails.Payment_Status_Delayed_Payment);
                break;
            case "inprocess":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == StaticDetails.Status_In_Process);
                break;
            case "completed":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == StaticDetails.Status_Shipped);
                break;
            case "approved":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == StaticDetails.Status_Approved);
                break;
            default:
                break;
        }

        return Json(new { data = orderHeaders });
    }

    #endregion
}
