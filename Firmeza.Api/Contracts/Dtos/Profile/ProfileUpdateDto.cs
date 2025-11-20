using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Profile;

public class ProfileUpdateDto
{
    [Required]
    [MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(30)]
    public string? Phone { get; set; }
}
