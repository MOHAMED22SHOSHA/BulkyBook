using Bulky.DataAcces.Repository;
using Bulky.DataAcces.Repository.IRepository;
using Bulky.Model;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly IUnitOfWork _UnitOfWork;

		public HomeController(ILogger<HomeController> logger, IUnitOfWork UnitOfWork)
        {
            _logger = logger;
			_UnitOfWork = UnitOfWork;
		}

        public IActionResult Index()
        {
            
            var productList = _UnitOfWork.Product.GetAll(includeProperties:"Category,ProductImages").ToList();
            return View(productList);
        }

        public IActionResult Details(int id)
        {

            ShoppingCart cart = new()
            {
                Product = _UnitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category,ProductImages"),
                Count=1,
                ProductId=id
            };
            if (cart == null)
            {
                return NotFound();
            }
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart obj)
        {
            
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            obj.ApplicationUserId = userId;
            obj.Id = 0;
            ShoppingCart cartDb = _UnitOfWork.ShoppingCart.Get(c => c.ApplicationUserId == userId && c.ProductId == obj.ProductId);
            if (cartDb == null)
            {
                _UnitOfWork.ShoppingCart.Add(obj);
                _UnitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _UnitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count());
            }
            else
            {
                cartDb.Count += obj.Count;
                _UnitOfWork.ShoppingCart.Update(cartDb);
                _UnitOfWork.Save();
            }

            TempData["success"] = "Product added to cart successfully!";
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
}