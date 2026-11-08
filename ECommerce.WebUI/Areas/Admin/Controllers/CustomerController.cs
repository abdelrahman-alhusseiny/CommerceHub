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
    [Route("Admin/Customer")]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CustomerController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllCustomers")]
        public async Task<IActionResult> GetAllCustomers(
            [FromQuery] string searchString = "",
            [FromQuery] string lockoutStatus = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 5;
            var (customers, totalItems) =
                await _unitOfWork.Customer.GetFilteredCustomersAsync(
                    searchString, lockoutStatus, currentSort, currentPage, pageSize);

            var customerRows = new List<CustomerRowVM>();
            foreach (var c in customers)
            {
                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(c.AppUser) ? "Locked" : "Unlocked";
                customerRows.Add(new CustomerRowVM
                {
                    Customer = c,
                    DisplayImageUrl = string.IsNullOrEmpty(c.ImageUrl) ? "/images/customers/default-customer.png" : c.ImageUrl,
                    AppUserEmail = c.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus,
                });
            }

            var vm = new CustomerListVM
            {
                Customers = customerRows,
                SourceView = "GetAllCustomers",

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

            return View("Areas/Admin/Views/Customer/GetAllCustomers.cshtml", vm);
        }

        [HttpGet("GetCustomer/{id:int}")]
        public async Task<IActionResult> GetCustomer(
            [FromRoute] int id, [FromQuery] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            vm.Customer = customer;
            vm.SourceView = "GetCustomer";
            vm.DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl;
            vm.AppUserEmail = customer.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(customer.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Customer/GetCustomer.cshtml", vm);
        }

        [HttpGet("RegisterCustomer")]
        public IActionResult RegisterCustomer([FromQuery] RegisterCustomerVM vm)
        {
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" },
            };

            return View("Areas/Admin/Views/Customer/RegisterCustomer.cshtml", vm);
        }

        [HttpPost("RegisterCustomer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCustomerConfirmed([FromForm] RegisterCustomerVM vm)
        {
            if (vm.Customer.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Customer.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Customer.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Customer.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Customer.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };

                return View("Areas/Admin/Views/Customer/RegisterCustomer.cshtml", vm);
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

                return View("Areas/Admin/Views/Customer/RegisterCustomer.cshtml", vm);
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Customer);

            if (vm.Customer.ImageFile != null && vm.Customer.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "customers");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
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

            vm.Customer.AppUserId = appUser.Id;

            await _unitOfWork.Customer.AddAsync(vm.Customer);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Customer registered successfully.";
            return RedirectToAction("GetAllCustomers", "Customer", new { area = "Admin" });
        }

        [HttpGet("EditCustomer/{id:int}")]
        public async Task<IActionResult> EditCustomer(
            [FromRoute] int id, [FromQuery] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            vm.Customer = customer;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
            vm.DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl;
            vm.HasCustomImage = !string.IsNullOrEmpty(customer.ImageUrl);

            return View("Areas/Admin/Views/Customer/EditCustomer.cshtml", vm);
        }

        [HttpPost("EditCustomer/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomerConfirmed([FromForm] CustomerVM vm)
        {
            if (vm.Customer.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Customer.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Customer.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Customer.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Customer.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" }
                };

                return View("Areas/Admin/Views/Customer/EditCustomer.cshtml", vm);
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
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Customer.ImageUrl.TrimStart('/'));
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

            TempData["success"] = "Customer updated successfully.";
            return RedirectToAction("GetCustomer", "Customer", new
            {
                area = "Admin",
                id = vm.Customer.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteCustomerImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomerImage([FromForm] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(c => c.Id == vm.Customer.Id);
            if (customer == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(customer.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, customer.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                customer.ImageUrl = null;
                await _unitOfWork.Customer.UpdateAsync(customer);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditCustomer", "Customer", new
            {
                area = "Admin",
                id = vm.Customer.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteCustomer/{id:int}")]
        public async Task<IActionResult> DeleteCustomer([FromRoute] int id, [FromQuery] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            vm.Customer = customer;
            vm.DisplayImageUrl = string.IsNullOrEmpty(customer.ImageUrl) ? "/images/customers/default-customer.png" : customer.ImageUrl;
            vm.AppUserEmail = customer.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(customer.AppUser) ? "Locked" : "Unlocked";
           
            return View("Areas/Admin/Views/Customer/DeleteCustomer.cshtml", vm);
        }

        [HttpPost("DeleteCustomer/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomerConfirmed([FromForm] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == vm.Customer.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(customer.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, customer.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Customer.Remove(customer);
            await _userManager.DeleteAsync(customer.AppUser);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Customer account has been deleted successfully.";
            return RedirectToAction("GetAllCustomers", "Customer", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ToggleCustomerLock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCustomerLock([FromForm] CustomerVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
               c => c.Id == vm.Customer.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            if (await _userManager.IsLockedOutAsync(customer.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(customer.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(customer.AppUser);
                TempData["success"] = $"Customer has been unlocked successfully.";
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(customer.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));
                TempData["success"] = $"Customer has been locked successfully.";
            }

            await _unitOfWork.SaveAsync();

            return RedirectToAction(vm.SourceView, "Customer", new
            {
                area = "Admin",
                id = vm.Customer.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("ResetCustomerPassword/{id:int}")]
        public async Task<IActionResult> ResetCustomerPassword([FromRoute] int id, [FromQuery] ResetPasswordVM vm)
        {
            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            vm.Id = customer.Id;
            vm.AppUserEmail = customer.AppUser.Email;
            return View("Areas/Admin/Views/Customer/ResetCustomerPassword.cshtml", vm);
        }

        [HttpPost("ResetCustomerPassword/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetCustomerPasswordConfirmed([FromForm] ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Customer/ResetCustomerPassword.cshtml", vm);
            }

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.Id == vm.Id, includeProperties: "AppUser");
            if (customer == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(customer.AppUser);
            var result = await _userManager.ResetPasswordAsync(customer.AppUser, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Customer/ResetCustomerPassword.cshtml", vm);
            }

            await _userManager.UpdateSecurityStampAsync(customer.AppUser);

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction(vm.SourceView, "Customer", new
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
