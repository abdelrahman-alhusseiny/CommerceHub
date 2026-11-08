using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;

namespace ECommerce.WebUI.Areas.Vendor.Controllers
{
    [Authorize(Roles = SD.Role_Vendor)]
    [Area("Vendor")]
    [Route("Vendor/Profile")]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            var vm = new VendorVM
            {
                Vendor = vendor,
                DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl,
                AppUserEmail = vendor.AppUser.Email
            };

            return View("Areas/Vendor/Views/Profile/GetProfile.cshtml", vm);
        }

        [HttpGet("EditProfile")]
        public async Task<IActionResult> EditProfile()
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

            var vm = new VendorVM
            {
                Vendor = vendor,
                DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl,
                HasCustomImage = !string.IsNullOrEmpty(vendor.ImageUrl)
            };

            return View("Areas/Vendor/Views/Profile/EditProfile.cshtml", vm);
        }

        [HttpPost("EditProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfileConfirmed([FromForm] VendorVM vm)
        {
            if (vm.Vendor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension =
                    Path.GetExtension(vm.Vendor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError(
                        "Vendor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Vendor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(
                        "Vendor.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Vendor/Views/Profile/EditProfile.cshtml", vm);
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
                    string oldImagePath = Path.Combine(
                        wwwRootPath, vm.Vendor.ImageUrl.TrimStart('/'));

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

            TempData["success"] = "Profile updated successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Vendor" });
        }

        [HttpPost("DeleteProfileImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
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

            if (!string.IsNullOrEmpty(vendor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, vendor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                vendor.ImageUrl = null;
                await _unitOfWork.Vendor.UpdateAsync(vendor);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditProfile", "Profile", new { area = "Vendor" });
        }

        [HttpGet("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            var vm = new VendorVM
            {
                Vendor = vendor,
                DisplayImageUrl = string.IsNullOrEmpty(vendor.ImageUrl) ? "/images/vendors/default-vendor.png" : vendor.ImageUrl,
                AppUserEmail = vendor.AppUser.Email
            };

            return View("Areas/Vendor/Views/Profile/DeleteAccount.cshtml", vm);
        }

        [HttpPost("DeleteAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(vendor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, vendor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Vendor.Remove(vendor);
            await _userManager.DeleteAsync(appUser);
            await _unitOfWork.SaveAsync();
            await _signInManager.SignOutAsync();

            TempData["success"] = "Your account has been deleted successfully.";
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet("ChangeAccountPassword")]
        public async Task<IActionResult> ChangeAccountPassword()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            var vm = new ChangePasswordVM
            {
                AppUserEmail = vendor.AppUser.Email
            };

            return View("Areas/Vendor/Views/Profile/ChangeAccountPassword.cshtml", vm);
        }

        [HttpPost("ChangeAccountPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccountPasswordConfirmed([FromForm] ChangePasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Vendor/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var vendor = await _unitOfWork.Vendor.GetFirstOrDefaultAsync(
                v => v.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (vendor == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(vendor.AppUser, vm.CurrentPassword, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Areas/Vendor/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

            await _signInManager.RefreshSignInAsync(vendor.AppUser);

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Vendor" });
        }
    }
}
