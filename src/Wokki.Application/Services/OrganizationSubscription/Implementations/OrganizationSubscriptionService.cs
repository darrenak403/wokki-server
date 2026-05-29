using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.OrganizationSubscription.Implementations;

public sealed class OrganizationSubscriptionService(IUnitOfWork unitOfWork) : IOrganizationSubscriptionService
{
    public async Task<AppMessage?> GetAccessFailureAsync(
        Guid? organizationId,
        string? role,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(role, RoleConstants.PlatformOperator, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!organizationId.HasValue)
            return AppMessages.Organization.Required;

        var organization = await unitOfWork.Organizations.GetByIdAsync(organizationId.Value, cancellationToken);
        return GetAccessFailure(organization);
    }

    public AppMessage? GetAccessFailure(Wokki.Domain.Entities.Organization? organization)
    {
        if (organization is null)
            return AppMessages.Organization.Required;

        if (!organization.IsActive)
            return AppMessages.Organization.Disabled;

        if (!organization.SubscriptionEnabled || organization.SubscriptionExpiresAt is null)
            return AppMessages.Organization.PackageNotActivated;

        return organization.SubscriptionExpiresAt.Value <= DateTime.UtcNow
            ? AppMessages.Organization.PackageExpired
            : null;
    }

    public string GetStatus(
        bool isActive,
        bool subscriptionEnabled,
        DateTime? subscriptionExpiresAt,
        DateTime utcNow)
    {
        if (!isActive)
            return "Disabled";

        if (!subscriptionEnabled || subscriptionExpiresAt is null)
            return "NotActivated";

        return subscriptionExpiresAt.Value <= utcNow ? "Expired" : "Active";
    }
}
