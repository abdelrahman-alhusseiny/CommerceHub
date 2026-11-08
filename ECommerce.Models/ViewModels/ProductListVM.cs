using ECommerce.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class ProductRowVM
    {
        public Product Product { get; set; }

        public string? DisplayImageUrl { get; set; }
        public string? DisplayPrice { get; set; }
        public string? DisplayOldPrice { get; set; }


        public string? CategoryName { get; set; }
        public string? VendorStoreName { get; set; }
    }
    public class ProductListVM
    {
        public IEnumerable<ProductRowVM> Products { get; set; }
        public string? SourceView { get; set; }


        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public IEnumerable<SelectListItem>? CategoryList { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? PriceSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
