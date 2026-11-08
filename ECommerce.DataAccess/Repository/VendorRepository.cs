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
    public class VendorRepository : Repository<Vendor>, IVendorRepository
    {
        private readonly ApplicationDbContext _db;
        public VendorRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Vendor>, int)> GetFilteredVendorsAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Vendors.Include(v => v.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(v => v.StoreName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(lockoutStatus))
            {
                if (lockoutStatus == "Locked")
                {
                    query = query.Where(v => v.AppUser.LockoutEnd.HasValue &&
                                             v.AppUser.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else if (lockoutStatus == "Unlocked")
                {
                    query = query.Where(v => !v.AppUser.LockoutEnd.HasValue ||
                                             v.AppUser.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(v => v.OwnerFullName),
                "name_desc" => query.OrderByDescending(v => v.OwnerFullName),
                "created" => query.OrderBy(v => v.CreatedAt),
                "created_desc" => query.OrderByDescending(v => v.CreatedAt),
                _ => query.OrderBy(v => v.OwnerFullName)
            };

            int totalItems = await query.CountAsync();

            var vendors = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (vendors, totalItems);
        }

        public async Task UpdateAsync(Vendor vendor)
        {
            var objFromDb = await _db.Vendors.FirstOrDefaultAsync(
                v => v.Id == vendor.Id);

            if (objFromDb != null)
            {
                objFromDb.StoreName = vendor.StoreName;
                objFromDb.OwnerFullName = vendor.OwnerFullName;
                objFromDb.PhoneNumber = vendor.PhoneNumber;
                objFromDb.StoreAddress = vendor.StoreAddress;
                objFromDb.StoreDescription = vendor.StoreDescription;
                objFromDb.ImageUrl = vendor.ImageUrl;
            }
        }
    }
}
