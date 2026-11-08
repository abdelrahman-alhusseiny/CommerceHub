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
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {

            builder.HasOne(oi => oi.OrderHeader)
                   .WithMany(oh => oh.OrderItems)
                   .HasForeignKey(oi => oi.OrderHeaderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(oi => oi.Product)
                   .WithMany(p => p.OrderItems)
                   .HasForeignKey(oi => oi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
