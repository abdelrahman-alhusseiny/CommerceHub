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
    [Route("Customer/Shop")]
    public class ShopController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public ShopController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] string searchString = "",
            [FromQuery] int? categoryId = null,
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (products, totalItems) =
                await _unitOfWork.Product.GetFilteredProductsAsync(
                null, searchString, categoryId, currentSort, currentPage, pageSize);

            var productRows = new List<ProductRowVM>();
            foreach (var p in products)
            {
                productRows.Add(new ProductRowVM
                {
                    Product = p,
                    DisplayImageUrl = string.IsNullOrEmpty(p.ProductImages.FirstOrDefault()?.ImageUrl) ? "/images/products/default-product.png" : p.ProductImages.FirstOrDefault()?.ImageUrl,
                    DisplayPrice = p.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG")),
                    DisplayOldPrice = p.OldPrice.HasValue ? p.OldPrice.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "",
                    CategoryName = p.Category.Name,
                });
            }

            var vm = new ProductListVM
            {
                Products = productRows,

                SearchString = searchString,

                CategoryId = categoryId,
                CategoryList = (await _unitOfWork.Category.GetAllAsync()).Select(
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == categoryId
                    }),

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                PriceSort = currentSort == "price" ? "price_desc" : "price",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Customer/Views/Shop/GetAllProducts.cshtml", vm);
        }

        [HttpGet("GetProduct/{id:int}")]
        public async Task<IActionResult> GetProduct(
            [FromRoute] int id, [FromQuery] ProductVM vm)
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

            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "Category,ProductImages,Vendor");

            if (product == null)
            {
                return NotFound();
            }

            var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.CustomerId == customer.Id && ci.ProductId == product.Id);

            var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
               wi => wi.CustomerId == customer.Id && wi.ProductId == product.Id);

            vm.Product = product;

            vm.ProductImageUrls = product.ProductImages != null && product.ProductImages.Any()
                ? product.ProductImages.Select(pi => pi.ImageUrl).ToList()
                : new List<string> { "/images/products/default-product.png" };
            vm.DisplayPrice = product.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG"));
            vm.DisplayOldPrice = product.OldPrice.HasValue ? product.OldPrice.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "";

            vm.CategoryName = product.Category.Name;
            vm.CategoryDescription = product.Category.Description;

            vm.VendorImageUrl = string.IsNullOrEmpty(product.Vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : product.Vendor.ImageUrl;
            vm.VendorStoreName = product.Vendor.StoreName;
            vm.VendorPhoneNumber = product.Vendor.PhoneNumber;
            vm.VendorStoreAddress = product.Vendor.StoreAddress;
            vm.VendorStoreDescription = product.Vendor.StoreDescription;

            vm.IsInCart = cartItem != null;
            vm.IsInWishlist = wishlistItem != null;

            return View("Areas/Customer/Views/Shop/GetProduct.cshtml", vm);
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

        [HttpPost("ToggleWishlist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist([FromForm] ProductVM vm)
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

            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == vm.Product.Id);
            if (product == null)
            {
                return NotFound();
            }

            var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                wi => wi.CustomerId == customer.Id && wi.ProductId == product.Id);

            if (wishlistItem == null)
            {
                var newWishlistItem = new WishlistItem
                {
                    CustomerId = customer.Id,
                    ProductId = product.Id
                };

                await _unitOfWork.WishlistItem.AddAsync(newWishlistItem);

                var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                    ci => ci.CustomerId == customer.Id && ci.ProductId == product.Id
                );
                if (cartItem != null)
                {
                    _unitOfWork.CartItem.Remove(cartItem);
                }
                await _unitOfWork.SaveAsync();

                await UpdateCartSession();

                TempData["success"] = $"{product.Name} has been added to your wishlist.";
            }
            else
            {
                _unitOfWork.WishlistItem.Remove(wishlistItem);
                await _unitOfWork.SaveAsync();

                TempData["info"] = $"{product.Name} has been removed from your wishlist.";
            }

            return RedirectToAction("GetProduct", "Shop", new
            {
                area = "Customer",
                id = vm.Product.Id,
                searchString = vm.SearchString,
                categoryId = vm.CategoryId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("AddToCart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromForm] ProductVM vm)
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

            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == vm.Product.Id);
            if (product == null)
            {
                return NotFound();
            }

            var existingCartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                ci => ci.CustomerId == customer.Id && ci.ProductId == product.Id);

            if (existingCartItem == null)
            {
                if (product.Quantity > 0)
                {
                    var cartItem = new CartItem
                    {
                        Quantity = 1,
                        CustomerId = customer.Id,
                        ProductId = product.Id
                    };

                    await _unitOfWork.CartItem.AddAsync(cartItem);

                    var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                        wi => wi.CustomerId == customer.Id && wi.ProductId == product.Id
                    );
                    if (wishlistItem != null)
                    {
                        _unitOfWork.WishlistItem.Remove(wishlistItem);
                    }
                    await _unitOfWork.SaveAsync();

                    await UpdateCartSession();

                    TempData["success"] = $"{product.Name} has been added to your cart.";
                }
                else
                {
                    TempData["error"] = "This product is out of stock.";
                }
            }
            else
            {
                if (existingCartItem.Quantity + 1 <= product.Quantity)
                {
                    existingCartItem.Quantity += 1;
                    await _unitOfWork.CartItem.UpdateAsync(existingCartItem);
                    TempData["success"] = $"{product.Name} quantity updated in your cart.";
                }
                else
                {
                    TempData["error"] = "Not enough stock available.";
                }
            }

            return RedirectToAction("GetProduct", "Shop", new
            {
                area = "Customer",
                id = vm.Product.Id,
                searchString = vm.SearchString,
                categoryId = vm.CategoryId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
