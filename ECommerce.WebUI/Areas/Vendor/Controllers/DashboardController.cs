using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebUI.Areas.Vendor.Controllers
{
    [Authorize(Roles = SD.Role_Vendor)]
    [Area("Vendor")]
    [Route("Vendor/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
