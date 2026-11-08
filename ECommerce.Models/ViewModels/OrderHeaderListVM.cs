using ECommerce.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class OrderHeaderRowVM
    {
        public OrderHeader OrderHeader { get; set; }
        public string? DisplayTotalCost { get; set; }
    }
    public class OrderHeaderListVM
    {
        public IEnumerable<OrderHeaderRowVM> OrderHeaders { get; set; }

        public string? SearchString { get; set; }
        public string? OrderStatus { get; set; }
        public IEnumerable<SelectListItem>? OrderStatusList { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? TotalCostSort { get; set; }
        public string? OrderDateSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
