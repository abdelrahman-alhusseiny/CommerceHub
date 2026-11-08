using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Security.Claims;

namespace ECommerce.WebUI.Areas.Customer.Controllers
{
    [Authorize(Roles = SD.Role_Customer)]
    [Area("Customer")]
    [Route("Customer/Cart")]
    public class CartController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _webhookSecret;

        public CartController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webhookSecret = configuration["Stripe:WebhookSecret"];
        }

        [HttpGet("CheckoutOrder")]
        public async Task<IActionResult> CheckoutOrder()
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

            var cartItems = await _unitOfWork.CartItem.GetAllAsync(ci => ci.CustomerId == customer.Id, includeProperties: "Product,Product.Category,Product.ProductImages");

            var cartItemRows = new List<CartRowVM>();
            foreach (var ci in cartItems)
            {
                cartItemRows.Add(new CartRowVM
                {
                    CartItem = ci,
                    ProductImageUrl = string.IsNullOrEmpty(ci.Product.ProductImages.FirstOrDefault()?.ImageUrl) ? "/images/products/default-product.png" : ci.Product.ProductImages.FirstOrDefault()?.ImageUrl,
                    ProductName = ci.Product.Name,
                    ProductCategoryName = ci.Product.Category.Name,
                    ProductUnitPrice = ci.Product.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG")),
                    ProductQuantity = ci.Product.Quantity,
                    ProductTotalPrice = (ci.Product.Price * ci.Quantity).ToString("C", new System.Globalization.CultureInfo("ar-EG"))
                });
            }

            var vm = new CartListVM
            {
                CartItems = cartItemRows,
                OrderTotalPrice = cartItemRows.Sum(ci => ci.CartItem.Quantity * ci.CartItem.Product.Price).ToString("C", new System.Globalization.CultureInfo("ar-EG"))
            };

            return View("Areas/Customer/Views/Cart/CheckoutOrder.cshtml", vm);
        }

        [HttpPost("UpdateCartSession")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartSession()
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

            var cartItems = await _unitOfWork.CartItem.GetAllAsync(ci => ci.CustomerId == customer.Id);
            var cartCount = cartItems.Sum(ci => ci.Quantity);

            HttpContext.Session.SetInt32(SD.SessionCartCount, cartCount);

            return NoContent();
        }

        [HttpPost("IncreaseItem")]
        public async Task<IActionResult> IncreaseItem([FromForm] int cartItemId)
        {
            var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.Id == cartItemId, includeProperties: "Product");
            if (cartItem == null)
            {
                return NotFound();
            }

            if (cartItem.Quantity < cartItem.Product.Quantity)
            {
                cartItem.Quantity += 1;
                await _unitOfWork.CartItem.UpdateAsync(cartItem);
                await _unitOfWork.SaveAsync();

                await UpdateCartSession();
            }
            else
            {
                TempData["error"] = "Not enough stock available.";
            }

            return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
        }

        [HttpPost("DecreaseItem")]
        public async Task<IActionResult> DecreaseItem([FromForm] int cartItemId)
        {
            var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.Id == cartItemId);
            if (cartItem == null)
            {
                return NotFound();
            }

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity -= 1;
                await _unitOfWork.CartItem.UpdateAsync(cartItem);
            }
            else
            {
                _unitOfWork.CartItem.Remove(cartItem);
            }

            await _unitOfWork.SaveAsync();
            await UpdateCartSession();

            return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
        }

        [HttpPost("RemoveItem")]
        public async Task<IActionResult> RemoveItem([FromForm] int cartItemId)
        {
            var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.Id == cartItemId);
            if (cartItem == null)
            {
                return NotFound();
            }

            _unitOfWork.CartItem.Remove(cartItem);
            await _unitOfWork.SaveAsync();

            await UpdateCartSession();

            TempData["success"] = "Item removed from cart.";
            return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
        }

        [HttpPost("ClearCart")]
        public async Task<IActionResult> ClearCart()
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

            var cartItems = await _unitOfWork.CartItem.GetAllAsync(ci => ci.CustomerId == customer.Id);

            if (cartItems != null && cartItems.Any())
            {
                _unitOfWork.CartItem.RemoveRange(cartItems);
                await _unitOfWork.SaveAsync();

                HttpContext.Session.SetInt32(SD.SessionCartCount, 0);

                TempData["success"] = "Your cart has been cleared.";
            }
            else
            {
                TempData["error"] = "Your cart is already empty.";
            }

            return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
        }

        [HttpPost("SubmitOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOrder(CartListVM vm)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(c => c.AppUserId == appUser.Id);
            if (customer == null)
            {
                return NotFound();
            }

            var cartItems = await _unitOfWork.CartItem.GetAllAsync(
                ci => ci.CustomerId == customer.Id,
                includeProperties: "Product"
            );

            foreach (var ci in cartItems)
            {
                if (ci.Quantity > ci.Product.Quantity)
                {
                    TempData["error"] = $"Not enough stock for {ci.Product.Name}.";
                    return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
                }
            }

            var orderHeader = new OrderHeader
            {
                OrderStatus = SD.OrderStatus_Pending,
                PaymentStatus = SD.PaymentStatus_Unpaid,

                CustomerId = customer.Id
            };

            foreach (var ci in cartItems)
            {
                orderHeader.OrderItems.Add(new OrderItem
                {
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity,
                    ProductId = ci.ProductId
                });
            }

            await _unitOfWork.OrderHeader.AddAsync(orderHeader);
            await _unitOfWork.SaveAsync();

            return RedirectToAction("ProcessPayment", "Cart", new { orderHeaderId = orderHeader.Id });
        }

        [HttpGet("ProcessPayment")]
        public async Task<IActionResult> ProcessPayment(int orderHeaderId)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == orderHeaderId,
                includeProperties: "OrderItems,OrderItems.Product"
            );
            if (orderHeader == null)
            {
                return NotFound();
            }

            try
            {
                var lineItems = new List<SessionLineItemOptions>();

                foreach (var item in orderHeader.OrderItems)
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "egp",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Name
                            }
                        },
                        Quantity = item.Quantity
                    });
                }

                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {
                    Mode = "payment",
                    LineItems = lineItems,
                    SuccessUrl = domain + $"Customer/Cart/ConfirmOrder/{orderHeader.Id}",
                    CancelUrl = domain + "Customer/Cart/CheckoutOrder",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrderHeaderId", orderHeader.Id.ToString() },
                        { "CustomerId", orderHeader.CustomerId.ToString() }
                    },
                    ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                    {
                        AllowedCountries = new List<string> { "EG" }
                    },
                    ShippingOptions = new List<SessionShippingOptionOptions>
                    {
                        new SessionShippingOptionOptions
                        {
                            ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                            {
                                DisplayName = "Standard Shipping",
                                Type = "fixed_amount",
                                FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                                {
                                    Amount = (long)(SD.ShippingCost_Standard * 100),
                                    Currency = "egp"
                                },
                                DeliveryEstimate = new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                                {
                                    Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                                    {
                                        Unit = "business_day",
                                        Value = 3
                                    },
                                    Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                                    {
                                        Unit = "business_day",
                                        Value = 7
                                    }
                                }
                            }
                        },
                        new SessionShippingOptionOptions
                        {
                            ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                            {
                                DisplayName = "Express Shipping",
                                Type = "fixed_amount",
                                FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                                {
                                    Amount = (long)(SD.ShippingCost_Express * 100),
                                    Currency = "egp"
                                },
                                DeliveryEstimate = new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                                {
                                    Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                                    {
                                        Unit = "business_day",
                                        Value = 1
                                    },
                                    Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                                    {
                                        Unit = "business_day",
                                        Value = 2
                                    }
                                }
                            }
                        },
                    },
                    PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
                    {
                        Enabled = true
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                if (!string.IsNullOrEmpty(session.Id))
                {
                    orderHeader.SessionId = session.Id;

                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                    await _unitOfWork.SaveAsync();
                }

                Response.Headers.Append("Location", session.Url);
                return new StatusCodeResult(303);
            }
            catch (StripeException)
            {
                TempData["error"] = "Payment processing failed. Please try again.";
                return RedirectToAction("CheckoutOrder", "Cart", new { area = "Customer" });
            }
        }

        [AllowAnonymous]
        [HttpPost("Webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = HttpContext.Request.Headers["Stripe-Signature"].ToString();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _webhookSecret,
                    throwOnApiVersionMismatch: false
                );

                if (stripeEvent.Type == SD.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null)
                    {
                        return BadRequest();
                    }

                    if (!int.TryParse(session.Metadata["OrderHeaderId"], out int orderHeaderId))
                    {
                        return BadRequest();
                    }

                    var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                        oh => oh.Id == orderHeaderId,
                        includeProperties: "Customer,OrderItems,OrderItems.Product");
                    if (orderHeader == null)
                    {
                        return NotFound();
                    }

                    orderHeader.PaymentIntentId = session.PaymentIntentId;

                    orderHeader.Email = session.CustomerDetails?.Email;
                    orderHeader.FullName = session.CustomerDetails?.Name;
                    orderHeader.PhoneNumber = session.CustomerDetails?.Phone;

                    orderHeader.StreetAddress1 = session.CustomerDetails?.Address?.Line1;
                    orderHeader.StreetAddress2 = session.CustomerDetails?.Address?.Line2;
                    orderHeader.City = session.CustomerDetails?.Address?.City;
                    orderHeader.State = session.CustomerDetails?.Address?.State;
                    orderHeader.PostalCode = session.CustomerDetails?.Address?.PostalCode;
                    orderHeader.Country = session.CustomerDetails?.Address?.Country;

                    orderHeader.SubtotalCost = (session.AmountSubtotal ?? 0) / 100m;
                    orderHeader.ShippingCost = (session.TotalDetails?.AmountShipping ?? 0) / 100m;
                    orderHeader.TotalCost = (session.AmountTotal ?? 0) / 100m;

                    if (session.PaymentStatus == "paid")
                    {
                        orderHeader.OrderStatus = SD.OrderStatus_Approved;
                        orderHeader.PaymentStatus = SD.PaymentStatus_Paid;

                        foreach (var orderItem in orderHeader.OrderItems)
                        {
                            var product = orderItem.Product;
                            if (product == null)
                            {
                                continue;
                            }

                            product.Quantity -= orderItem.Quantity;
                            if (product.Quantity < 0)
                            {
                                product.Quantity = 0;
                            }

                            await _unitOfWork.Product.UpdateAsync(product);
                        }

                        var cartItems = await _unitOfWork.CartItem.GetAllAsync(ci => ci.CustomerId == orderHeader.CustomerId);
                        _unitOfWork.CartItem.RemoveRange(cartItems);
                    }
                    else
                    {
                        orderHeader.OrderStatus = SD.OrderStatus_Cancelled;
                        orderHeader.PaymentStatus = SD.PaymentStatus_Failed;
                    }

                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);
                    await _unitOfWork.SaveAsync();
                }
                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpGet("ConfirmOrder/{orderHeaderId:int}")]
        public async Task<IActionResult> ConfirmOrder([FromRoute] int orderHeaderId)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                oh => oh.Id == orderHeaderId);

            if (orderHeader == null)
            {
                return NotFound();
            }

            HttpContext.Session.SetInt32(SD.SessionCartCount, 0);

            if (orderHeader.PaymentStatus == SD.PaymentStatus_Paid)
            {
                TempData["success"] = "Payment completed successfully! Your order has been confirmed.";
            }
            else
            {
                TempData["error"] = "Payment status is pending or failed. Please contact support if you completed the payment.";
            }

            return View("Areas/Customer/Views/Cart/ConfirmOrder.cshtml", orderHeaderId);
        }
    }
}
