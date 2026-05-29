using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Infrastructure.Tenancy;

namespace Wokki.Api.Middleware;

public sealed class OrganizationContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ICurrentUserService currentUser)
    {
        if (currentUser.IsAuthenticated)
        {
            var isPlatform = currentUser.IsPlatformOperator ||
                             string.Equals(currentUser.Role, RoleConstants.PlatformOperator, StringComparison.OrdinalIgnoreCase);
            tenantContext.SetOrganization(currentUser.OrganizationId, isPlatform);
        }

        await next(context);
    }
}
