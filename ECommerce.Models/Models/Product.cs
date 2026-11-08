using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is Required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long")]
        [MaxLength(200, ErrorMessage = "Name must be less than 200 characters long")]
        [Display(Name = "Name", Prompt = "Enter product name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is Required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters long")]
        [MaxLength(1000,
            ErrorMessage = "Description must be less than 1000 characters long")]
        [Display(Name = "Description", Prompt = "Enter description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "ISBN is Required")]
        [MinLength(10, ErrorMessage = "ISBN must be at least 10 characters long")]
        [MaxLength(13, ErrorMessage = "ISBN must be less than 13 characters long")]
        [Display(Name = "ISBN", Prompt = "Enter ISBN")]
        public string ISBN { get; set; }

        [Required(ErrorMessage = "Price is Required")]
        [Range(1, 10000, ErrorMessage = "Price must be between 1-10000")]
        [Display(Name = "Price", Prompt = "Enter price")]
        public decimal Price { get; set; }

        [Range(1, 10000, ErrorMessage = "Old Price must be between 1-10000")]
        [Display(Name = "Old Price (Optional)", Prompt = "Enter old price")]
        public decimal? OldPrice { get; set; }

        [Required(ErrorMessage = "Quantity is Required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1-1000")]
        [Display(Name = "Quantity", Prompt = "Enter quantity")]
        public int Quantity { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Category is Required")]
        [Display(Name = "Category", Prompt = "Select category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

        [ValidateNever]
        public ICollection<ProductImage> ProductImages { get; set; }

        [Required]
        [ValidateNever]
        public int VendorId { get; set; }

        [ForeignKey("VendorId")]
        [ValidateNever]
        public Vendor Vendor { get; set; }

        [ValidateNever]
        public ICollection<CartItem> CartItems { get; set; }

        [ValidateNever]
        public ICollection<WishlistItem> WishlistItems { get; set; }
        
        [ValidateNever]
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
