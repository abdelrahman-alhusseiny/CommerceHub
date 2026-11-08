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
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        private readonly ApplicationDbContext _db;
        public CustomerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Customer>, int)> GetFilteredCustomersAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Customers.Include(c => c.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(c => c.FullName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(lockoutStatus))
            {
                if (lockoutStatus == "Locked")
                {
                    query = query.Where(c => c.AppUser.LockoutEnd.HasValue &&
                                             c.AppUser.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else if (lockoutStatus == "Unlocked")
                {
                    query = query.Where(c => !c.AppUser.LockoutEnd.HasValue ||
                                             c.AppUser.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(c => c.FullName),
                "name_desc" => query.OrderByDescending(c => c.FullName),
                "created" => query.OrderBy(c => c.CreatedAt),
                "created_desc" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderBy(c => c.FullName)
            };

            int totalItems = await query.CountAsync();

            var customers = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (customers, totalItems);
        }

        public async Task UpdateAsync(Customer customer)
        {
            var objFromDb = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == customer.Id);

            if (objFromDb != null)
            {
                objFromDb.FullName = customer.FullName;
                objFromDb.PhoneNumber = customer.PhoneNumber;
                objFromDb.Address = customer.Address;
                objFromDb.Gender = customer.Gender;
                objFromDb.DateOfBirth = customer.DateOfBirth;
                objFromDb.ImageUrl = customer.ImageUrl;
            }
        }
    }
}
