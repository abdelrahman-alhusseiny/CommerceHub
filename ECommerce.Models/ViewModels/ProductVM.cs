using ECommerce.Models.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class ProductImageEntry
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ProductVM
    {
        public Product Product { get; set; }
        public string? SourceView { get; set; }
        public IEnumerable<SelectListItem>? CategoryList { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }


        public List<int>? ProductImageIds { get; set; }
        public List<string>? ProductImageUrls { get; set; }
        public List<ProductImageEntry>? ProductImagePairs { get; set; }
        public bool HasCustomImage { get; set; } = false;


        public string? DisplayPrice { get; set; }
        public string? DisplayOldPrice { get; set; }


        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }


        public string? VendorImageUrl { get; set; }
        public string? VendorStoreName { get; set; }
        public string? VendorPhoneNumber { get; set; }
        public string? VendorStoreAddress { get; set; }
        public string? VendorStoreDescription { get; set; }


        public bool IsInCart { get; set; }
        public bool IsInWishlist { get; set; }


        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
