using System.Security.Claims;
using Firmeza.Api.Contracts.Dtos.Auth;
using Firmeza.Api.Contracts.Dtos.Customers;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Services;
using Firmeza.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        CustomerService customers,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _customers = customers;
        _roleManager = roleManager;
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

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
