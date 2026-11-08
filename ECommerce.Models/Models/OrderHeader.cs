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
    public class OrderHeader
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput] 
        public int Id { get; set; }

       
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }



        public string? StreetAddress1 { get; set; }
        public string? StreetAddress2 { get; set; }  
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        

        public decimal? SubtotalCost { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? TotalCost { get; set; }



        [Required]
        [ValidateNever]
        public string? OrderStatus { get; set; }

        [Required]
        [ValidateNever]
        public string? PaymentStatus { get; set; }



        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }


        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        

        [Required]
        [ValidateNever]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ShippingDate { get; set; }



        [ValidateNever]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [Required]
        [ValidateNever]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        [ValidateNever]
        public Customer Customer { get; set; }
    }
}
