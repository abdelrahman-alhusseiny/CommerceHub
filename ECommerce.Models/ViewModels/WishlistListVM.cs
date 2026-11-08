using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class WishlistRowVM
    {
        public WishlistItem WishlistItem { get; set; }

        public string? ProductImageUrl { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCategoryName { get; set; }
        public string? ProductUnitPrice { get; set; }
        public int? ProductQuantity { get; set; }
    }

    public class WishlistListVM
    {
        public IEnumerable<WishlistRowVM> WishlistItems { get; set; } = new List<WishlistRowVM>();
    }
}
