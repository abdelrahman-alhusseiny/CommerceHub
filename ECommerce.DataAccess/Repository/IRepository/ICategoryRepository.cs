using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<(IEnumerable<Category>, int)> GetFilteredCategoriesAsync(
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize);

        Task<bool> IsNameExistsAsync(string name, int? excludeId = null);

        Task UpdateAsync(Category category);
    }
}
