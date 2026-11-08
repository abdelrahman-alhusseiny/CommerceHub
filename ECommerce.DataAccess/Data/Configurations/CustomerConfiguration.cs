using ECommerce.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasOne(c => c.AppUser)
                    .WithOne(u => u.Customer)
                    .HasForeignKey<Customer>(c => c.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
