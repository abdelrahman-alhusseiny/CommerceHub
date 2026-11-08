using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.WebUI.Areas.Customer.Controllers
{
    [Authorize(Roles = SD.Role_Customer)]
    [Area("Customer")]
    [Route("Customer/Order")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] string searchString = "",
            [FromQuery] string orderStatus = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id);
            if (customer == null)
            {
                return NotFound();
            }

            int pageSize = 5;
            var (orderHeaders, totalItems) = await _unitOfWork.OrderHeader.GetFilteredOrderHeadersAsync(
                customer.Id, searchString, orderStatus, currentSort, currentPage, pageSize);

            var orderHeaderRows = new List<OrderHeaderRowVM>();
            foreach (var oh in orderHeaders)
            {
                orderHeaderRows.Add(new OrderHeaderRowVM
                {
                    OrderHeader = oh,
                    DisplayTotalCost = oh.TotalCost.HasValue ? oh.TotalCost.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : ""
                });
            }

            var vm = new OrderHeaderListVM
            {
                OrderHeaders = orderHeaderRows,

                SearchString = searchString,

                OrderStatus = orderStatus,
                OrderStatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = SD.OrderStatus_Pending,   Text = SD.OrderStatus_Pending,   Selected = orderStatus == SD.OrderStatus_Pending },
                    new SelectListItem { Value = SD.OrderStatus_Approved,  Text = SD.OrderStatus_Approved,  Selected = orderStatus == SD.OrderStatus_Approved },
                    new SelectListItem { Value = SD.OrderStatus_InProcess, Text = SD.OrderStatus_InProcess, Selected = orderStatus == SD.OrderStatus_InProcess },
                    new SelectListItem { Value = SD.OrderStatus_Shipped,   Text = SD.OrderStatus_Shipped,   Selected = orderStatus == SD.OrderStatus_Shipped },
                    new SelectListItem { Value = SD.OrderStatus_Cancelled, Text = SD.OrderStatus_Cancelled, Selected = orderStatus == SD.OrderStatus_Cancelled },
                    new SelectListItem { Value = SD.OrderStatus_Refunded,  Text = SD.OrderStatus_Refunded,  Selected = orderStatus == SD.OrderStatus_Refunded }
                },

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                TotalCostSort = currentSort == "totalCost" ? "totalCost_desc" : "totalCost",
                OrderDateSort = currentSort == "orderDate" ? "orderDate_desc" : "orderDate",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Customer/Views/Order/GetAllOrders.cshtml", vm);
        }

        [HttpGet("GetOrder/{id:int}")]
        public async Task<IActionResult> GetOrder([FromRoute] int id, [FromQuery] OrderHeaderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == id, includeProperties: "OrderItems,OrderItems.Product");
            if (orderHeader == null)
            {
                return NotFound();
            }

            var orderItemRows = new List<OrderItemRowVM>();
            foreach (var oi in orderHeader.OrderItems)
            {
                orderItemRows.Add(new OrderItemRowVM
                {
                    OrderItem = oi,
                    ProductName = oi.Product?.Name,
                    DisplayUnitPrice = oi.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG")),
                    DisplayTotalPrice = (oi.Price * oi.Quantity).ToString("C", new System.Globalization.CultureInfo("ar-EG"))
                });
            }

            vm.OrderHeader = orderHeader;
            vm.OrderItems = orderItemRows;

            vm.DisplaySubtotalCost = orderHeader.SubtotalCost.HasValue ? orderHeader.SubtotalCost.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "";
            vm.DisplayShippingCost = orderHeader.ShippingCost.HasValue ? orderHeader.ShippingCost.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "";
            vm.DisplayTotalCost = orderHeader.TotalCost.HasValue ? orderHeader.TotalCost.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "";

            return View("Areas/Customer/Views/Order/GetOrder.cshtml", vm);
        }
    }
}
