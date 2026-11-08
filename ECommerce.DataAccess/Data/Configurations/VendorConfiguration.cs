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
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.HasOne(v => v.AppUser)
                    .WithOne(u => u.Vendor)
                    .HasForeignKey<Vendor>(v => v.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
