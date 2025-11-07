using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc; using Firmeza.Web.Models; using Firmeza.Web.Models.ViewModels; using System.Linq;
namespace Firmeza.Web.Controllers{
[Authorize(Policy="RequireSuperAdmin")]
public class AdminController:Controller{
    private readonly UserManager<AppUser> _users; private readonly RoleManager<IdentityRole> _roles;
    public AdminController(UserManager<AppUser> users, RoleManager<IdentityRole> roles){ _users=users; _roles=roles; }
    public IActionResult Users()=>View(_users.Users.ToList());
    public async Task<IActionResult> EditRoles(string id){ var u=await _users.FindByIdAsync(id); if(u==null) return NotFound(); var roles=_roles.Roles.Select(r=>r.Name!).OrderBy(x=>x).ToList(); var my=(await _users.GetRolesAsync(u)).ToList(); return View(new EditRolesViewModel{ UserId=u.Id, Email=u.Email??u.UserName??"", Roles=roles, Assigned=my }); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditRolesViewModel m){ var u=await _users.FindByIdAsync(m.UserId); if(u==null) return NotFound(); var current=await _users.GetRolesAsync(u); var toAdd=m.Assigned.Except(current).ToList(); var toRemove=current.Except(m.Assigned).ToList(); if(toAdd.Count>0) await _users.AddToRolesAsync(u,toAdd); if(toRemove.Count>0) await _users.RemoveFromRolesAsync(u,toRemove); return RedirectToAction(nameof(Users)); }
}}
