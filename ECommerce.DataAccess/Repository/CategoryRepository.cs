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
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Category>, int)> GetFilteredCategoriesAsync(
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(c => c.Name.Contains(searchString) ||
                    c.Description.Contains(searchString));
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(c => c.Name),
                "name_desc" => query.OrderByDescending(c => c.Name),
                "created" => query.OrderBy(c => c.CreatedAt),
                "created_desc" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderBy(c => c.Name)
            };

            int totalItems = await query.CountAsync();

            var categories = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, totalItems);
        }

        public async Task<bool> IsNameExistsAsync(
            string name, int? excludeId = null)
        {
            return await _db.Categories.AnyAsync(
                c => c.Name == name &&
                (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task UpdateAsync(Category category)
        {
            var objFromDb = await _db.Categories.FirstOrDefaultAsync(
                c => c.Id == category.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = category.Name;
                objFromDb.Description = category.Description;
                objFromDb.ImageUrl = category.ImageUrl;
            }
        }
    }
}
