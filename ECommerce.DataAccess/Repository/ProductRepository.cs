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
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Product>, int)> GetFilteredProductsAsync(
            int? vendorId,
            string searchString,
            int? categoryId,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Vendor)
                .AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(p => p.VendorId == vendorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(
                    p => p.Name.Contains(searchString) ||
                    p.Description.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "created" => query.OrderBy(p => p.CreatedAt),
                "created_desc" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            int totalItems = await query.CountAsync();

            var products = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalItems);
        }

        public async Task<bool> IsIsbnExistsAsync(
            string isbn, int? excludeId = null)
        {
            return await _db.Products.AnyAsync(
                p => p.ISBN == isbn &&
                (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        public async Task UpdateAsync(Product product)
        {
            var objFromDb = await _db.Products.FirstOrDefaultAsync(
                p => p.Id == product.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = product.Name;
                objFromDb.Description = product.Description;
                objFromDb.ISBN = product.ISBN;
                objFromDb.Price = product.Price;
                objFromDb.OldPrice = product.OldPrice;
                objFromDb.Quantity = product.Quantity;
                objFromDb.CategoryId = product.CategoryId;
                objFromDb.ProductImages = product.ProductImages;
            }
        }
    }
}
