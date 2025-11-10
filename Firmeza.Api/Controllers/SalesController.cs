using System.Security.Claims;
using Firmeza.Api.Contracts.Dtos.Sales;
using Firmeza.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly SaleService _service;

    public SalesController(SaleService service)
    {
        _service = service;
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
    [Authorize(Roles = "SuperAdmin,Admin,Cliente")]
    public async Task<ActionResult<SaleDto>> Create(SaleCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
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
}
