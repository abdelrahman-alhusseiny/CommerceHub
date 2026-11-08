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
    public class AdminRepository : Repository<Admin>, IAdminRepository
    {
        private readonly ApplicationDbContext _db;
        public AdminRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Admin>, int)> GetFilteredAdminsAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Admins.Include(a => a.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(a => a.FullName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(lockoutStatus))
            {
                if (lockoutStatus == "Locked")
                {
                    query = query.Where(a => a.AppUser.LockoutEnd.HasValue &&
                                             a.AppUser.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else if (lockoutStatus == "Unlocked")
                {
                    query = query.Where(a => !a.AppUser.LockoutEnd.HasValue ||
                                             a.AppUser.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(a => a.FullName),
                "name_desc" => query.OrderByDescending(a => a.FullName),
                "created" => query.OrderBy(a => a.CreatedAt),
                "created_desc" => query.OrderByDescending(a => a.CreatedAt),
                _ => query.OrderBy(a => a.FullName)
            };

            int totalItems = await query.CountAsync();

            var admins = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (admins, totalItems);
        }

        public async Task UpdateAsync(Admin admin)
        {
            var objFromDb = await _db.Admins.FirstOrDefaultAsync(a => a.Id == admin.Id);

            if (objFromDb != null)
            {
                objFromDb.FullName = admin.FullName;
                objFromDb.PhoneNumber = admin.PhoneNumber;
                objFromDb.Address = admin.Address;
                objFromDb.Gender = admin.Gender;
                objFromDb.DateOfBirth = admin.DateOfBirth;
                objFromDb.ImageUrl = admin.ImageUrl;
            }
        }
    }
}
