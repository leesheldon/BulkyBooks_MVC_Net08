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
public class UserController(IUnitOfWork unitOfWork, 
    UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string userId)
    {
        var appUser = unitOfWork.ApplicationUserRepository.Get(x => x.Id == userId, includeProperties: "Company");
        if (appUser == null) {
            TempData["error"] = "User not found.";
            return RedirectToAction("Index");
        }

        RoleManagementVM roleVM = new RoleManagementVM() {
            ApplicationUser = appUser,
            RoleList = roleManager.Roles.Select(r => new SelectListItem {
                Text = r.Name,
                Value = r.Name
            }),
            CompanyList = unitOfWork.CompanyRepository.GetAll().Select(c => new SelectListItem {
                Text = c.Name,
                Value = c.Id.ToString()
            })
        };

        var roleOfUser = userManager.GetRolesAsync(appUser).GetAwaiter().GetResult().FirstOrDefault();

        if (string.IsNullOrEmpty(roleOfUser)) {
            TempData["error"] = "Role of this user not found.";
            return RedirectToAction("Index");
        }
        else {
            roleVM.ApplicationUser.Role = roleOfUser;
        }

        return View(roleVM);
    }

    [HttpPost]
    public async Task<IActionResult> RoleManagement(RoleManagementVM roleVM)
    {
        var userFromDb = unitOfWork.ApplicationUserRepository.Get(x => x.Id == roleVM.ApplicationUser.Id);
        if (userFromDb == null) {
            TempData["error"] = "This user record in Database not found.";
            return RedirectToAction("Index");
        }

        var oldRole = userManager.GetRolesAsync(userFromDb).GetAwaiter().GetResult().FirstOrDefault();
        if (string.IsNullOrEmpty(oldRole)) {
            TempData["error"] = "The old role of this user not found.";
            return RedirectToAction("Index");
        }

        if (roleVM.ApplicationUser.Role != oldRole) {
            // This user's role was updated.
            if (roleVM.ApplicationUser.Role == StaticDetails.Role_Company) {
                userFromDb.CompanyId = roleVM.ApplicationUser.CompanyId;
            }

            if (oldRole == StaticDetails.Role_Company) {
                userFromDb.CompanyId = null;
            }

            unitOfWork.ApplicationUserRepository.Update(userFromDb);
            unitOfWork.Save();
            
            await userManager.RemoveFromRoleAsync(userFromDb, oldRole);
            await userManager.AddToRoleAsync(userFromDb, roleVM.ApplicationUser.Role);
        }
        else {
            if (oldRole == StaticDetails.Role_Company && userFromDb.CompanyId != roleVM.ApplicationUser.CompanyId) {
                userFromDb.CompanyId = roleVM.ApplicationUser.CompanyId;
                unitOfWork.ApplicationUserRepository.Update(userFromDb);
                unitOfWork.Save();
            }
        }

        return RedirectToAction("Index");
    }

    #region API Calls

    [HttpGet]
    public IActionResult GetAll()
    {
        List<ApplicationUser> userList = unitOfWork.ApplicationUserRepository.GetAll(includeProperties: "Company").ToList();

        foreach (var user in userList)
        {
            var role = userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

            if (string.IsNullOrEmpty(role)) {
                return Json(new { data = new List<ApplicationUser>(), success = false, message = "Role of user (" + user.Name + ") not found." });
            }

            user.Role = role;

            if (user.Company == null) {
                user.Company = new Company() { Name = "" };
            }
        }

        return Json(new { data = userList });
    }

    [HttpPost]
    public IActionResult LockOrUnlock([FromBody]string id)
    {
        var userFromDb = unitOfWork.ApplicationUserRepository.Get(x => x.Id == id);
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

        unitOfWork.ApplicationUserRepository.Update(userFromDb);
        unitOfWork.Save();

        return Json(new { success = true, message = result_message });
    }

    #endregion
}
