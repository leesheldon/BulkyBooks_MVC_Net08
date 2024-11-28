using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController(IUnitOfWork unitOfWork) : Controller
{
    public ShoppingCartVM shoppingCartVM { get; set; }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        shoppingCartVM = new() {
            ShoppingCartList = unitOfWork.ShoppingCartRepository.GetAll(x => 
                x.ApplicationUserId == userId, includeProperties: "Product")
        };

        foreach(var cart in shoppingCartVM.ShoppingCartList) {
            cart.Price = GetPriceBasedOnQuantity(cart);
            shoppingCartVM.OrderTotal += (cart.Price * cart.Count);
        }

        return View(shoppingCartVM);
    }

    public IActionResult Summary()
    {
        return View();
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
        var cartFromDb = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId);
        if (cartFromDb.Count <= 1) {
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
        var cartFromDb = unitOfWork.ShoppingCartRepository.Get(x => x.Id == cartId);
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
