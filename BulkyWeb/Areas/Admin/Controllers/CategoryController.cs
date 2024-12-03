using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = StaticDetails.Role_Admin)]
public class CategoryController(IUnitOfWork unitOfWork) : Controller
{
    public IActionResult Index()
    {
        List<Category> categoryList = unitOfWork.CategoryRepository.GetAll().ToList();
        return View(categoryList);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category newCategory)
    {
        var categoryFromDb = unitOfWork.CategoryRepository.Get(x => x.Name == newCategory.Name);

        if (categoryFromDb != null)
        {
            ModelState.AddModelError("name", "This Category Name has been added.");
        }

        var displayOrder_FromDb = unitOfWork.CategoryRepository.Get(x => x.DisplayOrder == newCategory.DisplayOrder);

        if (displayOrder_FromDb != null)
        {
            ModelState.AddModelError("displayorder", "This Display Order has been added.");
        }

        if (!ModelState.IsValid) return View();

        unitOfWork.CategoryRepository.Add(newCategory);
        unitOfWork.Save();
        TempData["success"] = "Category created successfully.";

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int? id)
    {
        if (id == null || id == 0)
        {
            TempData["error"] = "Category not found.";
            return RedirectToAction("Index");
        }
        
        var categoryFromDb = unitOfWork.CategoryRepository.Get(x => x.Id == id);
        if (categoryFromDb == null)
        {
            TempData["error"] = "Category not found.";
            return RedirectToAction("Index");
        }

        return View(categoryFromDb);
    }

    [HttpPost]
    public IActionResult Edit(Category updateCategory)
    {
        var categoryFromDb = unitOfWork.CategoryRepository.Get(x => x.Id != updateCategory.Id && x.Name == updateCategory.Name);

        if (categoryFromDb != null)
        {
            ModelState.AddModelError("name", "This Category Name has been used.");
        }

        var displayOrder_FromDb = unitOfWork.CategoryRepository.Get(x => x.Id != updateCategory.Id && x.DisplayOrder == updateCategory.DisplayOrder);

        if (displayOrder_FromDb != null)
        {
            ModelState.AddModelError("displayorder", "This Display Order has been used.");
        }

        if (!ModelState.IsValid) return View();

        unitOfWork.CategoryRepository.Update(updateCategory);
        unitOfWork.Save();
        TempData["success"] = "Category updated successfully.";

        return RedirectToAction("Index");
    }

    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            TempData["error"] = "Category not found.";
            return RedirectToAction("Index");
        }
        
        var categoryFromDb = unitOfWork.CategoryRepository.Get(x => x.Id == id);
        if (categoryFromDb == null)
        {
            TempData["error"] = "Category not found.";
            return RedirectToAction("Index");
        }

        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePost(int? id)
    {
        var categoryFromDb = unitOfWork.CategoryRepository.Get(x => x.Id == id);

        if (categoryFromDb == null)
        {
            TempData["error"] = "Category not found.";
            return RedirectToAction("Index");
        }

        unitOfWork.CategoryRepository.Remove(categoryFromDb);
        unitOfWork.Save();
        TempData["success"] = "Category deleted successfully.";

        return RedirectToAction("Index");
    }
}
