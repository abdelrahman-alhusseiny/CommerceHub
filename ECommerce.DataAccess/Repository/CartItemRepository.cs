using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository
{
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        private readonly ApplicationDbContext _db;
        public CartItemRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsync(CartItem cartItem)
        {
            var objFromDb = await _db.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItem.Id);

            if (objFromDb != null)
            {
                objFromDb.Quantity = cartItem.Quantity;
            }
        }
    }
}
