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
    public class WishlistItemRepository : Repository<WishlistItem>, IWishlistItemRepository
    {
        private readonly ApplicationDbContext _db;
        public WishlistItemRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsync(WishlistItem wishlistItem)
        {
            var objFromDb = await _db.WishlistItems.FirstOrDefaultAsync(wi => wi.Id == wishlistItem.Id);

            if (objFromDb != null)
            {
            }
        }
    }
}
