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
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is Required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Name must be less than 100")]
        [Display(Name = "Name", Prompt = "Enter category name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is Required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters long")]
        [MaxLength(1000, ErrorMessage = "Description must be less than 1000 characters long")]
        [Display(Name = "Description", Prompt = "Enter description")]
        public string Description { get; set; }

        [ValidateNever]
        public string? ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Image (Optional)", Prompt = "Upload category image")]
        public IFormFile? ImageFile { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<Product> Products { get; set; }
    }
}
