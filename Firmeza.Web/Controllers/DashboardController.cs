using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Firmeza.Web.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (User.IsInRole("SuperAdmin"))
                return View("SuperAdmin");

            return View("Admin");
        }
    }
}
