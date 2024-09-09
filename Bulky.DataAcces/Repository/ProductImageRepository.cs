using Bulky.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.Model;
using Bulky.DataAcces.Repository.IRepository;

namespace Bulky.DataAcces.Repository
{
    public class ProductImageRepository:Repository<ProductImage>, IProductImageRepository
    {
        private AppDbContext _db;
        public ProductImageRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductImage obj)
        {
            _db.productImages.Update(obj);

        }
    }
}
