using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_SuperAdmin)]
    [Area("Admin")]
    [Route("Admin/Vendor")]
    public class VendorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VendorController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllVendors")]
        public async Task<IActionResult> GetAllVendors(
            [FromQuery] string searchString = "",
            [FromQuery] string lockoutStatus = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 5;
            var (vendors, totalItems) =
                await _unitOfWork.Vendor.GetFilteredVendorsAsync(
                    searchString, lockoutStatus, currentSort, currentPage, pageSize);

            var vendorRows = new List<VendorRowVM>();
            foreach (var v in vendors)
            {
                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(v.AppUser) ? "Locked" : "Unlocked";
                vendorRows.Add(new VendorRowVM
                {
                    Vendor = v,
                    DisplayImageUrl = string.IsNullOrEmpty(v.ImageUrl) ? "/images/vendors/default-vendor.png" : v.ImageUrl,
                    AppUserEmail = v.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus,
                });
            }

            var vm = new VendorListVM
            {
                Vendors = vendorRows,
                SourceView = "GetAllVendors",

                SearchString = searchString,

                LockoutStatus = lockoutStatus,
                LockoutStatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Locked", Text = "Locked Accounts", Selected = lockoutStatus == "Locked" },
                    new SelectListItem { Value = "Unlocked", Text = "Unlocked Accounts", Selected = lockoutStatus == "Unlocked" }
                },

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Admin/Views/Vendor/GetAllVendors.cshtml", vm);
        }

        [HttpGet("GetVendor/{id:int}")]
        public async Task<IActionResult> GetVendor(
            [FromRoute] int id, [FromQuery] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            vm.Vendor = vendor;
            vm.SourceView = "GetVendor";
            vm.DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl;
            vm.AppUserEmail = vendor.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(vendor.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Vendor/GetVendor.cshtml", vm);
        }

        [HttpGet("RegisterVendor")]
        public IActionResult RegisterVendor([FromQuery] RegisterVendorVM vm)
        {
            return View("Areas/Admin/Views/Vendor/RegisterVendor.cshtml", vm);
        }

        [HttpPost("RegisterVendor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterVendorConfirmed([FromForm] RegisterVendorVM vm)
        {
            if (vm.Vendor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Vendor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Vendor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Vendor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Vendor.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Vendor/RegisterVendor.cshtml", vm);
            }

            var appUser = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email
            };

            var result = await _userManager.CreateAsync(appUser, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Vendor/RegisterVendor.cshtml", vm);
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Vendor);

            if (vm.Vendor.ImageFile != null && vm.Vendor.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "vendors");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Vendor.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Vendor.ImageFile.CopyToAsync(fileStream);
                }

                vm.Vendor.ImageUrl = "/images/vendors/" + fileName + extension;
            }

            vm.Vendor.AppUserId = appUser.Id;

            await _unitOfWork.Vendor.AddAsync(vm.Vendor);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Vendor registered successfully.";
            return RedirectToAction("GetAllVendors", "Vendor", new { area = "Admin" });
        }

        [HttpGet("EditVendor/{id:int}")]
        public async Task<IActionResult> EditVendor(
            [FromRoute] int id, [FromQuery] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == id);
            if (vendor == null)
            {
                return NotFound();
            }

            vm.Vendor = vendor;
            vm.DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl;
            vm.HasCustomImage = !string.IsNullOrEmpty(vendor.ImageUrl);

            return View("Areas/Admin/Views/Vendor/EditVendor.cshtml", vm);
        }

        [HttpPost("EditVendor/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVendorConfirmed([FromForm] VendorVM vm)
        {
            if (vm.Vendor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Vendor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Vendor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Vendor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Vendor.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Vendor/EditVendor.cshtml", vm);
            }

            if (vm.Vendor.ImageFile != null && vm.Vendor.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "vendors");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Vendor.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Vendor.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Vendor.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Vendor.ImageFile.CopyToAsync(fileStream);
                }

                vm.Vendor.ImageUrl = "/images/vendors/" + fileName + extension;
            }

            await _unitOfWork.Vendor.UpdateAsync(vm.Vendor);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Vendor updated successfully.";
            return RedirectToAction("GetVendor", "Vendor", new
            {
                area = "Admin",
                id = vm.Vendor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteVendorImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVendorImage([FromForm] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(v => v.Id == vm.Vendor.Id);
            if (vendor == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(vendor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, vendor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                vendor.ImageUrl = null;
                await _unitOfWork.Vendor.UpdateAsync(vendor);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditVendor", "Vendor", new
            {
                area = "Admin",
                id = vm.Vendor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteVendor/{id:int}")]
        public async Task<IActionResult> DeleteVendor([FromRoute] int id, [FromQuery] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == id, includeProperties: "AppUser"); 
            if (vendor == null)
            {
                return NotFound();
            }

            vm.Vendor = vendor;
            vm.DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl;
            vm.AppUserEmail = vendor.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(vendor.AppUser) ? "Locked" : "Unlocked";
            
            return View("Areas/Admin/Views/Vendor/DeleteVendor.cshtml", vm);
        }

        [HttpPost("DeleteVendor/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVendorConfirmed([FromForm] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == vm.Vendor.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(vendor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, vendor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Vendor.Remove(vendor);
            await _userManager.DeleteAsync(vendor.AppUser);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Vendor account has been deleted successfully.";
            return RedirectToAction("GetAllVendors", "Vendor", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ToggleVendorLock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVendorLock([FromForm] VendorVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
               v => v.Id == vm.Vendor.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            if (await _userManager.IsLockedOutAsync(vendor.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(vendor.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(vendor.AppUser);
                TempData["success"] = $"Vendor has been unlocked successfully.";
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(vendor.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));
                TempData["success"] = $"Vendor has been locked successfully.";
            }

            await _unitOfWork.SaveAsync();

            return RedirectToAction(vm.SourceView, "Vendor", new
            {
                area = "Admin",
                id = vm.Vendor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("ResetVendorPassword/{id:int}")]
        public async Task<IActionResult> ResetVendorPassword([FromRoute] int id, [FromQuery] ResetPasswordVM vm)
        {
            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            vm.Id = vendor.Id;
            vm.AppUserEmail = vendor.AppUser.Email;
            return View("Areas/Admin/Views/Vendor/ResetVendorPassword.cshtml", vm);
        }

        [HttpPost("ResetVendorPassword/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetVendorPasswordConfirmed([FromForm] ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Vendor/ResetVendorPassword.cshtml", vm);
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.Id == vm.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(vendor.AppUser);
            var result = await _userManager.ResetPasswordAsync(vendor.AppUser, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Vendor/ResetVendorPassword.cshtml", vm);
            }

            await _userManager.UpdateSecurityStampAsync(vendor.AppUser);

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction(vm.SourceView, "Vendor", new
            {
                area = "Admin",
                id = vm.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
