using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Bulky.Utility;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> productList = _unitOfWork.ProductRepository
            .GetAll(includeProperties: "Category,ProductImages");

        return View(productList);
    }

    public IActionResult Details(int productId)
    {
        ShoppingCart cart = new() {
            Product = _unitOfWork.ProductRepository
                .Get(x => x.Id == productId, includeProperties: "Category,ProductImages"),
            Count = 1,
            ProductId = productId
        };

        return View(cart);
    }

    [Authorize]
    [HttpPost]
    public IActionResult Details(ShoppingCart cart)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        cart.ApplicationUserId = userId;
        ShoppingCart cartFromDb = _unitOfWork.ShoppingCartRepository.Get(x => 
            x.ApplicationUserId == userId && x.ProductId == cart.ProductId);
        
        if (cartFromDb != null) {
            // This cart existed
            cartFromDb.Count += cart.Count;
            _unitOfWork.ShoppingCartRepository.Update(cartFromDb);
            _unitOfWork.Save();
        }
        else {
            // Add cart record
            _unitOfWork.ShoppingCartRepository.Add(cart);
            _unitOfWork.Save();

            // Set cart session value
            IEnumerable<ShoppingCart> cartsByUser = _unitOfWork.ShoppingCartRepository.GetAll(
                x => x.ApplicationUserId == userId);

            HttpContext.Session.SetInt32(StaticDetails.SessionCart, cartsByUser.Count());
        }
        
        TempData["success"] = "Cart updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
