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
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Devices and gadgets including phones, laptops, and accessories.", ImageUrl = "/images/categories/electronics.png" },
                new Category { Id = 2, Name = "Books", Description = "Printed and digital books across various genres and subjects.", ImageUrl = "/images/categories/books.png" },
                new Category { Id = 3, Name = "Clothing", Description = "Men’s, women’s, and children’s apparel for all seasons.", ImageUrl = "/images/categories/clothing.png" },
                new Category { Id = 4, Name = "Home & Kitchen", Description = "Furniture, appliances, cookware, and home essentials.", ImageUrl = "/images/categories/home-kitchen.png" },
                new Category { Id = 5, Name = "Sports & Outdoors", Description = "Equipment, apparel, and accessories for fitness and outdoor activities.", ImageUrl = "/images/categories/sports-outdoors.png" },
                new Category { Id = 6, Name = "Beauty & Personal Care", Description = "Cosmetics, skincare, haircare, and grooming products.", ImageUrl = "/images/categories/beauty-care.png" },
                new Category { Id = 7, Name = "Toys & Games", Description = "Educational toys, puzzles, and entertainment for children and adults.", ImageUrl = "/images/categories/toys-games.png" },
                new Category { Id = 8, Name = "Automotive", Description = "Car accessories, tools, and maintenance products.", ImageUrl = "/images/categories/automotive.png" },
                new Category { Id = 9, Name = "Health & Wellness", Description = "Supplements, medical supplies, and wellness products.", ImageUrl = "/images/categories/health-wellness.png" },
                new Category { Id = 10, Name = "Groceries", Description = "Food, beverages, and everyday household consumables.", ImageUrl = "/images/categories/groceries.png" },
                new Category { Id = 11, Name = "Others", Description = "Miscellaneous items that do not fit into the main categories.", ImageUrl = "/images/categories/others.png" }
            );
        }
    }
}
