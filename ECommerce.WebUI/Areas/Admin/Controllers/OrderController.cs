using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe;

namespace ECommerce.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_SuperAdmin)]
    [Area("Admin")]
    [Route("Admin/Order")]
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
            int pageSize = 5;
            var (orderHeaders, totalItems) = await _unitOfWork.OrderHeader.GetFilteredOrderHeadersAsync(
                null, searchString, orderStatus, currentSort, currentPage, pageSize);

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

            return View("Areas/Admin/Views/Order/GetAllOrders.cshtml", vm);
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

            return View("Areas/Admin/Views/Order/GetOrder.cshtml", vm);
        }

        [HttpPost("StartProcessing")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing([FromForm] OrderHeaderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == vm.OrderHeader.Id);
            if (orderHeader == null)
            {
                return NotFound();
            }

            if (orderHeader.OrderStatus != SD.OrderStatus_Approved)
            {
                TempData["Error"] = "Only approved orders can be moved to processing.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            orderHeader.OrderStatus = SD.OrderStatus_InProcess;
            await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = $"Order #{orderHeader.Id} is now being processed.";
            return RedirectToAction("GetOrder", "Order", new
            {
                area = "Admin",
                id = vm.OrderHeader.Id,
                searchString = vm.SearchString,
                orderStatus = vm.OrderStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ShipOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder([FromForm] OrderHeaderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == vm.OrderHeader.Id);
            if (orderHeader == null)
            {
                return NotFound();
            }

            if (orderHeader.OrderStatus != SD.OrderStatus_InProcess)
            {
                TempData["Error"] = "Only orders in process can be shipped.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            if (string.IsNullOrWhiteSpace(vm.OrderHeader.TrackingNumber) || string.IsNullOrWhiteSpace(vm.OrderHeader.Carrier))
            {
                TempData["Error"] = "Tracking number and carrier are required to ship the order.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            orderHeader.TrackingNumber = vm.OrderHeader.TrackingNumber.Trim();
            orderHeader.Carrier = vm.OrderHeader.Carrier.Trim();
            orderHeader.OrderStatus = SD.OrderStatus_Shipped;
            orderHeader.ShippingDate = DateTime.UtcNow;

            await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = $"Order #{orderHeader.Id} has been shipped successfully.";
            return RedirectToAction("GetOrder", "Order", new
            {
                area = "Admin",
                id = vm.OrderHeader.Id,
                searchString = vm.SearchString,
                orderStatus = vm.OrderStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("CancelOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder([FromForm] OrderHeaderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == vm.OrderHeader.Id);
            if (orderHeader == null)
            {
                return NotFound();
            }

            if (orderHeader.OrderStatus == SD.OrderStatus_Shipped)
            {
                TempData["Error"] = "Cannot cancel shipped orders. Customer must initiate a return.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }
            if (orderHeader.OrderStatus == SD.OrderStatus_Cancelled || orderHeader.OrderStatus == SD.OrderStatus_Refunded)
            {
                TempData["Error"] = "This order has already been cancelled.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            if (string.IsNullOrEmpty(orderHeader.PaymentIntentId))
            {
                TempData["error"] = "Payment information was not found.";
                return RedirectToAction("GetOrder", "Order", new
                {
                    area = "Admin",
                    id = vm.OrderHeader.Id,
                    searchString = vm.SearchString,
                    orderStatus = vm.OrderStatus,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = orderHeader.PaymentIntentId,
                    Reason = RefundReasons.RequestedByCustomer
                };

                var service = new RefundService();
                var refund = service.Create(options);

                if (refund.Status == "succeeded" || refund.Status == "pending")
                {
                    orderHeader.OrderStatus = SD.OrderStatus_Refunded;
                    orderHeader.PaymentStatus = SD.PaymentStatus_Refunded;

                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                    await _unitOfWork.SaveAsync();

                    TempData["Success"] = $"Order #{orderHeader.Id} has been cancelled and refund issued.";
                }
                else
                {
                    orderHeader.OrderStatus = SD.OrderStatus_Cancelled;

                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                    await _unitOfWork.SaveAsync();

                    TempData["Error"] = $"Refund status: {refund.Status}. Order cancelled, refund requires manual follow-up.";
                }
            }
            catch (StripeException ex)
            {
                orderHeader.OrderStatus = SD.OrderStatus_Cancelled;

                await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                TempData["Error"] = $"Stripe refund failed: {ex.Message}. Order cancelled, refund must be processed manually.";
            }

            return RedirectToAction("GetOrder", "Order", new
            {
                area = "Admin",
                id = vm.OrderHeader.Id,
                searchString = vm.SearchString,
                orderStatus = vm.OrderStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        //[HttpGet("RefundOrder/{id:int}")]
        //public async Task<IActionResult> RefundOrder([FromRoute] int id, [FromQuery] OrderHeaderVM vm)
        //{
        //    var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(oh => oh.Id == id);
        //    if (orderHeader == null)
        //    {
        //        return NotFound();
        //    }

        //    if (string.IsNullOrEmpty(orderHeader.PaymentIntentId))
        //    {
        //        TempData["error"] = "Payment information was not found.";
        //        return RedirectToAction("GetOrder", "Order", new
        //        {
        //            area = "Admin",
        //            id = vm.OrderHeader.Id,
        //            searchString = vm.SearchString,
        //            orderStatus = vm.OrderStatus,
        //            currentSort = vm.CurrentSort,
        //            currentPage = vm.CurrentPage
        //        });
        //    }

        //    try
        //    {
        //        var options = new RefundCreateOptions
        //        {
        //            PaymentIntent = orderHeader.PaymentIntentId,
        //            Reason = RefundReasons.RequestedByCustomer
        //        };

        //        var service = new RefundService();
        //        var refund = service.Create(options);

        //        if (refund.Status == "succeeded" || refund.Status == "pending")
        //        {
        //            orderHeader.OrderStatus = SD.OrderStatus_Refunded;
        //            orderHeader.PaymentStatus = SD.PaymentStatus_Refunded;

        //            await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
        //            await _unitOfWork.SaveAsync();

        //            TempData["Success"] = $"Order #{orderHeader.Id} has been cancelled and refund issued.";
        //        }
        //        else
        //        {
        //            orderHeader.OrderStatus = SD.OrderStatus_Cancelled;

        //            await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
        //            await _unitOfWork.SaveAsync();

        //            TempData["Error"] = $"Refund status: {refund.Status}. Order cancelled, refund requires manual follow-up.";
        //        }
        //    }
        //    catch (StripeException ex)
        //    {
        //        orderHeader.OrderStatus = SD.OrderStatus_Cancelled;

        //        await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
        //        await _unitOfWork.SaveAsync();

        //        TempData["Error"] = $"Stripe refund failed: {ex.Message}. Order cancelled, refund must be processed manually.";
        //    }

        //    return RedirectToAction("GetOrder", "Order", new
        //    {
        //        area = "Admin",
        //        id = vm.OrderHeader.Id,
        //        searchString = vm.SearchString,
        //        orderStatus = vm.OrderStatus,
        //        currentSort = vm.CurrentSort,
        //        currentPage = vm.CurrentPage
        //    });
        //}
    }
}
