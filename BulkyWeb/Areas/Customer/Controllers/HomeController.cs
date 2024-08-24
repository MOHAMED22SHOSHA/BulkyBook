using Bulky.DataAcces.Repository;
using Bulky.DataAcces.Repository.IRepository;
using Bulky.Model;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
            var productList = _UnitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            return View(productList);
        }

        public IActionResult Details(int id)
        {
            Product Product = _UnitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category");
            if (Product == null)
            {
                return NotFound();
            }
            return View(Product);
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