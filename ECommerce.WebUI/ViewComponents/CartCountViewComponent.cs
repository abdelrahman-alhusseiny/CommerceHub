using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.WebUI.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public CartCountViewComponent(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var appUser = await _userManager.GetUserAsync(HttpContext.User);
            if (appUser == null)
            {
                HttpContext.Session.Remove(SD.SessionCartCount);
                return View(0);
            }

            var customer = await _unitOfWork.Customer.GetFirstOrDefaultAsync(
                c => c.AppUserId == appUser.Id);
            if (customer == null)
            {
                HttpContext.Session.Remove(SD.SessionCartCount);
                return View(0);
            }

            var sessionCount = HttpContext.Session.GetInt32(SD.SessionCartCount);
            if (sessionCount.HasValue)
            {
                return View(sessionCount.Value);
            }

            var cartItems = await _unitOfWork.CartItem.GetAllAsync(ci => ci.CustomerId == customer.Id);
            var cartCount = cartItems.Sum(ci => ci.Quantity);

            HttpContext.Session.SetInt32(SD.SessionCartCount, cartCount);
            return View(cartCount);
        }
    }
}
