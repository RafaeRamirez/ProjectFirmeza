using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Firmeza.Web.Services; using Firmeza.Web.Models; using System.Security.Claims;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireAdmin")]
public class CustomersController:Controller{
    private readonly CustomerService _svc; public CustomersController(CustomerService svc)=>_svc=svc;
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public async Task<IActionResult> Index(string? q){
        var userId=CurrentUserId; if(userId==null) return Forbid();
        return View(await _svc.ListAsync(q, userId));
    }
    public IActionResult Create()=>View(new Customer());
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Create(Customer m){ if(!ModelState.IsValid) return View(m); var userId=CurrentUserId; if(userId==null) return Forbid(); m.CreatedByUserId=userId; await _svc.CreateAsync(m); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(Guid id){ var userId=CurrentUserId; if(userId==null) return Forbid(); var e=await _svc.GetAsync(id, userId); if(e==null) return NotFound(); return View(e); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Edit(Customer m){ if(!ModelState.IsValid) return View(m); var userId=CurrentUserId; if(userId==null) return Forbid(); await _svc.UpdateAsync(m, userId); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(Guid id){ var userId=CurrentUserId; if(userId==null) return Forbid(); var e=await _svc.GetAsync(id, userId); if(e==null) return NotFound(); return View(e); }
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id){ var userId=CurrentUserId; if(userId==null) return Forbid(); await _svc.DeleteAsync(id, userId); return RedirectToAction(nameof(Index)); }
}}
