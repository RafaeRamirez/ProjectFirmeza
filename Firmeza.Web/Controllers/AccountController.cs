using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Firmeza.Web.Models;
using Firmeza.Web.Models.ViewModels;
using Firmeza.Web.Interfaces;
namespace Firmeza.Web.Controllers{
[AllowAnonymous]
public class AccountController:Controller{
    private readonly SignInManager<AppUser> _signIn; private readonly UserManager<AppUser> _users; private readonly IEmailSender _email; private readonly ILogger<AccountController> _logger; private readonly IHostEnvironment _env;
    public AccountController(SignInManager<AppUser> signIn, UserManager<AppUser> users, IEmailSender email, ILogger<AccountController> logger, IHostEnvironment env){ _signIn=signIn; _users=users; _email=email; _logger=logger; _env=env; }
    [HttpGet] public IActionResult Login(string? returnUrl=null){ ViewData["ReturnUrl"]=returnUrl; return View(new LoginViewModel()); }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl=null){
        if(!ModelState.IsValid) return View(model);
        var user=await _users.FindByEmailAsync(model.Email);
        if(user is null){ ModelState.AddModelError(string.Empty, "Credenciales inválidas."); return View(model); }
        var res=await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
        if(res.Succeeded) return Redirect(returnUrl ?? Url.Action("Index","Dashboard")!);
        ModelState.AddModelError(string.Empty, "Credenciales inválidas."); return View(model);
    }
    [HttpGet] public IActionResult Register()=>View(new RegisterViewModel());
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model){
        if(!ModelState.IsValid) return View(model);
        var user=new AppUser{ UserName=model.Email, Email=model.Email, EmailConfirmed=true };
        var res=await _users.CreateAsync(user, model.Password);
        if(res.Succeeded){
            var addToCustomer=await _users.AddToRoleAsync(user, "Customer");
            if(addToCustomer.Succeeded) return RedirectToAction(nameof(Login));
            foreach(var e in addToCustomer.Errors) ModelState.AddModelError(string.Empty, e.Description);
        }
        foreach(var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }
    [HttpGet] public IActionResult ForgotPassword()=>View(new ForgotPasswordViewModel());
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model){
        if(!ModelState.IsValid) return View(model);
        var confirmation=new ForgotPasswordConfirmationViewModel();
        var user=await _users.FindByEmailAsync(model.Email);
        if(user!=null){
            var token=await _users.GeneratePasswordResetTokenAsync(user);
            var encoded=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callback=Url.Action(nameof(ResetPassword), "Account", new { userId=user.Id, token=encoded }, Request.Scheme);
            var body=$"<p>Hola,</p><p>Recibimos una solicitud para restablecer tu contraseña. Haz clic en el siguiente enlace:</p><p><a href=\"{callback}\">Restablecer contraseña</a></p><p>Si no solicitaste este cambio, ignora este correo.</p>";
            confirmation.ShowResetLink=_env.IsDevelopment();
            if(confirmation.ShowResetLink) confirmation.ResetLink=callback;
            try
            {
                await _email.SendAsync(user.Email??model.Email, "Restablecer contraseña", body);
                confirmation.EmailSent=true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento.");
                confirmation.ErrorMessage="No pudimos enviar el correo. Revisa la configuración SMTP.";
            }
        }
        return View("ForgotPasswordConfirmation", confirmation);
    }
    [HttpGet] public IActionResult ResetPassword(string userId, string token){
        if(string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token)) return BadRequest();
        return View(new ResetPasswordViewModel{ UserId=userId, Token=token });
    }
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model){
        if(!ModelState.IsValid) return View(model);
        var user=await _users.FindByIdAsync(model.UserId);
        if(user==null || !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase)){
            ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
            return View(model);
        }
        var decoded=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result=await _users.ResetPasswordAsync(user, decoded, model.Password);
        if(result.Succeeded){
            TempData["LoginMessage"]="Contraseña restablecida correctamente. Inicia sesión con tu nueva contraseña.";
            return RedirectToAction(nameof(Login));
        }
        foreach(var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }
    [Authorize] public async Task<IActionResult> Logout(){ await _signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    public IActionResult AccessDenied()=>View();
}}
