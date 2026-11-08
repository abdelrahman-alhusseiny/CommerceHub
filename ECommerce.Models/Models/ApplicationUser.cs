using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.Models
{
    public class ApplicationUser : IdentityUser
    {
        [ValidateNever]
        public Customer Customer { get; set; }

        [ValidateNever]
        public Vendor Vendor { get; set; }

        [ValidateNever]
        public Admin Admin { get; set; }
    }
}
