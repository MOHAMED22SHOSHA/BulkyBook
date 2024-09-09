using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAcces.Repository.IRepository;
using Bulky.DataAccess.Data;
using Bulky.Model;
namespace Bulky.DataAcces.Repository
{
    internal class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private AppDbContext _db;
        public ShoppingCartRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        //public int Count(ShoppingCart obj)
        //{
        //    return _db.shoppingCarts.Count();
        //}

        public void Update(ShoppingCart obj)
        {
            _db.shoppingCarts.Update(obj);
        }
         


    }
}
