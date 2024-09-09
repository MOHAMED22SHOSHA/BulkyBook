using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Model.ViewModels
{
	public class OrderVM
	{
		public OrderHeeaader OrderHeeaader { get; set; }
		public IEnumerable<OrderDetail> OrderDetail { get; set; }
	}
}
