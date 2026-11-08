using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.WebUI.Areas.Vendor.Controllers
{
    [Authorize(Roles = SD.Role_Vendor)]
    [Area("Vendor")]
    [Route("Vendor/Product")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(
            IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] string searchString = "",
            [FromQuery] int? categoryId = null,
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id);
            if (vendor == null)
            {
                return NotFound();
            }

            int pageSize = 5;
            var (products, totalItems) =
                await _unitOfWork.Product.GetFilteredProductsAsync(
                vendor.Id, searchString, categoryId, currentSort, currentPage, pageSize);

            var productRows = new List<ProductRowVM>();
            foreach (var p in products)
            {
                productRows.Add(new ProductRowVM
                {
                    Product = p,
                    DisplayImageUrl = string.IsNullOrEmpty(p.ProductImages.FirstOrDefault()?.ImageUrl) ? "/images/products/default-product.png" : p.ProductImages.FirstOrDefault()?.ImageUrl,
                    DisplayPrice = p.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG")),
                    CategoryName = p.Category.Name
                });
            }

            var vm = new ProductListVM
            {
                Products = productRows,
                SourceView = "GetAllProducts",

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
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Vendor/Views/Product/GetAllProducts.cshtml", vm);
        }

        [HttpGet("GetProduct/{id:int}")]
        public async Task<IActionResult> GetProduct(
            [FromRoute] int id, [FromQuery] ProductVM vm)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "Category,ProductImages");

            if (product == null)
            {
                return NotFound();
            }

            vm.Product = product;
            vm.SourceView = "GetProduct";
            vm.ProductImageUrls = product.ProductImages != null && product.ProductImages.Any()
                ? product.ProductImages.Select(pi => pi.ImageUrl).ToList()
                : new List<string> { "/images/products/default-product.png" };
            vm.DisplayPrice = product.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG"));
            vm.DisplayOldPrice = product.OldPrice.HasValue ? product.OldPrice.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "No previous price";
            vm.CategoryName = product.Category.Name;

            return View("Areas/Vendor/Views/Product/GetProduct.cshtml", vm);
        }

        [HttpGet("CreateProduct")]
        public async Task<IActionResult> CreateProduct([FromQuery] ProductVM vm)
        {
            vm.CategoryList = (await _unitOfWork.Category.GetAllAsync()).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                });

            return View("Areas/Vendor/Views/Product/CreateProduct.cshtml", vm);
        }

        [HttpPost("CreateProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductConfirmed([FromForm] ProductVM vm)
        {
            if (await _unitOfWork.Product.IsIsbnExistsAsync(vm.Product.ISBN))
            {
                ModelState.AddModelError("Product.ISBN", "This ISBN is already exist");
            }

            if (!ModelState.IsValid)
            {
                vm.CategoryList = (await _unitOfWork.Category.GetAllAsync()).Select(
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    });

                return View("Areas/Vendor/Views/Product/CreateProduct.cshtml", vm);
            }

            vm.Product.ProductImages = new List<ProductImage>();

            if (vm.ImageFiles != null && vm.ImageFiles.Any())
            {
                foreach (var file in vm.ImageFiles)
                {
                    if (file.Length == 0)
                        continue;

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        continue;

                    if (file.Length > 2 * 1024 * 1024)
                        continue;

                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string folderPath = Path.Combine(wwwRootPath, "images", "products");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = Guid.NewGuid().ToString("N");
                    string fullPath = Path.Combine(folderPath, fileName + extension);

                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var productImage = new ProductImage
                    {
                        ImageUrl = "/images/products/" + fileName + extension
                    };

                    vm.Product.ProductImages.Add(productImage);
                }
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id);
            if (vendor == null)
            {
                return NotFound();
            }

            vm.Product.VendorId = vendor.Id;

            await _unitOfWork.Product.AddAsync(vm.Product);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Product created successfully.";
            return RedirectToAction("GetAllProducts", "Product", new { area = "Vendor" });
        }

        [HttpGet("EditProduct/{id:int}")]
        public async Task<IActionResult> EditProduct(
            [FromRoute] int id, [FromQuery] ProductVM vm)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "ProductImages");

            if (product == null)
            {
                return NotFound();
            }

            vm.Product = product;
            vm.CategoryList = (await _unitOfWork.Category.GetAllAsync()).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                });
            vm.ProductImagePairs = product.ProductImages != null && product.ProductImages.Any()
                ? product.ProductImages
                    .Select(img => new ProductImageEntry
                    {
                        ImageId = img.Id,
                        ImageUrl = img.ImageUrl
                    })
                    .ToList()
                : new List<ProductImageEntry>
                {
                    new ProductImageEntry
                    {
                        ImageId = 0, // or leave null if not needed
                        ImageUrl = "/images/products/default-product.png"
                    }
                };
            vm.HasCustomImage = product.ProductImages.Any();

            return View("Areas/Vendor/Views/Product/EditProduct.cshtml", vm);
        }

        [HttpPost("EditProduct/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductConfirmed([FromForm] ProductVM vm)
        {
            if (await _unitOfWork.Product.IsIsbnExistsAsync(
                vm.Product.ISBN, vm.Product.Id))
            {
                ModelState.AddModelError("Product.ISBN", "This ISBN is already exist");
            }

            if (!ModelState.IsValid)
            {
                vm.CategoryList = (await _unitOfWork.Category.GetAllAsync()).Select(
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    });

                return View("Areas/Vendor/Views/Product/EditProduct.cshtml", vm);
            }

            if (vm.ImageFiles != null && vm.ImageFiles.Any())
            {
                foreach (var file in vm.ImageFiles)
                {
                    if (file.Length == 0)
                        continue;

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        continue;

                    if (file.Length > 2 * 1024 * 1024)
                        continue;

                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string folderPath = Path.Combine(wwwRootPath, "images", "products");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = Guid.NewGuid().ToString("N");
                    string fullPath = Path.Combine(folderPath, fileName + extension);

                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var productImage = new ProductImage
                    {
                        ImageUrl = "/images/products/" + fileName + extension
                    };

                    vm.Product.ProductImages.Add(productImage);
                }
            }

            await _unitOfWork.Product.UpdateAsync(vm.Product);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Product updated successfully.";
            return RedirectToAction(vm.SourceView, "Product", new
            {
                area = "Vendor",
                id = vm.Product.Id,
                searchString = vm.SearchString,
                categoryId = vm.CategoryId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteProductImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductImage([FromForm] ProductVM vm)
        {
            var images = await _unitOfWork.ProductImage.GetImagesByIdsAsync(vm.ProductImageIds);
            if (images != null && images.Any())
            {
                foreach (var image in images)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string oldImagePath = Path.Combine(
                        wwwRootPath, image.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.RemoveRange(images);
                await _unitOfWork.SaveAsync();

                TempData["success"] = "Images deleted successfully.";
            }
            else
            {
                TempData["error"] = "No images found to delete.";
            }

            return RedirectToAction("EditProduct", "Product", new
            {
                area = "Vendor",
                id = vm.Product.Id,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                categoryId = vm.CategoryId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteProduct/{id:int}")]
        public async Task<IActionResult> DeleteProduct(
            [FromRoute] int id, [FromQuery] ProductVM vm)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "Category,ProductImages");

            if (product == null)
            {
                return NotFound();
            }

            vm.Product = product;
            vm.ProductImageUrls = product.ProductImages != null && product.ProductImages.Any()
                ? product.ProductImages.Select(pi => pi.ImageUrl).ToList()
                : new List<string> { "/images/products/default-product.png" }; vm.HasCustomImage = product.ProductImages.Any();
            vm.DisplayPrice = product.Price.ToString("C", new System.Globalization.CultureInfo("ar-EG"));
            vm.DisplayOldPrice = product.OldPrice.HasValue ? product.OldPrice.Value.ToString("C", new System.Globalization.CultureInfo("ar-EG")) : "No previous price";
            vm.CategoryName = product.Category.Name;

            return View("Areas/Vendor/Views/Product/DeleteProduct.cshtml", vm);
        }

        [HttpPost("DeleteProduct/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductConfirmed([FromForm] ProductVM vm)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                p => p.Id == vm.Product.Id, includeProperties: "ProductImages");

            if (product == null)
            {
                return NotFound();
            }

            if (product.ProductImages != null && product.ProductImages.Any())
            {
                foreach (var image in product.ProductImages)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string oldImagePath = Path.Combine(
                        wwwRootPath, image.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
            }

            _unitOfWork.Product.Remove(product);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Product deleted successfully.";
            return RedirectToAction("GetAllProducts", "Product", new
            {
                area = "Vendor",
                searchString = vm.SearchString,
                categoryId = vm.CategoryId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
