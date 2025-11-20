using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Firmeza.Web.Services;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Firmeza.Web.Data;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Security.Claims;
using System.Collections.Generic;
using Firmeza.Web.Utils;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireAdmin")]
public class SalesController:Controller{
    private readonly AppDbContext _db; private readonly SaleService _svc; private readonly IPdfService _pdf; private readonly IWebHostEnvironment _env; private readonly ILogger<SalesController> _logger; private readonly IExcelService _excel;
    public SalesController(AppDbContext db, SaleService svc, IPdfService pdf, IWebHostEnvironment env, ILogger<SalesController> logger, IExcelService excel){ _db=db; _svc=svc; _pdf=pdf; _env=env; _logger=logger; _excel=excel; }
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? customerId, decimal? minTotal, decimal? maxTotal){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        await LoadLookupsAsync(userId, includeProducts:false);
        ViewBag.FilterFrom = from?.ToString("yyyy-MM-dd");
        ViewBag.FilterTo = to?.ToString("yyyy-MM-dd");
        ViewBag.FilterCustomerId = customerId?.ToString();
        ViewBag.FilterMinTotal = minTotal?.ToString("0.##");
        ViewBag.FilterMaxTotal = maxTotal?.ToString("0.##");
        var sales=await _svc.ListAsync(from, to, customerId, minTotal, maxTotal, userId);

        var dailyFilter = Request.Query["daily"].ToString();
        var monthlyFilter = Request.Query["monthly"].ToString();
        var yearlyFilter = Request.Query["yearly"].ToString();

        var localEntries = sales.Select(s => new { Date = s.CreatedAt.ToLocalFromUtc(), Amount = s.Total }).ToList();
        ViewBag.TotalSalesAmount = localEntries.Sum(x => x.Amount);
        var dailyGroups = localEntries
            .GroupBy(x => x.Date.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new Firmeza.Web.Models.ViewModels.SalesSummaryItem
            {
                Label = g.Key.ToString("yyyy-MM-dd"),
                Total = g.Sum(x => x.Amount)
            });
        var monthlyGroups = localEntries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g => new Firmeza.Web.Models.ViewModels.SalesSummaryItem
            {
                Label = $"{g.Key.Year}-{g.Key.Month:00}",
                Total = g.Sum(x => x.Amount)
            });
        var yearlyGroups = localEntries
            .GroupBy(x => x.Date.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new Firmeza.Web.Models.ViewModels.SalesSummaryItem
            {
                Label = g.Key.ToString(),
                Total = g.Sum(x => x.Amount)
            });

        ViewBag.DailyTotals = string.IsNullOrWhiteSpace(dailyFilter) ? dailyGroups : dailyGroups.Where(d => d.Label == dailyFilter);
        ViewBag.MonthlyTotals = string.IsNullOrWhiteSpace(monthlyFilter) ? monthlyGroups : monthlyGroups.Where(m => m.Label == monthlyFilter);
        ViewBag.YearlyTotals = string.IsNullOrWhiteSpace(yearlyFilter) ? yearlyGroups : yearlyGroups.Where(y => y.Label == yearlyFilter);
        ViewBag.DailyFilter = dailyFilter;
        ViewBag.MonthlyFilter = monthlyFilter;
        ViewBag.YearlyFilter = yearlyFilter;

        return View(sales);
    }
    public async Task<IActionResult> Create(){ var userId=CurrentUserId; if(userId==null) return Forbid(); await LoadLookupsAsync(userId); return View(); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid customerId, Guid[] productId, int[] quantity, decimal[] unitPrice){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        if(productId.Length!=quantity.Length || productId.Length!=unitPrice.Length) return RedirectToAction(nameof(Index));
        var customer=await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==customerId && c.CreatedByUserId==userId);
        if(customer==null) return Forbid();
        var productSet=productId.Distinct().ToHashSet();
        var allowedProducts=await _db.Products.AsNoTracking().Where(p=>productSet.Contains(p.Id) && p.CreatedByUserId==userId).Select(p=>p.Id).ToListAsync();
        if(allowedProducts.Count!=productSet.Count) return Forbid();
        var sale=new Sale{ CustomerId=customerId };
        sale.CreatedByUserId=userId;
        for(int i=0;i<productId.Length;i++){ sale.Items.Add(new SaleItem{ ProductId=productId[i], Quantity=quantity[i], UnitPrice=unitPrice[i], Subtotal=quantity[i]*unitPrice[i] }); }
        sale.Total=sale.Items.Sum(x=>x.Subtotal);
        await _svc.CreateAsync(sale);
        sale.Customer = customer;
        await EnsureReceiptAsync(sale);
        return RedirectToAction(nameof(Details), new { id=sale.Id });
    }
    public async Task<IActionResult> Details(Guid id){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        var sale=await _svc.GetAsync(id, userId); if(sale==null) return NotFound();
        await EnsureReceiptAsync(sale);
        ViewBag.ReceiptUrl = Url.Content($"~/receipts/recibo_{sale.Id}.pdf");
        return View(sale);
    }

    public async Task<IActionResult> Export(DateTime? from, DateTime? to, Guid? customerId, decimal? minTotal, decimal? maxTotal){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        var sales=await _svc.ListAsync(from, to, customerId, minTotal, maxTotal, userId);
        var bytes=await _excel.ExportSalesAsync(sales);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ventas.xlsx");
    }

    public async Task<IActionResult> Edit(Guid id){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        var sale=await _svc.GetAsync(id, userId); if(sale==null) return NotFound();
        await LoadLookupsAsync(userId);
        return View(sale);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid saleId, Guid customerId, Guid[] productId, int[] quantity, decimal[] unitPrice){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        if(productId.Length!=quantity.Length || productId.Length!=unitPrice.Length) return RedirectToAction(nameof(Index));
        var existing=await _svc.GetAsync(saleId, userId); if(existing==null) return NotFound();
        var customer=await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==customerId && c.CreatedByUserId==userId);
        if(customer==null) return Forbid();
        var productSet=productId.Distinct().ToHashSet();
        var allowedProducts=await _db.Products.AsNoTracking().Where(p=>productSet.Contains(p.Id) && p.CreatedByUserId==userId).Select(p=>p.Id).ToListAsync();
        if(allowedProducts.Count!=productSet.Count) return Forbid();
        var updated=new Sale{ Id=saleId, CustomerId=customerId, CreatedAt=existing.CreatedAt };
        updated.CreatedByUserId=userId;
        for(int i=0;i<productId.Length;i++){ updated.Items.Add(new SaleItem{ ProductId=productId[i], Quantity=quantity[i], UnitPrice=unitPrice[i], Subtotal=quantity[i]*unitPrice[i] }); }
        await _svc.UpdateAsync(updated, userId);
        var refreshed=await _svc.GetAsync(saleId, userId);
        if(refreshed!=null) await EnsureReceiptAsync(refreshed);
        return RedirectToAction(nameof(Details), new { id=saleId });
    }

    public async Task<IActionResult> Delete(Guid id){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        var sale=await _svc.GetAsync(id, userId); if(sale==null) return NotFound();
        return View(sale);
    }

    [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        await _svc.DeleteAsync(id, userId);
        var webRoot=_env.WebRootPath ?? Path.Combine(_env.ContentRootPath,"wwwroot");
        var receiptsDir=Path.Combine(webRoot,"receipts");
        var path=Path.Combine(receiptsDir,$"recibo_{id}.pdf");
        if(System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return RedirectToAction(nameof(Index));
    }

    private async Task EnsureReceiptAsync(Sale sale){
        var customer = sale.Customer ?? await _db.Customers.FindAsync(sale.CustomerId) ?? new Customer{ FullName="-" };
        var webRoot=_env.WebRootPath ?? Path.Combine(_env.ContentRootPath,"wwwroot");
        var receiptsDir=Path.Combine(webRoot,"receipts");
        Directory.CreateDirectory(receiptsDir);
        var path=Path.Combine(receiptsDir,$"recibo_{sale.Id}.pdf");
        if(!System.IO.File.Exists(path)){
            var bytes=await _pdf.BuildReceiptAsync(sale, customer);
            await System.IO.File.WriteAllBytesAsync(path, bytes);
            _logger.LogInformation("Recibo generado en {ReceiptPath}", path);
        }
    }

    private async Task LoadLookupsAsync(string ownerId, bool includeProducts = true){
        if(includeProducts)
            ViewBag.Products=await _db.Products.AsNoTracking().Where(p=>p.IsActive && p.CreatedByUserId==ownerId).OrderBy(p=>p.Name).ToListAsync();
        ViewBag.Customers=await _db.Customers.AsNoTracking().Where(c=>c.CreatedByUserId==ownerId).OrderBy(c=>c.FullName).ToListAsync();
    }
}}
