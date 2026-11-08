using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public IProductImageRepository ProductImage { get; private set; }
        public ICustomerRepository Customer { get; private set; }
        public IVendorRepository Vendor { get; private set; }
        public IAdminRepository Admin { get; private set; }
        public ICartItemRepository CartItem { get; private set; }
        public IWishlistItemRepository WishlistItem { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IOrderItemRepository OrderItem { get; private set; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            ProductImage = new ProductImageRepository(_db);
            Customer = new CustomerRepository(_db);
            Vendor = new VendorRepository(_db);
            Admin = new AdminRepository(_db);
            CartItem = new CartItemRepository(_db);
            WishlistItem = new WishlistItemRepository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderItem = new OrderItemRepository(_db);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
