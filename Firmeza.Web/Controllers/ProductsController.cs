using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Firmeza.Web.Services; using Firmeza.Web.Interfaces; using Firmeza.Web.Models;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireAdmin")]
public class ProductsController:Controller{
    private readonly ProductService _svc; private readonly IExcelService _excel;
    public ProductsController(ProductService svc, IExcelService excel){ _svc=svc; _excel=excel; }
    public async Task<IActionResult> Index()=>View(await _svc.ListAsync());
    public IActionResult Create()=>View(new Product());
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Create(Product m){ if(!ModelState.IsValid)return View(m); await _svc.CreateAsync(m); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(Guid id){ var e=await _svc.GetAsync(id); if(e==null) return NotFound(); return View(e); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Edit(Product m){ if(!ModelState.IsValid)return View(m); await _svc.UpdateAsync(m); return RedirectToAction(nameof(Index)); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Delete(Guid id){ await _svc.DeleteAsync(id); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Export(){ var data=await _svc.ListAsync(); var bytes=await _excel.ExportProductsAsync(data); return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "productos.xlsx"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Import(IFormFile file){ if(file==null || file.Length==0) return RedirectToAction(nameof(Index)); using var s=file.OpenReadStream(); var result=await _excel.ImportProductsAsync(s); foreach(var p in result.ok) await _svc.CreateAsync(p); TempData["ImportErrors"]=string.Join("; ", result.errors); return RedirectToAction(nameof(Index)); }
}}