using Firmeza.Api.Contracts.Dtos.Auth;
using Firmeza.Api.Domain.Entities;

namespace Firmeza.Api.Services.Abstractions;

public interface ITokenService
{
    Task<AuthResponseDto> CreateAsync(AppUser user, CancellationToken cancellationToken = default);
}
