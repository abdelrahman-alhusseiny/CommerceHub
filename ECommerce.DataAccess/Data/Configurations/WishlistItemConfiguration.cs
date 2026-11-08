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
    public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
    {
        public void Configure(EntityTypeBuilder<WishlistItem> builder)
        {
            builder.HasOne(wi => wi.Customer)
                   .WithMany(c => c.WishlistItems)
                   .HasForeignKey(wi => wi.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wi => wi.Product)
                   .WithMany(p => p.WishlistItems)
                   .HasForeignKey(wi => wi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
