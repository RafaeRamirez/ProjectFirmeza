using System.Security.Claims;
using Firmeza.Api.Contracts.Dtos.Products;
using Firmeza.Api.Contracts.Responses;
using Firmeza.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Cliente")]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetAll([FromQuery] ProductQueryParameters parameters)
    {
        var response = await _service.SearchAsync(parameters);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Cliente")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        var created = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(Guid id, ProductUpdateDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("El identificador del producto no coincide.");
        }

        var updated = await _service.UpdateAsync(dto);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductDeleteResultDto>> Delete(Guid id, [FromQuery] bool force = false)
    {
        var result = await _service.DeleteAsync(id, force);
        if (!result.Removed && !result.SetInactive)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
