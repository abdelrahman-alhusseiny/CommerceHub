using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebUI.Areas.Customer.Controllers
{
    [Authorize(Roles = SD.Role_Customer)]
    [Area("Customer")]
    [Route("Customer/Wishlist")]
    public class WishlistController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public WishlistController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllWishlistItems")]
        public async Task<IActionResult> GetAllWishlistItems()
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

            var wishlistItems = await _unitOfWork.WishlistItem.GetAllAsync(wi => wi.CustomerId == customer.Id, includeProperties: "Product,Product.Category,Product.ProductImages");

            var wishlistItemRows = new List<WishlistRowVM>();
            foreach (var wi in wishlistItems)
            {
                wishlistItemRows.Add(new WishlistRowVM
                {
                    WishlistItem = wi,
                    ProductImageUrl = string.IsNullOrEmpty(wi.Product.ProductImages.FirstOrDefault()?.ImageUrl) ? "/images/products/default-product.png" : wi.Product.ProductImages.FirstOrDefault()?.ImageUrl,
                    ProductName = wi.Product.Name,
                    ProductCategoryName = wi.Product.Category.Name,
                    ProductUnitPrice = wi.Product.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG")),
                    ProductQuantity = wi.Product.Quantity
                });
            }

            var vm = new WishlistListVM
            {
                WishlistItems = wishlistItemRows
            };

            return View("Areas/Customer/Views/Wishlist/GetAllWishlistItems.cshtml", vm);
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

        [HttpPost("RemoveFromWishlist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist([FromForm] int wishlistItemId)
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

            var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                wi => wi.Id == wishlistItemId && wi.CustomerId == customer.Id, includeProperties: "Product");
            if (wishlistItem == null)
            {
                return NotFound();
            }

            _unitOfWork.WishlistItem.Remove(wishlistItem);
            await _unitOfWork.SaveAsync();

            TempData["info"] = $"{wishlistItem.Product.Name} has been removed from your wishlist.";
            return RedirectToAction("GetAllWishlistItems", "Wishlist", new { area = "Customer" });
        }

        [HttpPost("AddToCart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromForm] int wishlistItemId)
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

            var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                wi => wi.Id == wishlistItemId && wi.CustomerId == customer.Id, includeProperties: "Product");
            if (wishlistItem == null)
            {
                return NotFound();
            }

            var existingCartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.CustomerId == customer.Id && ci.ProductId == wishlistItem.ProductId);

            if (existingCartItem == null)
            {
                if (wishlistItem.Product.Quantity > 0)
                {
                    var cartItem = new CartItem
                    {
                        Quantity = 1,
                        CustomerId = customer.Id,
                        ProductId = wishlistItem.ProductId
                    };

                    await _unitOfWork.CartItem.AddAsync(cartItem);
                    _unitOfWork.WishlistItem.Remove(wishlistItem);
                    await _unitOfWork.SaveAsync();

                    await UpdateCartSession();

                    TempData["success"] = $"{wishlistItem.Product.Name} has been added to your cart.";
                }
                else
                {
                    TempData["error"] = "This product is out of stock.";
                }
            }
            else
            {
                if (existingCartItem.Quantity + 1 <= wishlistItem.Product.Quantity)
                {
                    existingCartItem.Quantity += 1;
                    await _unitOfWork.CartItem.UpdateAsync(existingCartItem);
                    _unitOfWork.WishlistItem.Remove(wishlistItem);
                    await _unitOfWork.SaveAsync();

                    TempData["success"] = $"{wishlistItem.Product.Name} quantity updated in your cart.";
                }
                else
                {
                    TempData["error"] = "Not enough stock available.";
                }
            }

            return RedirectToAction("GetAllWishlistItems", "Wishlist", new { area = "Customer" });
        }
    }
}
