using Microsoft.AspNetCore.Http;
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
    public class Vendor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Store Name is Required")]
        [MinLength(3, ErrorMessage = "Store Name must be at least 3 characters long")]
        [MaxLength(100,
            ErrorMessage = "Store Name must be less than 100 characters long")]
        [Display(Name = "Store Name", Prompt = "Enter store name")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Owner Full Name is Required")]
        [MinLength(3,
            ErrorMessage = "Owner Full Name must be at least 3 characters long")]
        [MaxLength(100,
            ErrorMessage = "Owner Full Name must be less than 100 characters long")]
        [Display(Name = "Owner Full Name", Prompt = "Enter owner full name")]
        public string OwnerFullName { get; set; }

        [Required(ErrorMessage = "Phone Number is Required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number", Prompt = "Enter phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Store Address is Required")]
        [MinLength(5, ErrorMessage = "Store Address must be at least 5 characters long")]
        [MaxLength(200,
            ErrorMessage = "Store Address must be less than 200 characters long")]
        [Display(Name = "Store Address", Prompt = "Enter store address")]
        public string StoreAddress { get; set; }

        [Required(ErrorMessage = "Store Description is Required")]
        [MinLength(10,
            ErrorMessage = "Store Description must be at least 10 characters long")]
        [MaxLength(1000,
            ErrorMessage = "Store Description must be less than 1000 characters long")]
        [Display(Name = "Store Description", Prompt = "Enter store description")]
        public string StoreDescription { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public string? ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Profile Image (Optional)", Prompt = "Upload profile image")]
        public IFormFile? ImageFile { get; set; }

        [Required]
        [ValidateNever]
        public string AppUserId { get; set; }

        [ForeignKey("AppUserId")]
        [ValidateNever]
        public ApplicationUser AppUser { get; set; }

        [Required]
        [ValidateNever]
        public ICollection<Product> Products { get; set; }
    }
}
