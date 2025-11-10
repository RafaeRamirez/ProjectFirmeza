using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Sales;

public class SaleItemCreateDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; }
}
