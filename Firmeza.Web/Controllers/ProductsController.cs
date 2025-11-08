using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Firmeza.Web.Models.ViewModels;
using Firmeza.Web.Services;

namespace Firmeza.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductService _svc;
        private readonly ProductRequestService _requests;
        private readonly IExcelService _excel;
        private readonly IWebHostEnvironment _env;

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public ProductsController(ProductService svc, ProductRequestService requests, IExcelService excel, IWebHostEnvironment env)
        {
            _svc = svc;
            _requests = requests;
            _excel = excel;
            _env = env;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Catalog()
        {
            var products = await _svc.ListAvailableAsync();
            return View(products);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(ProductRequestInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["CatalogMessage"] = "Datos inválidos en la solicitud.";
                return RedirectToAction(nameof(Catalog));
            }

            var userId = CurrentUserId;
            if (userId == null)
                return Challenge();

            var userEmail = User.Identity?.Name;
            var created = await _requests.CreateAsync(input.ProductId, input.Quantity, input.Note, userId, userEmail);
            TempData["CatalogMessage"] = created == null
                ? "El producto ya no está disponible."
                : "Solicitud enviada. Nuestro equipo te contactará pronto.";
            return RedirectToAction(nameof(Catalog));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            return View(await _svc.ListAsync(userId));
        }

        [Authorize(Policy = "RequireAdmin")]
        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create(Product m)
        {
            if (!ModelState.IsValid) return View(m);
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            await _svc.CreateAsync(m, userId);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            var e = await _svc.GetAsync(id, userId);
            if (e == null) return NotFound();
            return View(e);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Edit(Product m)
        {
            if (!ModelState.IsValid) return View(m);
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            await _svc.UpdateAsync(m, userId);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            var e = await _svc.GetAsync(id, userId);
            if (e == null) return NotFound();
            return View(e);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteConfirmed(Guid id, bool force = false)
        {
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            var result = await _svc.DeleteAsync(id, force, userId);
            if (result.SetInactive && !force)
            {
                TempData["ProductMessage"] = "No se puede eliminar porque ya tiene ventas; se marcó como inactivo. Usa la eliminación forzada si estás seguro.";
            }
            else if (result.Removed)
            {
                TempData["ProductMessage"] = "Producto eliminado definitivamente.";
                foreach (var saleId in result.DeletedSales)
                    RemoveReceipt(saleId);
            }
            else
            {
                TempData["ProductMessage"] = "Producto no encontrado.";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Export()
        {
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            var data = await _svc.ListAsync(userId);
            var bytes = await _excel.ExportProductsAsync(data);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "productos.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));
            using var s = file.OpenReadStream();
            var result = await _excel.ImportProductsAsync(s);
            var userId = CurrentUserId;
            if (userId == null) return Forbid();
            foreach (var p in result.ok)
            {
                p.CreatedByUserId = userId;
                await _svc.CreateAsync(p, userId);
            }
            TempData["ImportErrors"] = string.Join("; ", result.errors);
            return RedirectToAction(nameof(Index));
        }

        private void RemoveReceipt(Guid saleId)
        {
            var webRoot = _env.WebRootPath ?? System.IO.Path.Combine(_env.ContentRootPath, "wwwroot");
            var path = System.IO.Path.Combine(webRoot, "receipts", $"recibo_{saleId}.pdf");
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}
