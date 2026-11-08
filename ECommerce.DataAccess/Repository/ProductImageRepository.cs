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
    public class ProductImageRepository
    : Repository<ProductImage>, IProductImageRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductImageRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ProductImage>> GetImagesByIdsAsync(List<int> imageIds)
        {
            return await _db.ProductImages.Where(pi => imageIds.Contains(pi.Id)).ToListAsync();
        }

        public async Task UpdateAsync(ProductImage productImage)
        {
            var objFromDb = await _db.ProductImages.FirstOrDefaultAsync(
                pi => pi.Id == productImage.Id);

            if (objFromDb != null)
            {
                objFromDb.ImageUrl = productImage.ImageUrl;
            }
        }
    }
}
