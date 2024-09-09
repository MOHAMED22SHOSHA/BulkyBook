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
	public class OrderDetailsRepository: Repository<OrderDetail>, IOrderDetailsRepository
	{

		private AppDbContext _db;
		public OrderDetailsRepository(AppDbContext db) : base(db)
		{
			_db = db;
		}

		public void Update(OrderDetail obj)
		{
			_db.orderDetail.Update(obj);
		}
	}
}
