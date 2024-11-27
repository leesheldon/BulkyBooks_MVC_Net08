using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
// [Authorize(Roles = StaticDetails.Role_Admin)]
public class ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment) : Controller
{
    public IActionResult Index()
    {
        List<Product> productList = unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
        
        return View(productList);
    }

    public IActionResult CreateOrUpdate(int? id)
    {   
        ViewBag.ViewAction = "Create";

        ProductVM productVM = new ()
        {
            CategoryList = unitOfWork.CategoryRepository.GetAll().Select(x => new SelectListItem 
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
        };

        if (id != null && id > 0)
        {
            // Update
            productVM.Product = unitOfWork.ProductRepository.Get(x => x.Id == id);
            ViewBag.ViewAction = "Update";
        }

        return View(productVM);
    }

    [HttpPost]
    public IActionResult CreateOrUpdate(ProductVM productVM, IFormFile? file)
    {
        var productFromDb = unitOfWork.ProductRepository.Get(
            x => x.Id != productVM.Product.Id && x.Title == productVM.Product.Title);

        if (productFromDb != null)
        {
            ModelState.AddModelError("title", "This Title has been used.");
        }

        if (ModelState.IsValid)
        {
            // Upload image file
            string rootPath = webHostEnvironment.WebRootPath;
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string productPath = Path.Combine(rootPath, @"images\product");

                if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                {
                    // Delete old image
                    var oldImagePath = Path.Combine(rootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVM.Product.ImageUrl = @"\images\product\" + fileName;
            }

            // Check if Create or Update
            if (productVM.Product.Id == 0)
            {
                unitOfWork.ProductRepository.Add(productVM.Product);
                TempData["success"] = "Product created successfully.";
            }
            else
            {
                unitOfWork.ProductRepository.Update(productVM.Product);
                TempData["success"] = "Product updated successfully.";
            }

            unitOfWork.Save();

            return RedirectToAction("Index");
        }
        else
        {
            productVM.CategoryList = unitOfWork.CategoryRepository
                .GetAll()
                .Select(x => new SelectListItem {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });
            
            return View(productVM);
        }
    }

    #region API Calls

    [HttpGet]
    public IActionResult GetAll()
    {
        List<Product> productList = unitOfWork.ProductRepository.GetAll(includeProperties: "Category").ToList();
        return Json(new { data = productList });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var productFromDb = unitOfWork.ProductRepository.Get(x => x.Id == id);
        if (productFromDb == null)
        {
            return Json(new { success = false, message = "Product to be deleted not found!" });
        }

        // Delete old image
        var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, productFromDb.ImageUrl.TrimStart('\\'));

        if (System.IO.File.Exists(oldImagePath))
        {
            System.IO.File.Delete(oldImagePath);
        }

        unitOfWork.ProductRepository.Remove(productFromDb);
        unitOfWork.Save();

        return Json(new { success = true, message = "Delete product successfully!" });
    }

    #endregion
}
