using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.WebUI.Areas.Customer.Controllers
{
    [Authorize(Roles = SD.Role_Customer)]
    [Area("Customer")]
    [Route("Customer/Profile")]
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

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            var vm = new CustomerVM
            {
                Customer = customer,
                DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl,
                AppUserEmail = customer.AppUser.Email
            };

            return View("Areas/Customer/Views/Profile/GetProfile.cshtml", vm);
        }

        [HttpGet("EditProfile")]
        public async Task<IActionResult> EditProfile()
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

            var vm = new CustomerVM
            {
                Customer = customer,
                GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                },
                DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl,
                HasCustomImage = !string.IsNullOrEmpty(customer.ImageUrl),
            };

            return View("Areas/Customer/Views/Profile/EditProfile.cshtml", vm);
        }

        [HttpPost("EditProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfileConfirmed([FromForm] CustomerVM vm)
        {
            if (vm.Customer.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension =
                    Path.GetExtension(vm.Customer.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError(
                        "Customer.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Customer.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(
                        "Customer.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };
             
                return View("Areas/Customer/Views/Profile/EditProfile.cshtml", vm);
            }

            if (vm.Customer.ImageFile != null && vm.Customer.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "customers");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Customer.ImageUrl))
                {
                    string oldImagePath = Path.Combine(
                        wwwRootPath, vm.Customer.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Customer.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Customer.ImageFile.CopyToAsync(fileStream);
                }

                vm.Customer.ImageUrl = "/images/customers/" + fileName + extension;
            }

            await _unitOfWork.Customer.UpdateAsync(vm.Customer);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Profile updated successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Customer" });
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

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id);
            if (customer == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(customer.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, customer.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                customer.ImageUrl = null;
                await _unitOfWork.Customer.UpdateAsync(customer);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditProfile", "Profile", new { area = "Customer" });
        }

        [HttpGet("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            var vm = new CustomerVM
            {
                Customer = customer,
                DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl,
                AppUserEmail = customer.AppUser.Email
            };

            return View("Areas/Customer/Views/Profile/DeleteAccount.cshtml", vm);
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

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id);
            if (customer == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(customer.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, customer.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Customer.Remove(customer);
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

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            var vm = new ChangePasswordVM
            {
                AppUserEmail = customer.AppUser.Email
            };

            return View("Areas/Customer/Views/Profile/ChangeAccountPassword.cshtml", vm);
        }

        [HttpPost("ChangeAccountPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccountPasswordConfirmed([FromForm] ChangePasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Customer/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

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
           
            var result = await _userManager.ChangePasswordAsync(customer.AppUser, vm.CurrentPassword, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Areas/Customer/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

            await _signInManager.RefreshSignInAsync(customer.AppUser);

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Customer" });
        }
    }
}

