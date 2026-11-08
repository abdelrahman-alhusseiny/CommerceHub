using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface ICartItemRepository : IRepository<CartItem>
    {
        Task UpdateAsync(CartItem cartItem);
    }
}
