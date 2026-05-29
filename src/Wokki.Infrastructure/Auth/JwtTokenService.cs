using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Entities;
namespace Wokki.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;
    // In-process only — tokens are lost on process restart and not shared across instances (single-instance MVP deployment).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Guid> RefreshTokens = new();

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.OrganizationId is not null)
            claims.Add(new Claim("organization_id", user.OrganizationId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        RefreshTokens[token] = user.Id;
        return token;
    }

    public Guid? ValidateRefreshToken(string refreshToken) =>
        RefreshTokens.TryGetValue(refreshToken, out var userId) ? userId : null;

    // Access tokens remain valid until natural expiry after revocation — acceptable window for internal HR tool.
    public void RevokeRefreshToken(Guid userId)
    {
        var keys = RefreshTokens.Where(kv => kv.Value == userId).Select(kv => kv.Key).ToList();
        foreach (var key in keys)
            RefreshTokens.TryRemove(key, out _);
    }
}
