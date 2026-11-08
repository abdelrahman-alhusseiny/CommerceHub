using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_SuperAdmin)]
    [Area("Admin")]
    [Route("Admin/Category")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories(
            [FromQuery] string searchString = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 5;
            var (categories, totalItems) =
                await _unitOfWork.Category.GetFilteredCategoriesAsync(
                    searchString, currentSort, currentPage, pageSize);

            var categoryRows = new List<CategoryRowVM>();
            foreach (var c in categories)
            {
                categoryRows.Add(new CategoryRowVM
                {
                    Category = c,
                    DisplayImageUrl = string.IsNullOrEmpty(c.ImageUrl) ? "/images/categories/default-category.png" : c.ImageUrl,
                });
            }

            var vm = new CategoryListVM
            {
                Categories = categoryRows,
                SourceView = "GetAllCategories",

                SearchString = searchString,

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Admin/Views/Category/GetAllCategories.cshtml", vm);
        }

        [HttpGet("GetCategory/{id:int}")]
        public async Task<IActionResult> GetCategory(
            [FromRoute] int id, [FromQuery] CategoryVM vm)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            vm.Category = category;
            vm.SourceView = "GetCategory";
            vm.DisplayImageUrl = string.IsNullOrEmpty(category.ImageUrl) ? "/images/categories/default-category.png" : category.ImageUrl;

            return View("Areas/Admin/Views/Category/GetCategory.cshtml", vm);
        }

        [HttpGet("CreateCategory")]
        public IActionResult CreateCategory([FromQuery] CategoryVM vm)
        {
            return View("Areas/Admin/Views/Category/CreateCategory.cshtml", vm);
        }

        [HttpPost("CreateCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategoryConfirmed([FromForm] CategoryVM vm)
        {
            if (await _unitOfWork.Category.IsNameExistsAsync(vm.Category.Name))
            {
                ModelState.AddModelError("Category.Name", "This category name already exists");
            }

            if (vm.Category.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Category.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Category.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Category.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Category.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Category/CreateCategory.cshtml", vm);
            }

            if (vm.Category.ImageFile != null && vm.Category.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "categories");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Category.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Category.ImageFile.CopyToAsync(fileStream);
                }

                vm.Category.ImageUrl = "/images/categories/" + fileName + extension;
            }

            await _unitOfWork.Category.AddAsync(vm.Category);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Category created successfully.";
            return RedirectToAction("GetAllCategories", "Category", new { area = "Admin" });
        }

        [HttpGet("EditCategory/{id:int}")]
        public async Task<IActionResult> EditCategory(
            [FromRoute] int id, [FromQuery] CategoryVM vm)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            vm.Category = category;
            vm.DisplayImageUrl = string.IsNullOrEmpty(category.ImageUrl) ? "/images/categories/default-category.png" : category.ImageUrl;
            vm.HasCustomImage = !string.IsNullOrEmpty(category.ImageUrl);

            return View("Areas/Admin/Views/Category/EditCategory.cshtml", vm);
        }

        [HttpPost("EditCategory/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategoryConfirmed([FromForm] CategoryVM vm)
        {
            if (await _unitOfWork.Category.IsNameExistsAsync(
                vm.Category.Name, vm.Category.Id))
            {
                ModelState.AddModelError("Category.Name", "This category name is already exist");
            }

            if (vm.Category.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Category.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Category.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Category.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Category.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Category/EditCategory.cshtml", vm);
            }

            if (vm.Category.ImageFile != null && vm.Category.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "categories");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Category.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Category.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Category.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Category.ImageFile.CopyToAsync(fileStream);
                }

                vm.Category.ImageUrl = "/images/categories/" + fileName + extension;
            }

            await _unitOfWork.Category.UpdateAsync(vm.Category);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Category updated successfully.";
            return RedirectToAction(vm.SourceView, "Category", new
            {
                area = "Admin",
                id = vm.Category.Id,
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteCategoryImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryImage([FromForm] CategoryVM vm)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == vm.Category.Id);
            if (category == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, category.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                category.ImageUrl = null;
                await _unitOfWork.Category.UpdateAsync(category);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditCategory", "Category", new
            {
                area = "Admin",
                id = vm.Category.Id,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteCategory/{id:int}")]
        public async Task<IActionResult> DeleteCategory([FromRoute] int id, [FromQuery] CategoryVM vm)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            vm.Category = category;
            vm.DisplayImageUrl = string.IsNullOrEmpty(category.ImageUrl) ? "/images/categories/default-category.png" : category.ImageUrl;

            return View("Areas/Admin/Views/Category/DeleteCategory.cshtml", vm);
        }

        [HttpPost("DeleteCategory/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed([FromForm] CategoryVM vm)
        {
            var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
                c => c.Id == vm.Category.Id);
            if (category == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, category.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Category.Remove(category);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Category has been deleted successfully.";
            return RedirectToAction("GetAllCategories", "Category", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
