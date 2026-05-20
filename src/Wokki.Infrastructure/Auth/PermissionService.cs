using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Auth;

public sealed class PermissionService(IHttpContextAccessor httpContextAccessor) : IPermissionService
{
    public Task<bool> AuthorizeAsync(string resource, string action, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Task.FromResult(false);

        // Phase 1 stub: authenticated users pass; extend for resource-based checks later
        _ = resource;
        _ = action;
        _ = resourceId;
        return Task.FromResult(true);
    }
}
