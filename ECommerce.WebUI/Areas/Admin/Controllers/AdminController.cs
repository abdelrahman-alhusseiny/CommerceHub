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

namespace ECommerce.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.Role_SuperAdmin)]
    [Area("Admin")]
    [Route("Admin/Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllAdmins")]
        public async Task<IActionResult> GetAllAdmins(
            [FromQuery] string searchString = "",
            [FromQuery] string lockoutStatus = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 5;
            var (admins, totalItems) =
                await _unitOfWork.Admin.GetFilteredAdminsAsync(
                    searchString, lockoutStatus, currentSort, currentPage, pageSize);

            var adminRows = new List<AdminRowVM>();
            foreach (var a in admins)
            {
                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(a.AppUser) ? "Locked" : "Unlocked";
                adminRows.Add(new AdminRowVM
                {
                    Admin = a,
                    DisplayImageUrl = string.IsNullOrEmpty(a.ImageUrl) ? "/images/admins/default-admin.png" : a.ImageUrl,
                    AppUserEmail = a.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus
                });
            }

            var vm = new AdminListVM
            {
                Admins = adminRows,
                SourceView = "GetAllAdmins",

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

            return View("Areas/Admin/Views/Admin/GetAllAdmins.cshtml", vm);
        }

        [HttpGet("GetAdmin/{id:int}")]
        public async Task<IActionResult> GetAdmin(
            [FromRoute] int id, [FromQuery] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            vm.Admin = admin;
            vm.SourceView = "GetAdmin";
            vm.DisplayImageUrl = string.IsNullOrEmpty(admin.ImageUrl) ? "/images/admins/default-admin.png" : admin.ImageUrl;
            vm.AppUserEmail = admin.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(admin.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Admin/GetAdmin.cshtml", vm);
        }

        [HttpGet("RegisterAdmin")]
        public IActionResult RegisterAdmin([FromQuery] RegisterAdminVM vm)
        {
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" },
            };

            return View("Areas/Admin/Views/Admin/RegisterAdmin.cshtml", vm);
        }

        [HttpPost("RegisterAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdminConfirmed([FromForm] RegisterAdminVM vm)
        {
            if (vm.Admin.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Admin.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Admin.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Admin.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Admin.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };

                return View("Areas/Admin/Views/Admin/RegisterAdmin.cshtml", vm);
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

                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };

                return View("Areas/Admin/Views/Admin/RegisterAdmin.cshtml", vm);
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Admin);

            if (vm.Admin.ImageFile != null && vm.Admin.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "admins");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Admin.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Admin.ImageFile.CopyToAsync(fileStream);
                }

                vm.Admin.ImageUrl = "/images/admins/" + fileName + extension;
            }

            vm.Admin.AppUserId = appUser.Id;

            await _unitOfWork.Admin.AddAsync(vm.Admin);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Admin registered successfully.";
            return RedirectToAction("GetAllAdmins", "Admin", new { area = "Admin" });
        }

        [HttpGet("EditAdmin/{id:int}")]
        public async Task<IActionResult> EditAdmin(
            [FromRoute] int id, [FromQuery] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == id);
            if (admin == null)
            {
                return NotFound();
            }

            vm.Admin = admin;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
            vm.DisplayImageUrl = string.IsNullOrEmpty(admin.ImageUrl) ? "/images/admins/default-admin.png" : admin.ImageUrl;
            vm.HasCustomImage = !string.IsNullOrEmpty(admin.ImageUrl);

            return View("Areas/Admin/Views/Admin/EditAdmin.cshtml", vm);
        }

        [HttpPost("EditAdmin/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdminConfirmed([FromForm] AdminVM vm)
        {
            if (vm.Admin.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Admin.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Admin.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Admin.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Admin.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" }
                };

                return View("Areas/Admin/Views/Admin/EditAdmin.cshtml", vm);
            }

            if (vm.Admin.ImageFile != null && vm.Admin.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "admins");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Admin.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Admin.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Admin.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Admin.ImageFile.CopyToAsync(fileStream);
                }

                vm.Admin.ImageUrl = "/images/admins/" + fileName + extension;
            }

            await _unitOfWork.Admin.UpdateAsync(vm.Admin);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Admin updated successfully.";
            return RedirectToAction("GetAdmin", "Admin", new
            {
                area = "Admin",
                id = vm.Admin.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteAdminImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdminImage([FromForm] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(a => a.Id == vm.Admin.Id);
            if (admin == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(admin.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, admin.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                admin.ImageUrl = null;
                await _unitOfWork.Admin.UpdateAsync(admin);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditAdmin", "Admin", new
            {
                area = "Admin",
                id = vm.Admin.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteAdmin/{id:int}")]
        public async Task<IActionResult> DeleteAdmin([FromRoute] int id, [FromQuery] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            vm.Admin = admin;
            vm.DisplayImageUrl = string.IsNullOrEmpty(admin.ImageUrl) ? "/images/admins/default-admin.png" : admin.ImageUrl;
            vm.AppUserEmail = admin.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(admin.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Admin/DeleteAdmin.cshtml", vm);
        }

        [HttpPost("DeleteAdmin/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdminConfirmed([FromForm] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == vm.Admin.Id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(admin.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, admin.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Admin.Remove(admin);
            await _userManager.DeleteAsync(admin.AppUser);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Admin account has been deleted successfully.";
            return RedirectToAction("GetAllAdmins", "Admin", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ToggleAdminLock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminLock([FromForm] AdminVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
               a => a.Id == vm.Admin.Id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            if (await _userManager.IsLockedOutAsync(admin.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(admin.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(admin.AppUser);
                TempData["success"] = $"Admin has been unlocked successfully.";
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(admin.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));
                TempData["success"] = $"Admin has been locked successfully.";
            }

            await _unitOfWork.SaveAsync();

            return RedirectToAction(vm.SourceView, "Admin", new
            {
                area = "Admin",
                id = vm.Admin.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("ResetAdminPassword/{id:int}")]
        public async Task<IActionResult> ResetAdminPassword([FromRoute] int id, [FromQuery] ResetPasswordVM vm)
        {
            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            vm.Id = admin.Id;
            vm.AppUserEmail = admin.AppUser.Email;
            return View("Areas/Admin/Views/Admin/ResetAdminPassword.cshtml", vm);
        }

        [HttpPost("ResetAdminPassword/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAdminPasswordConfirmed([FromForm] ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Admin/ResetAdminPassword.cshtml", vm);
            }

            var admin = await _unitOfWork.Admin.GetFirstOrDefaultAsync(
                a => a.Id == vm.Id, includeProperties: "AppUser");
            if (admin == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(admin.AppUser);
            var result = await _userManager.ResetPasswordAsync(admin.AppUser, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Admin/ResetAdminPassword.cshtml", vm);
            }

            await _userManager.UpdateSecurityStampAsync(admin.AppUser);

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction(vm.SourceView, "Admin", new
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
