using Firmeza.WebApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.WebApplication.Controllers;

public class ProductsController : Controller
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var items = await _service.ListAsync(q);
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, decimal unitPrice)
    {
        await _service.CreateAsync(name, unitPrice);
        return RedirectToAction(nameof(Index));
    }
}
