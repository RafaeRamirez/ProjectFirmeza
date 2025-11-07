using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc;
using Firmeza.Web.Models; using Firmeza.Web.Models.ViewModels;
namespace Firmeza.Web.Controllers{
[AllowAnonymous]
public class AccountController:Controller{
    private readonly SignInManager<AppUser> _signIn; private readonly UserManager<AppUser> _users;
    public AccountController(SignInManager<AppUser> signIn, UserManager<AppUser> users){ _signIn=signIn; _users=users; }
    [HttpGet] public IActionResult Login(string? returnUrl=null){ ViewData["ReturnUrl"]=returnUrl; return View(new LoginViewModel()); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl=null){
        if(!ModelState.IsValid) return View(model);
        var user=await _users.FindByEmailAsync(model.Email);
        if(user is null){ ModelState.AddModelError(string.Empty, "Credenciales inválidas."); return View(model); }
        var res=await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
        if(res.Succeeded) return Redirect(returnUrl ?? Url.Action("Index","Home")!);
        ModelState.AddModelError(string.Empty, "Credenciales inválidas."); return View(model);
    }
    [HttpGet] public IActionResult Register()=>View(new RegisterViewModel());
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model){
        if(!ModelState.IsValid) return View(model);
        var user=new AppUser{ UserName=model.Email, Email=model.Email, EmailConfirmed=true };
        var res=await _users.CreateAsync(user, model.Password);
        if(res.Succeeded) return RedirectToAction(nameof(Login));
        foreach(var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }
    [Authorize] public async Task<IActionResult> Logout(){ await _signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    public IActionResult AccessDenied()=>View();
}}
