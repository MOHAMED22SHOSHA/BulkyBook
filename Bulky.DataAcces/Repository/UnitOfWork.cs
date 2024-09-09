using Bulky.DataAcces.Repository.IRepository;
using Bulky.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAcces.Repository
{
	public class UnitOfWork : IUnitOfWork
	{
		private AppDbContext _db;
		public ICategoryRepository Category { get; private set; }
		public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }
		public IShoppingCartRepository ShoppingCart { get; private set; }
        public IApplicationUserRepository ApplicationUser { get; private set; }

		public IOrderDetailsRepository OrderDetails { get; private set; }

		public IOrderHeaderRepository OrderHeeaader { get; private set; }
		public IProductImageRepository ProductImage { get; private set; }
        public UnitOfWork(AppDbContext db)
		{
			_db = db;
			Category = new CategoryRepository(_db);
			Product = new ProductRepository(_db);
			Company = new CompanyRepository(_db);
            ShoppingCart =new ShoppingCartRepository(_db);
            ApplicationUser= new ApplicationUserRepository(_db);
			OrderDetails = new OrderDetailsRepository(_db);
            OrderHeeaader = new OrderHeaderRepository(_db);
			ProductImage = new ProductImageRepository(_db);
        }
		public void Save()
		{
			_db.SaveChanges();
		}
	}
}
