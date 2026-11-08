using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace ECommerce.WebUI.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(
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

        [HttpGet("ChooseRole")]
        public IActionResult ChooseRole()
        {
            var vm = new ChooseRoleVM
            {
                CustomerImageUrl = "/images/identity/customer.png",
                VendorImageUrl = "/images/identity/vendor.png"
            };

            return View("Areas/Identity/Views/Account/ChooseRole.cshtml", vm);
        }

        [HttpGet("RegisterCustomer")]
        public IActionResult RegisterCustomer()
        {
            var vm = new RegisterCustomerVM();
            vm.GenderList = new List<SelectListItem>
        {
            new SelectListItem { Text = "Male", Value = "Male" },
            new SelectListItem { Text = "Female", Value = "Female" },
        };

            return View("Areas/Identity/Views/Account/RegisterCustomer.cshtml", vm);
        }

        [HttpPost("RegisterCustomer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCustomerConfirmed([FromForm] RegisterCustomerVM vm)
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

                return View("Areas/Identity/Views/Account/RegisterCustomer.cshtml", vm);
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

                return View("Areas/Identity/Views/Account/RegisterCustomer.cshtml", vm);
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

            TempData["success"] = "Customer account registered successfully.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [HttpGet("RegisterVendor")]
        public IActionResult RegisterVendor()
        {
            return View("Areas/Identity/Views/Account/RegisterVendor.cshtml");
        }

        [HttpPost("RegisterVendor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterVendorConfirmed([FromForm] RegisterVendorVM vm)
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
                return View("Areas/Identity/Views/Account/RegisterVendor.cshtml", vm);
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

                return View("Areas/Identity/Views/Account/RegisterVendor.cshtml", vm);
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

            TempData["success"] = "Vendor account registered successfully.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [HttpGet("Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            var vm = new LoginVM { ReturnUrl = returnUrl };
            return View("Areas/Identity/Views/Account/Login.cshtml", vm);
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginConfirmed([FromForm] LoginVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                TempData["success"] = "You have logged in successfully.";
                return await RedirectToLocal(vm.ReturnUrl, user);
            }
            else
            {
                if (result.IsLockedOut)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    ModelState.AddModelError(string.Empty,
                        $"Your account is locked until {lockoutEnd?.LocalDateTime}");

                    return View("Areas/Identity/Views/Account/Login.cshtml", vm);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }
        }

        private async Task<IActionResult> RedirectToLocal(
            string? returnUrl, ApplicationUser user)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SD.Role_SuperAdmin))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (roles.Contains(SD.Role_Admin))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (roles.Contains(SD.Role_Vendor))
                return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
            if (roles.Contains(SD.Role_Customer))
                return RedirectToAction("GetAllProducts", "Shop", new { area = "Customer" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
