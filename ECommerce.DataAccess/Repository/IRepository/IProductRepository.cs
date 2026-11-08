using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<(IEnumerable<Product>, int)> GetFilteredProductsAsync(
            int? vendorId,
            string searchString,
            int? categoryId,
            string currentSort,
            int currentPage,
            int pageSize);

        Task<bool> IsIsbnExistsAsync(string isbn, int? excludeId = null);

        Task UpdateAsync(Product product);
    }
}
