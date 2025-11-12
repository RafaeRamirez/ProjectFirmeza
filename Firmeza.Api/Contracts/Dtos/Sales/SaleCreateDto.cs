using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Sales;

public class SaleCreateDto
{
    public Guid CustomerId { get; set; }

    [MinLength(1)]
    public List<SaleItemCreateDto> Items { get; set; } = new();
}
