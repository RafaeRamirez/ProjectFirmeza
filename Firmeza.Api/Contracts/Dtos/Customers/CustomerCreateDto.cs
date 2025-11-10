using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Customers;

public class CustomerCreateDto
{
    [Required]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(30)]
    public string? Phone { get; set; }
}
