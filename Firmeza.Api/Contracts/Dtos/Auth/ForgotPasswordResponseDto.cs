namespace Firmeza.Api.Contracts.Dtos.Auth;

public class ForgotPasswordResponseDto
{
    public bool EmailSent { get; set; }
    public bool ShowResetLink { get; set; }
    public string? Message { get; set; }
    public string? ResetLink { get; set; }
    public string? UserId { get; set; }
    public string? Token { get; set; }
}
