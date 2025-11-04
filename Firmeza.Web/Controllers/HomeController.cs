using Firmeza.WebApplication.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.WebApplication.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    [HttpGet] public IActionResult Index() => View();

    // Simple DB health-check
    [HttpGet]
    public async Task<IActionResult> HealthDb()
    {
        var ok = await _db.Database.CanConnectAsync();
        if (!ok) return Content("DB FAIL");
        var total = await _db.Products.CountAsync();
        return Content($"DB OK. Products: {total}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => Problem(detail: "An unexpected error occurred.");
}
