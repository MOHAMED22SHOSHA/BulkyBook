using Bulky.DataAcces.Repository.IRepository;
using Bulky.DataAccess.Data;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IWebHostEnvironment _WebHostEnvironment;

        public ProductController(IUnitOfWork UnitOfWork, IWebHostEnvironment WebHostEnvironment)
        {
            _UnitOfWork = UnitOfWork;
            _WebHostEnvironment = WebHostEnvironment;
        }

        public IActionResult Index()
        {
            var obj = _UnitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            return View(obj);
        }

        [HttpGet]
        public IActionResult Upsert(int? id) // Update or Insert
        {
            ProductVM productVM = new ProductVM
            {
                CategoryList = _UnitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                product = new Product()
            };

            if (id == null || id == 0)
            {
                // Create
                return View(productVM);
            }
            else
            {
                // Update
                productVM.product = _UnitOfWork.Product.Get(p => p.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = _WebHostEnvironment.WebRootPath;
                    if (file != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string ProductPath = Path.Combine(wwwRootPath, @"Image\Product");

                        if (!string.IsNullOrEmpty(obj.product.ImageUrl))
                        {
                            var oldImagepath = Path.Combine(wwwRootPath, obj.product.ImageUrl.TrimStart('\\'));

                            if (System.IO.File.Exists(oldImagepath))
                            {
                                System.IO.File.Delete(oldImagepath);
                            }
                        }

                        using (var fileStream = new FileStream(Path.Combine(ProductPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        obj.product.ImageUrl = @"\Image\Product\" + fileName;
                    }

                    if (obj.product.Id == 0)
                    {
                        _UnitOfWork.Product.Add(obj.product);
                        _UnitOfWork.Save();
                        TempData["success"] = "Product created Successfully";
                    }
                    else
                    {
                        _UnitOfWork.Product.Update(obj.product);
                        _UnitOfWork.Save();
                        TempData["success"] = "Product updated Successfully";
                    }

                    _UnitOfWork.Save();
                    
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    
                    ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
                }
            }

            return View();
        }

        //[HttpGet]
        //public IActionResult Delete(int? id)
        //{
        //    var product = _UnitOfWork.Product.Get(p => p.Id == id);
        //    if (id == null || id == 0 || product == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(product);
        //}

        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePost(int? id)
        //{
        //    var product = _UnitOfWork.Product.Get(p => p.Id == id);
        //    if (id == null || id == 0 || product == null)
        //    {
        //        return NotFound();
        //    }

        //    _UnitOfWork.Product.Remove(product);
        //    _UnitOfWork.Save();
        //    TempData["success"] = "Product deleted Successfully";
        //    return RedirectToAction("Index");
        //}


        #region API calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var obj = _UnitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data =  obj });
        }

		[HttpDelete]
		public IActionResult Delete(int? id)
		{
			var productToBeDeleted = _UnitOfWork.Product.Get(u => u.Id == id);
			if (productToBeDeleted == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			var oldImagePath = Path.Combine(_WebHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));

			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}


			_UnitOfWork.Product.Remove(productToBeDeleted);
			_UnitOfWork.Save();

			return Json(new { success = true, message = "Delete Successful" });
		}

		#endregion
	}
}
