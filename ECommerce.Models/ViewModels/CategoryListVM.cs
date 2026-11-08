using ECommerce.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class CategoryRowVM
    {
        public Category Category { get; set; }

        public string? DisplayImageUrl { get; set; }
    }

    public class CategoryListVM
    {
        public IEnumerable<CategoryRowVM> Categories { get; set; }
        public string? SourceView { get; set; }


        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
