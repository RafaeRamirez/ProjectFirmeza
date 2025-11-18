using System.Security.Claims;
using System.Text;
using Firmeza.Api.Contracts.Dtos.Auth;
using Firmeza.Api.Contracts.Dtos.Customers;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Services;
using Firmeza.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string CustomerRole = "Customer";
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly CustomerService _customers;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _clientBaseUrl;
    private readonly string _resetPath;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        CustomerService customers,
        RoleManager<IdentityRole> roleManager,
        IEmailSender emailSender,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _customers = customers;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _logger = logger;
        _environment = environment;
        _clientBaseUrl = configuration["Client:BaseUrl"] ?? "http://localhost:4200";
        _resetPath = configuration["Client:ResetPath"] ?? "/restablecer";
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterClientDto dto)
    {
        await EnsureRoleExistsAsync(CustomerRole);
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            return Conflict("Ya existe un usuario con ese correo.");
        }

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(";", result.Errors.Select(e => e.Description));
            return BadRequest(errors);
        }

        var roleAssignment = await _userManager.AddToRoleAsync(user, CustomerRole);
        if (!roleAssignment.Succeeded)
        {
            var errors = string.Join(";", roleAssignment.Errors.Select(e => e.Description));
            return StatusCode(StatusCodes.Status500InternalServerError, $"No se pudo asignar el rol de cliente: {errors}");
        }

        var customer = await _customers.CreateAsync(new CustomerCreateDto
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone
        }, user.Id);

        var token = await _tokenService.CreateAsync(user);
        return Ok(token with
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!check.Succeeded)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var customer = await _customers.GetByUserAsync(user.Id);
        var token = await _tokenService.CreateAsync(user);
        return Ok(token with
        {
            CustomerId = customer?.Id,
            CustomerName = customer?.FullName
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Email,
            Roles = await _userManager.GetRolesAsync(user),
            UserId = user.Id
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(ForgotPasswordRequestDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest("Debes indicar el correo del usuario.");
        }

        var response = new ForgotPasswordResponseDto
        {
            Message = "Si el correo existe enviaremos un enlace para restablecer tu contraseña."
        };

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            response.EmailSent = true;
            return Ok(response);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetUrl = BuildResetUrl(user.Id, encoded);

        try
        {
            var body = $"""
                        Hola,<br/><br/>
                        Recibimos una solicitud para restablecer tu contraseña en Firmeza. Haz clic en el siguiente enlace para continuar:<br/>
                        <a href="{resetUrl}">Restablecer contraseña</a><br/><br/>
                        Si no solicitaste este cambio, puedes ignorar este correo.
                        """;
            await _emailSender.SendAsync(user.Email ?? dto.Email, "Restablecer contraseña", body);
            response.EmailSent = true;
            response.Message = "Enviamos un enlace a tu correo. Revísalo para continuar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento para {Email}", dto.Email);
            response.EmailSent = false;
            response.Message = "No pudimos enviar el correo. Usa el enlace directo para continuar.";
            response.ShowResetLink = true;
        }

        if (_environment.IsDevelopment() || response.ShowResetLink)
        {
            response.UserId = user.Id;
            response.Token = encoded;
            response.ResetLink = resetUrl;
            response.ShowResetLink = true;
        }

        return Ok(response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequestDto dto)
    {
        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.UserId) ||
            string.IsNullOrWhiteSpace(dto.Token) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest("Datos incompletos para restablecer la contraseña.");
        }

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user is null || !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Usuario no encontrado.");
        }

        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
        var result = await _userManager.ResetPasswordAsync(user, decoded, dto.Password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(error);
        }

        return Ok(new { message = "Contraseña restablecida correctamente." });
    }

    private string BuildResetUrl(string userId, string token)
    {
        var baseUri = _clientBaseUrl.TrimEnd('/');
        var path = _resetPath.StartsWith("/") ? _resetPath : $"/{_resetPath}";
        var target = $"{baseUri}{path}";
        var separator = target.Contains('?') ? "&" : "?";
        var userParam = Uri.EscapeDataString(userId);
        var tokenParam = Uri.EscapeDataString(token);
        return $"{target}{separator}userId={userParam}&token={tokenParam}";
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
