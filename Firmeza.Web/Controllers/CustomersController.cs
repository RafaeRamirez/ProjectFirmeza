using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Firmeza.Web.Services; using Firmeza.Web.Models;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireAdmin")]
public class CustomersController:Controller{
    private readonly CustomerService _svc; public CustomersController(CustomerService svc)=>_svc=svc;
    public async Task<IActionResult> Index(string? q)=>View(await _svc.ListAsync(q));
    public IActionResult Create()=>View(new Customer());
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Create(Customer m){ if(!ModelState.IsValid) return View(m); await _svc.CreateAsync(m); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(Guid id){ var e=await _svc.GetAsync(id); if(e==null) return NotFound(); return View(e); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Edit(Customer m){ if(!ModelState.IsValid) return View(m); await _svc.UpdateAsync(m); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(Guid id){ var e=await _svc.GetAsync(id); if(e==null) return NotFound(); return View(e); }
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id){ await _svc.DeleteAsync(id); return RedirectToAction(nameof(Index)); }
}}