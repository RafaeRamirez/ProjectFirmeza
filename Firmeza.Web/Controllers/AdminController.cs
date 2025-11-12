using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc; using Firmeza.Web.Models; using Firmeza.Web.Models.ViewModels; using Microsoft.EntityFrameworkCore; using System.Linq;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireSuperAdmin")]
public class AdminController:Controller{
    private readonly UserManager<AppUser> _users; private readonly RoleManager<IdentityRole> _roles;
    public AdminController(UserManager<AppUser> users, RoleManager<IdentityRole> roles){ _users=users; _roles=roles; }
    public async Task<IActionResult> Users(){
        var list=await _users.Users.ToListAsync();
        var result=new List<AdminUserListItemViewModel>(list.Count);
        foreach(var user in list){
            var roles=await _users.GetRolesAsync(user);
            result.Add(new AdminUserListItemViewModel{
                UserId=user.Id,
                Email=user.Email??user.UserName??string.Empty,
                Roles=roles.OrderBy(r=>r).ToList()
            });
        }
        return View(result);
    }
    public async Task<IActionResult> Edit(string id){ var u=await _users.FindByIdAsync(id); if(u==null) return NotFound(); return View(new ManageUserViewModel{ UserId=u.Id, Email=u.Email??string.Empty, UserName=u.UserName??string.Empty, PhoneNumber=u.PhoneNumber}); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ManageUserViewModel m){ if(!ModelState.IsValid) return View(m); var u=await _users.FindByIdAsync(m.UserId); if(u==null) return NotFound(); u.Email=m.Email; u.UserName=m.UserName; u.PhoneNumber=m.PhoneNumber; var res=await _users.UpdateAsync(u); if(!res.Succeeded){ foreach(var e in res.Errors) ModelState.AddModelError(string.Empty,e.Description); return View(m);} return RedirectToAction(nameof(Users)); }
    public async Task<IActionResult> Delete(string id){ var u=await _users.FindByIdAsync(id); if(u==null) return NotFound(); return View(u); }
    [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id){ var u=await _users.FindByIdAsync(id); if(u==null) return NotFound(); if(string.Equals(User?.Identity?.Name,u.Email,StringComparison.OrdinalIgnoreCase)) { TempData["Error"]="No puedes eliminar tu propia cuenta."; return RedirectToAction(nameof(Users)); } await _users.DeleteAsync(u); return RedirectToAction(nameof(Users)); }
    public async Task<IActionResult> EditRoles(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();

        var roles = _roles.Roles
            .Select(r => r.Name!)
            .OrderBy(x => x)
            .ToList();

        var my = (await _users.GetRolesAsync(u)).Where(name => roles.Contains(name)).ToList();

        return View(new EditRolesViewModel
        {
            UserId = u.Id,
            Email = u.Email ?? u.UserName ?? "",
            Roles = roles,
            Assigned = my
        });
    }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditRolesViewModel m){ var u=await _users.FindByIdAsync(m.UserId); if(u==null) return NotFound(); var current=await _users.GetRolesAsync(u); var toAdd=m.Assigned.Except(current).ToList(); var toRemove=current.Except(m.Assigned).ToList(); if(toAdd.Count>0) await _users.AddToRolesAsync(u,toAdd); if(toRemove.Count>0) await _users.RemoveFromRolesAsync(u,toRemove); return RedirectToAction(nameof(Users)); }
}}
