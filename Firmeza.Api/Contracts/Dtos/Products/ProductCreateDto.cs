using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Products;

public class ProductCreateDto
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 1000000)]
    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
}
