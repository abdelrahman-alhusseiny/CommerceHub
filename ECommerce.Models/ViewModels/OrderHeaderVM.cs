using ECommerce.Models.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Models.ViewModels
{
    public class OrderItemRowVM
    {
        public OrderItem OrderItem { get; set; }
        public string? ProductName { get; set; }
        public string? DisplayUnitPrice { get; set; }
        public string? DisplayTotalPrice { get; set; }
    }

    public class OrderHeaderVM
    {
        public OrderHeader OrderHeader { get; set; }
        public IEnumerable<OrderItemRowVM> OrderItems { get; set; }

        public string? DisplaySubtotalCost { get; set; }
        public string? DisplayShippingCost { get; set; }
        public string? DisplayTotalCost { get; set; }

        public string? SearchString { get; set; }
        public string? OrderStatus { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
