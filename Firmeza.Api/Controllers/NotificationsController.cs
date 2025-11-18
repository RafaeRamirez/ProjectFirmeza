using System.Security.Claims;
using Firmeza.Api.Contracts.Dtos.ProductRequests;
using Firmeza.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ProductRequestService _service;

    public NotificationsController(ProductRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductRequestDto>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var notifications = await _service.ListByUserAsync(userId);
        return Ok(notifications);
    }

    [HttpPost]
    public async Task<ActionResult<ProductRequestBatchResultDto>> Create(ProductRequestBatchCreateDto dto)
    {
        if (dto is null || dto.Items.Count == 0)
        {
            return BadRequest("Debe enviar al menos un producto para solicitar la compra.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirst("email")?.Value
                    ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.CreateBatchAsync(userId, email, dto.Items);
        if (result.Requests.Count == 0)
        {
            return BadRequest(new
            {
                message = "No se pudo registrar la solicitud. Revisa el stock de los productos.",
                errors = result.Errors
            });
        }

        return Ok(result);
    }
}
