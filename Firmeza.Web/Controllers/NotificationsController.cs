using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Firmeza.Web.Services;

namespace Firmeza.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ProductRequestService _requests;

        public NotificationsController(ProductRequestService requests)
        {
            _requests = requests;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var items = await _requests.ListByUserAsync(userId);
            ViewData["Title"] = "Notificaciones";
            return View(items);
        }
    }
}
