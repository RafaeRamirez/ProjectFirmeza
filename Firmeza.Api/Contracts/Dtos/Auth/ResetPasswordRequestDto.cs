namespace Firmeza.Api.Contracts.Dtos.Auth;

public class ResetPasswordRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
