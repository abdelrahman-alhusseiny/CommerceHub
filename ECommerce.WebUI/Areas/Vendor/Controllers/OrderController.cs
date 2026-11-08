using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.WebUI.Areas.Vendor.Controllers
{
    [Authorize(Roles = SD.Role_Vendor)]
    [Area("Vendor")]
    [Route("Vendor/Order")]
    public class OrderController : Controller
    {
        
    }
}
