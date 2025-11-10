using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Auth;

public class RegisterClientDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }
}
