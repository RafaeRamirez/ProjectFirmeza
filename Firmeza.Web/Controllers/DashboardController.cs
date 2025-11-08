using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using Firmeza.Web.Models.ViewModels;
using Firmeza.Web.Services;

namespace Firmeza.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ProductRequestService _requests;

        public DashboardController(ProductRequestService requests)
        {
            _requests = requests;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("SuperAdmin"))
                return View("SuperAdmin");

            if (User.IsInRole("Admin"))
                return View("Admin");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new CustomerDashboardViewModel();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                model.Requests = await _requests.ListByUserAsync(userId);
                model.HasRecentUpdates = model.Requests.Any(r => r.Status != "Pending" && r.ProcessedAt.HasValue && r.ProcessedAt.Value > DateTime.UtcNow.AddDays(-2));
            }
            return View("Customer", model);
        }
    }
}
