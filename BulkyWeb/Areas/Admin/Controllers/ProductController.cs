using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = StaticDetails.Role_Admin)]
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
            productVM.Product = unitOfWork.ProductRepository.Get(x => x.Id == id, includeProperties: "ProductImages");
            ViewBag.ViewAction = "Update";
        }

        return View(productVM);
    }

    [HttpPost]
    public IActionResult CreateOrUpdate(ProductVM productVM, List<IFormFile> files)
    {
        var productFromDb = unitOfWork.ProductRepository.Get(
            x => x.Id != productVM.Product.Id && x.Title == productVM.Product.Title);

        if (productFromDb != null)
        {
            ModelState.AddModelError("title", "This Title has been used.");
        }

        if (!ModelState.IsValid)
        {
            productVM.CategoryList = unitOfWork.CategoryRepository
                .GetAll()
                .Select(x => new SelectListItem {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });
            
            return View(productVM);
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

        // Upload image files
        string rootPath = webHostEnvironment.WebRootPath;
        if (files != null)
        {
            foreach (IFormFile file in files)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string productPath = @"images\products\product-" + productVM.Product.Id;
                string finalPath = Path.Combine(rootPath, productPath);

                if (!Directory.Exists(finalPath))
                    Directory.CreateDirectory(finalPath);
                
                using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create)) {
                    file.CopyTo(fileStream);
                }

                ProductImage productImage = new() {
                    ImageUrl = @"\" + productPath + @"\" + fileName,
                    ProductId = productVM.Product.Id
                };

                if (productVM.Product.ProductImages == null)
                    productVM.Product.ProductImages = new List<ProductImage>();
                
                productVM.Product.ProductImages.Add(productImage);
            }

            unitOfWork.ProductRepository.Update(productVM.Product);
            unitOfWork.Save();
        }

        return RedirectToAction("Index");
    }

    public IActionResult DeleteImage(int imageId)
    {
        var imageToBeDeleted = unitOfWork.ProductImageRepository.Get(x => x.Id == imageId);
        if (imageToBeDeleted == null)
        {
            TempData["error"] = "This image to be deleted not found.";
            return RedirectToAction("Index");
        }

        int productId = imageToBeDeleted.ProductId;
        
        if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl)) {
            // Delete old image
            var oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath)) {
                System.IO.File.Delete(oldImagePath);
            }
                
        }

        unitOfWork.ProductImageRepository.Remove(imageToBeDeleted);
        unitOfWork.Save();

        TempData["success"] = "Delete product image successfully.";

        return RedirectToAction(nameof(CreateOrUpdate), new { id = productId });
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

        string productPath = @"images\products\product-" + id;
        string finalPath = Path.Combine(webHostEnvironment.WebRootPath, productPath);

        if (Directory.Exists(finalPath)) {
            string[] filePaths = Directory.GetFiles(finalPath);
            foreach (string filePath in filePaths)
            {
                System.IO.File.Delete(filePath);
            }

            Directory.Delete(finalPath);
        }

        unitOfWork.ProductRepository.Remove(productFromDb);
        unitOfWork.Save();

        return Json(new { success = true, message = "Delete product successfully!" });
    }

    #endregion
}
