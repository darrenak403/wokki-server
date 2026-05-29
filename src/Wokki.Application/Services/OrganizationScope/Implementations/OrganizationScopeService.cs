using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Domain.Constants;

namespace Wokki.Application.Services.OrganizationScope.Implementations;

public sealed class OrganizationScopeService(ICurrentUserService currentUser) : IOrganizationScopeService
{
    public Guid? GetCurrentOrganizationId() => currentUser.OrganizationId;

    public bool IsPlatformOperator =>
        currentUser.IsPlatformOperator ||
        string.Equals(currentUser.Role, RoleConstants.PlatformOperator, StringComparison.OrdinalIgnoreCase);

    public bool IsSameOrganization(Guid organizationId)
    {
        if (IsPlatformOperator || currentUser.OrganizationId is null)
            return false;
        return currentUser.OrganizationId == organizationId;
    }

    public void EnsureOrganizationUser()
    {
        if (IsPlatformOperator || currentUser.OrganizationId is null)
            throw new InvalidOperationException("Organization context is required.");
    }

    public void EnsureSameOrganization(Guid organizationId)
    {
        EnsureOrganizationUser();
        if (currentUser.OrganizationId != organizationId)
            throw new InvalidOperationException("Cross-organization access is not allowed.");
    }

    public Guid RequireOrganizationId()
    {
        EnsureOrganizationUser();
        return currentUser.OrganizationId!.Value;
    }
}
