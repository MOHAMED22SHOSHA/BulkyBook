using Bulky.DataAcces.Repository.IRepository;
using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;

namespace Bulky.DataAcces.Repository
{
	public class ProductRepository : Repository<Product>, IProductRepository
	{
		private AppDbContext _db;
		public ProductRepository(AppDbContext db) : base(db)
		{
			_db = db;
		}
		public void Update(Product obj)
		{
			var objFromDb=_db.products.FirstOrDefault(p=>p.Id==obj.Id);
			if (objFromDb != null)
			{
                objFromDb.Title = obj.Title;
				objFromDb.Description = obj.Description;
				objFromDb.CategoryId = obj.CategoryId;
                objFromDb.ISBN= obj.ISBN;
                objFromDb.ListPrice = obj.ListPrice;
                objFromDb.Price = obj.Price;
                objFromDb.Price100 = obj.Price100;
                objFromDb.Price50= obj.Price50;
                objFromDb.Author = obj.Author;
				if (obj.ImageUrl != null)
				{
					objFromDb.ImageUrl = obj.ImageUrl;
				}

            }
		}
	}
}
