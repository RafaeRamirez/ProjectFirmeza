using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Firmeza.Api.Contracts.Dtos.Auth;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Domain.Settings;
using Firmeza.Api.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Firmeza.Api.Services;

public class JwtTokenService : ITokenService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(UserManager<AppUser> userManager, IOptions<JwtSettings> options, TimeProvider? timeProvider = null)
    {
        _userManager = userManager;
        _settings = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AuthResponseDto> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_settings.ExpirationMinutes);
        var roles = await _userManager.GetRolesAsync(user);
        var rolesReadOnly = roles.ToArray();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponseDto(tokenString, expires.UtcDateTime, user.Email ?? string.Empty, rolesReadOnly);
    }
}
