using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (User.IsInRole("SuperAdmin"))
                return View("SuperAdmin");

            if (User.IsInRole("Admin"))
                return View("Admin");

            return View("Customer");
        }
    }
}
