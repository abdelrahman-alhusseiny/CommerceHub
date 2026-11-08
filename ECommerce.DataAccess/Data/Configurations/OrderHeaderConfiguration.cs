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
    public class OrderHeaderConfiguration : IEntityTypeConfiguration<OrderHeader>
    {
        public void Configure(EntityTypeBuilder<OrderHeader> builder)
        {
            builder.HasOne(oh => oh.Customer)
                   .WithMany(c => c.OrderHeaders)
                   .HasForeignKey(oh => oh.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
