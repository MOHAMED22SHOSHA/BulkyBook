using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAcces.Repository.IRepository
{
	public interface IUnitOfWork
	{
		ICategoryRepository Category { get; }
		IProductRepository Product { get; }
        ICompanyRepository Company { get; }
        IApplicationUserRepository ApplicationUser{ get; }
		IShoppingCartRepository ShoppingCart { get; }
		IOrderDetailsRepository OrderDetails { get; }
		IOrderHeaderRepository OrderHeeaader { get; }
        IProductImageRepository ProductImage { get; }
        void Save();
	}
}
