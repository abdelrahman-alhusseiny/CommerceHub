using ECommerce.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class CategoryVM
    {
        public Category Category { get; set; }
        public string? SourceView { get; set; }


        public string? DisplayImageUrl { get; set; }
        public bool HasCustomImage { get; set; } = false;


        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
