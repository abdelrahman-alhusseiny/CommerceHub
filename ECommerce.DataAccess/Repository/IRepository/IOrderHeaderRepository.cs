using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {

        Task<(IEnumerable<OrderHeader>, int)> GetFilteredOrderHeadersAsync(
            int? customerId,
            string searchString,
            string orderStatus,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(OrderHeader order);
    }
}
