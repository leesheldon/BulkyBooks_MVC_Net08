using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
// [Authorize(Roles = StaticDetails.Role_Admin)]
public class CompanyController(IUnitOfWork unitOfWork) : Controller
{
    public IActionResult Index()
    {
        List<Company> companyList = unitOfWork.CompanyRepository.GetAll().ToList();
        
        return View(companyList);
    }

    public IActionResult CreateOrUpdate(int? id)
    {   
        if (id != null && id > 0)
        {
            // Update
            Company company = unitOfWork.CompanyRepository.Get(x => x.Id == id);
            ViewBag.ViewAction = "Update";

            return View(company);
        }

        ViewBag.ViewAction = "Create";

        return View();
    }

    [HttpPost]
    public IActionResult CreateOrUpdate(Company company)
    {
        var companyFromDb = unitOfWork.CompanyRepository.Get(
            x => x.Id != company.Id && x.Name == company.Name);

        if (companyFromDb != null)
        {
            ModelState.AddModelError("title", "This Company name has been used.");
        }

        if (ModelState.IsValid)
        {
            // Check if Create or Update
            if (company.Id == 0)
            {
                unitOfWork.CompanyRepository.Add(company);
                TempData["success"] = "Company created successfully.";
            }
            else
            {
                unitOfWork.CompanyRepository.Update(company);
                TempData["success"] = "Company updated successfully.";
            }

            unitOfWork.Save();

            return RedirectToAction("Index");
        }
        else
        {
            return View(company);
        }
    }

    #region API Calls

    [HttpGet]
    public IActionResult GetAll()
    {
        List<Company> companyList = unitOfWork.CompanyRepository.GetAll().ToList();
        return Json(new { data = companyList });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyFromDb = unitOfWork.CompanyRepository.Get(x => x.Id == id);
        if (companyFromDb == null)
        {
            return Json(new { success = false, message = "Company to be deleted not found!" });
        }

        unitOfWork.CompanyRepository.Remove(companyFromDb);
        unitOfWork.Save();

        return Json(new { success = true, message = "Delete company successfully!" });
    }

    #endregion
}
