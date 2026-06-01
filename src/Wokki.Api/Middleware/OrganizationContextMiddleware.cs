using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Infrastructure.Tenancy;

namespace Wokki.Api.Middleware;

public sealed class OrganizationContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        ICurrentUserService currentUser,
        IOrganizationSubscriptionService organizationSubscription)
    {
        if (currentUser.IsAuthenticated)
        {
            var isPlatform = currentUser.IsPlatformOperator ||
                             string.Equals(currentUser.Role, RoleConstants.PlatformOperator, StringComparison.OrdinalIgnoreCase);
            tenantContext.SetOrganization(currentUser.OrganizationId, isPlatform);

            var skipPackageGate = OrgLessUserAccess.IsOrgLessUser(currentUser.Role, currentUser.OrganizationId)
                                  && OrgLessUserAccess.IsAllowedPath(context.Request.Path.Value);

            if (!skipPackageGate && !IsOrgSubscriptionStatusRequest(context))
            {
                var accessFailure = await organizationSubscription.GetAccessFailureAsync(
                    currentUser.OrganizationId,
                    currentUser.Role,
                    context.RequestAborted);
                if (accessFailure is not null)
                {
                    await ApiResponse<object>.FailureResponse(accessFailure).ToHttpResult().ExecuteAsync(context);
                    return;
                }
            }
        }

        await next(context);
    }

    private static bool IsOrgSubscriptionStatusRequest(HttpContext context) =>
        HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Path.StartsWithSegments("/api/v1/org/subscription", StringComparison.OrdinalIgnoreCase);
}
