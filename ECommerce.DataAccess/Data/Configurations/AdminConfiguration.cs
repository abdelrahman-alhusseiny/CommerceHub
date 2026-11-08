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
    public class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.HasOne(a => a.AppUser)
                    .WithOne(u => u.Admin)
                    .HasForeignKey<Admin>(a => a.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
