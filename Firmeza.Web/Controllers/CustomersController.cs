using System.Security.Claims;
using System.Text;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Firmeza.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class CustomersController : Controller
{
    private readonly CustomerService _svc;
    private readonly UserManager<AppUser> _users;
    private readonly IEmailSender _email;
    private readonly ILogger<CustomersController> _logger;
    private readonly IHostEnvironment _env;

    public CustomersController(
        CustomerService svc,
        UserManager<AppUser> users,
        IEmailSender email,
        ILogger<CustomersController> logger,
        IHostEnvironment env)
    {
        _svc = svc;
        _users = users;
        _email = email;
        _logger = logger;
        _env = env;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index(string? q)
    {
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        return View(await _svc.ListAsync(q, userId));
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        model.CreatedByUserId = userId;
        await _svc.CreateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        var entity = await _svc.GetAsync(id, userId);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        await _svc.UpdateAsync(model, userId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        var entity = await _svc.GetAsync(id, userId);
        if (entity == null) return NotFound();
        return View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Forbid();
        await _svc.DeleteAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordReset(Guid id)
    {
        var userId = CurrentUserId;
        if (userId == null) return Forbid();

        var customer = await _svc.GetAsync(id, userId);
        if (customer == null)
        {
            TempData["CustomersError"] = "El cliente no existe o no te pertenece.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            TempData["CustomersError"] = "Este cliente no tiene correo registrado.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _users.FindByEmailAsync(customer.Email);
        if (user is null)
        {
            TempData["CustomersError"] = "No hay ninguna cuenta asociada a ese correo.";
            return RedirectToAction(nameof(Index));
        }

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callback = Url.Action("ResetPassword", "Account", new { userId = user.Id, token = encoded }, Request.Scheme);
        var body = $"""
            <p>Hola {customer.FullName},</p>
            <p>Recibimos una solicitud para restablecer tu contraseña en Firmeza.</p>
            <p><a href="{callback}">Haz clic aquí para crear una nueva contraseña</a></p>
            <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
            """;

        try
        {
            await _email.SendAsync(customer.Email, "Restablecer contraseña", body);
            TempData["CustomersMessage"] = $"Se envió un enlace de recuperación a {customer.Email}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de recuperación para {Email}", customer.Email);
            TempData["CustomersError"] = "No se pudo enviar el correo. Revisa la configuración SMTP.";
        }

        if (_env.IsDevelopment())
        {
            TempData["CustomersResetLink"] = callback;
        }

        return RedirectToAction(nameof(Index));
    }
}
