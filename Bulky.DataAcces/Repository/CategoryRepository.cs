using Bulky.DataAcces.Repository.IRepository;
using Bulky.DataAccess.Data;
using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAcces.Repository
{
	public class CategoryRepository : Repository<Category>, ICategoryRepository
	{
		private AppDbContext _db;
		public CategoryRepository(AppDbContext db) : base(db)
		{
			_db = db;
		}



		public void Update(Category obj)
		{
			_db.categories.Update(obj);
		}
	}
}
