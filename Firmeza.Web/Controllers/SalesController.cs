using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using Firmeza.Web.Services; using Firmeza.Web.Interfaces; using Firmeza.Web.Models; using Firmeza.Web.Data; using System.Linq;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireAdmin")]
public class SalesController:Controller{
    private readonly AppDbContext _db; private readonly SaleService _svc; private readonly IPdfService _pdf;
    public SalesController(AppDbContext db, SaleService svc, IPdfService pdf){ _db=db; _svc=svc; _pdf=pdf; }
    public async Task<IActionResult> Index()=>View(await _svc.ListAsync());
    public async Task<IActionResult> Create(){ ViewBag.Products=await _db.Products.AsNoTracking().OrderBy(p=>p.Name).ToListAsync(); ViewBag.Customers=await _db.Customers.AsNoTracking().OrderBy(c=>c.FullName).ToListAsync(); return View(); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid customerId, Guid[] productId, int[] quantity, decimal[] unitPrice){
        if(productId.Length!=quantity.Length || productId.Length!=unitPrice.Length) return RedirectToAction(nameof(Index));
        var sale=new Sale{ CustomerId=customerId };
        for(int i=0;i<productId.Length;i++){ sale.Items.Add(new SaleItem{ ProductId=productId[i], Quantity=quantity[i], UnitPrice=unitPrice[i], Subtotal=quantity[i]*unitPrice[i] }); }
        sale.Total=sale.Items.Sum(x=>x.Subtotal);
        await _svc.CreateAsync(sale);
        var customer=await _db.Customers.FindAsync(customerId) ?? new Customer{ FullName="-" };
        var bytes=await _pdf.BuildReceiptAsync(sale, customer);
        var path=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot","receipts",$"recibo_{sale.Id}.pdf"); await System.IO.File.WriteAllBytesAsync(path, bytes);
        return RedirectToAction(nameof(Details), new { id=sale.Id });
    }
    public async Task<IActionResult> Details(Guid id){ var sale=await _svc.GetAsync(id); if(sale==null) return NotFound(); return View(sale); }
}}