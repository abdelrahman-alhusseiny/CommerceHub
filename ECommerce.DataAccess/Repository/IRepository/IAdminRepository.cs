using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IAdminRepository : IRepository<Admin>
    {
        Task<(IEnumerable<Admin>, int)> GetFilteredAdminsAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Admin admin);
    }
}
