using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IOrderItemRepository : IRepository<OrderItem>
    {
        Task UpdateAsync(OrderItem orderItem);
    }
}
