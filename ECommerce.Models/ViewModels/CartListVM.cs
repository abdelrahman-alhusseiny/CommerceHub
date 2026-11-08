using ECommerce.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class CartRowVM
    {
        public CartItem CartItem { get; set; }

        public string? ProductImageUrl { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCategoryName { get; set; }
        public string? ProductUnitPrice { get; set; }
        public int? ProductQuantity { get; set; }
        public string? ProductTotalPrice { get; set; }
    }

    public class CartListVM
    {
        public IEnumerable<CartRowVM> CartItems { get; set; } = new List<CartRowVM>();
        public string? OrderTotalPrice { get; set; }
    }
}
