using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.ViewComponents;

public class ShoppingCartViewComponent(IUnitOfWork unitOfWork) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        if (claim != null) {
            if (HttpContext.Session.GetInt32(StaticDetails.SessionCart) == null) {
                // Set cart session value
                IEnumerable<ShoppingCart> cartsByUser = unitOfWork.ShoppingCartRepository.GetAll(
                    x => x.ApplicationUserId == claim.Value);

                HttpContext.Session.SetInt32(StaticDetails.SessionCart, cartsByUser.Count());
            }

            return View(HttpContext.Session.GetInt32(StaticDetails.SessionCart));
        }
        else {
            HttpContext.Session.Clear();
            return View(0);
        }
    }
}
