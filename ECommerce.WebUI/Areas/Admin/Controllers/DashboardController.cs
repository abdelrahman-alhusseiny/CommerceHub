using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_SuperAdmin)]
    [Area("Admin")]
    [Route("Admin/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
