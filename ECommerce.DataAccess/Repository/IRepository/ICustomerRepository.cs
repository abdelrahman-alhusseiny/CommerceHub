using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<(IEnumerable<Customer>, int)> GetFilteredCustomersAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize);
        Task UpdateAsync(Customer customer);
    }
}
