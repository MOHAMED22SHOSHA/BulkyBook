using Bulky.DataAcces.Repository.IRepository;
using Bulky.DataAccess.Data;
using Bulky.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAcces.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private AppDbContext _db;
        public CompanyRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }



        public void Update(Company obj)
        {
            _db.companies.Update(obj);
        }
    }
}
