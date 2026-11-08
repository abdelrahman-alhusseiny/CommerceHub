using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        IProductImageRepository ProductImage { get; }
        ICustomerRepository Customer { get; }
        IVendorRepository Vendor { get; }
        IAdminRepository Admin { get; }
        ICartItemRepository CartItem { get; }
        IWishlistItemRepository WishlistItem { get; }
        IOrderHeaderRepository OrderHeader { get; }
        IOrderItemRepository OrderItem { get; }
        Task SaveAsync();
    }
}
