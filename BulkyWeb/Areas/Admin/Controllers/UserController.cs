using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = StaticDetails.Role_Admin)]
public class UserController(DataContext context, UserManager<ApplicationUser> userManager) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string userId)
    {
        var userRole = context.UserRoles.FirstOrDefault(x => x.UserId == userId);
        if (userRole == null) {
            TempData["error"] = "User-Role record not found.";
            return RedirectToAction("Index");
        }

        string roleId = userRole.RoleId;
        var appUser = context.ApplicationUsers.Include(x => x.Company).FirstOrDefault(y => y.Id == userId);
        if (appUser == null) {
            TempData["error"] = "User not found.";
            return RedirectToAction("Index");
        }

        RoleManagementVM roleVM = new RoleManagementVM() {
            ApplicationUser = appUser,
            RoleList = context.Roles.Select(r => new SelectListItem {
                Text = r.Name,
                Value = r.Name
            }),
            CompanyList = context.Companies.Select(c => new SelectListItem {
                Text = c.Name,
                Value = c.Id.ToString()
            })
        };

        var role = context.Roles.FirstOrDefault(x => x.Id == roleId);
        if (role == null || role.Name == null) {
            TempData["error"] = "Role not found.";
            return RedirectToAction("Index");
        }
        else {
            roleVM.ApplicationUser.Role = role.Name;
        }

        return View(roleVM);
    }

    [HttpPost]
    public async Task<IActionResult> RoleManagement(RoleManagementVM roleVM)
    {
        var role = context.UserRoles.FirstOrDefault(x => x.UserId == roleVM.ApplicationUser.Id);
        if (role == null) {
            TempData["error"] = "User-Role record of this user not found.";
            return RedirectToAction("Index");
        }

        string roleId = role.RoleId;

        var oldRole = context.Roles.FirstOrDefault(x => x.Id == roleId);
        if (oldRole == null || oldRole.Name == null) {
            TempData["error"] = "The old role of this user not found.";
            return RedirectToAction("Index");
        }

        string oldRoleName = oldRole.Name;

        if (!(roleVM.ApplicationUser.Role == oldRoleName)) {
            // This user's role was updated.
            var userFromDb = context.ApplicationUsers.FirstOrDefault(x => x.Id == roleVM.ApplicationUser.Id);
            if (userFromDb == null) {
                TempData["error"] = "This user record in Database not found.";
                return RedirectToAction("Index");
            }

            if (roleVM.ApplicationUser.Role == StaticDetails.Role_Company) {
                userFromDb.CompanyId = roleVM.ApplicationUser.CompanyId;
            }

            if (oldRoleName == StaticDetails.Role_Company) {
                userFromDb.CompanyId = null;
            }

            context.SaveChanges();
            
            await userManager.RemoveFromRoleAsync(userFromDb, oldRoleName);
            await userManager.AddToRoleAsync(userFromDb, roleVM.ApplicationUser.Role);
        }

        return RedirectToAction("Index");
    }

    #region API Calls

    [HttpGet]
    public IActionResult GetAll()
    {
        List<ApplicationUser> userList = context.ApplicationUsers.Include(x => x.Company).ToList();
        var userRoles = context.UserRoles.ToList();
        var roles = context.Roles.ToList();

        foreach (var user in userList)
        {
            var roleId = userRoles.FirstOrDefault(x => x.UserId == user.Id).RoleId;
            user.Role = roles.FirstOrDefault(x => x.Id == roleId).Name;

            if (user.Company == null) {
                user.Company = new Company() { Name = "" };
            }
        }

        return Json(new { data = userList });
    }

    [HttpPost]
    public IActionResult LockOrUnlock([FromBody]string id)
    {
        var userFromDb = context.ApplicationUsers.FirstOrDefault(x => x.Id == id);
        if (userFromDb == null) {
            return Json(new { success = false, message = "Error while locking/Unlocking user!" });
        }

        var result_message = "";
        if (userFromDb.LockoutEnd != null && userFromDb.LockoutEnd > DateTime.Now) {
            // User is currently locked and we need to unlock them.
            userFromDb.LockoutEnd = DateTime.Now;
            result_message = "Unlock user successfully!";
        }
        else {
            userFromDb.LockoutEnd = DateTime.Now.AddYears(100);
            result_message = "Lock user successfully!";
        }

        context.SaveChanges();

        return Json(new { success = true, message = result_message });
    }

    #endregion
}
