using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Firmeza.Web.Models.ViewModels;
using Firmeza.Web.Services;

namespace Firmeza.Web.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class ProductRequestsController : Controller
    {
        private readonly ProductRequestService _service;

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public ProductRequestsController(ProductRequestService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _service.ListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(ReviewProductRequestInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["RequestMessage"] = "Datos inválidos.";
                return RedirectToAction(nameof(Index));
            }

            var userId = CurrentUserId;
            if (userId == null) return Forbid();

            var status = input.Action == "approve" ? "Approved" : "Rejected";
            var ok = await _service.UpdateStatusAsync(input.RequestId, status, input.ResponseMessage, userId);
            TempData["RequestMessage"] = ok ? "Solicitud actualizada." : "No se encontró la solicitud.";
            return RedirectToAction(nameof(Index));
        }
    }
}
