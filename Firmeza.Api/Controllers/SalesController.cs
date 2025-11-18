using System.Security.Claims;
using Firmeza.Api.Contracts.Dtos.Customers;
using Firmeza.Api.Contracts.Dtos.Sales;
using Firmeza.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private const string CustomerRole = "Customer";
    private readonly SaleService _service;
    private readonly CustomerService _customers;

    public SalesController(SaleService service, CustomerService customers)
    {
        _service = service;
        _customers = customers;
    }

    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<List<SaleDto>>> GetAll([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var sales = await _service.ListAsync(from, to);
        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id)
    {
        var sale = await _service.GetAsync(id);
        return sale is null ? NotFound() : Ok(sale);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SaleDto>> Create(SaleCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (User.IsInRole(CustomerRole))
        {
            var customer = await _customers.GetByUserAsync(userId) ?? await CreateCustomerForUserAsync(userId);
            if (customer is null)
            {
                return BadRequest("No fue posible crear un registro de cliente para este usuario.");
            }
            dto.CustomerId = customer.Id;
        }
        else if (dto.CustomerId == Guid.Empty)
        {
            return BadRequest("Debes indicar el cliente para registrar la venta.");
        }

        try
        {
            var sale = await _service.CreateAsync(dto, userId, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    private async Task<CustomerDto?> CreateCustomerForUserAsync(string userId)
    {
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = email;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            var suffix = userId.Length > 8 ? userId.Substring(0, 8) : userId;
            name = $"Cliente {suffix}";
        }

        var dto = new CustomerCreateDto
        {
            FullName = name!,
            Email = email
        };

        return await _customers.CreateAsync(dto, userId);
    }
}
