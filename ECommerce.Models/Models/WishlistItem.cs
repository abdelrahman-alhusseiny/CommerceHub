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
    public class WishlistItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required]
        [ValidateNever]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ValidateNever]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        [ValidateNever]
        public Customer Customer { get; set; }

        [Required]
        [ValidateNever]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
    }
}
