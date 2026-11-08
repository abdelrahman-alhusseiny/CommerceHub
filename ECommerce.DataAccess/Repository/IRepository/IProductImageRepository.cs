using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IProductImageRepository : IRepository<ProductImage>
    {
        Task<IEnumerable<ProductImage>> GetImagesByIdsAsync(List<int> imageIds);
        Task UpdateAsync(ProductImage productImage);
    }
}
