namespace Firmeza.Api.Contracts.Dtos.Auth;

public record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    IReadOnlyCollection<string> Roles,
    Guid? CustomerId = null,
    string? CustomerName = null);
