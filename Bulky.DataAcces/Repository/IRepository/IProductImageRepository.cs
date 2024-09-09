using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAcces.Repository.IRepository
{
    public  interface IProductImageRepository:IRepository<ProductImage>
    {
        void Update(ProductImage obj);
    }
}
