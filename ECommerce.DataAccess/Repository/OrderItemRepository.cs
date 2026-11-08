using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository
{
    public class OrderItemRepository : Repository<OrderItem>, IOrderItemRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderItemRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsync(OrderItem orderItem)
        {
            var objFromDb = await _db.OrderItems.FirstOrDefaultAsync(
                o => o.Id == orderItem.Id);

            if (objFromDb != null)
            {
                objFromDb.Price = orderItem.Price;
                objFromDb.Quantity = orderItem.Quantity;
            }
        }
    }
}
