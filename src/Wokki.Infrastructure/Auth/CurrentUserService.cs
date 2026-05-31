using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;

namespace Wokki.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? OrganizationId
    {
        get
        {
            var value = User?.FindFirstValue("organization_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsPlatformOperator =>
        string.Equals(Role, RoleConstants.PlatformOperator, StringComparison.OrdinalIgnoreCase);
}
