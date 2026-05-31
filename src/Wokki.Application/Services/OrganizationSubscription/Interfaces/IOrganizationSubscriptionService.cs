using Wokki.Common.Utils;

namespace Wokki.Application.Services.OrganizationSubscription.Interfaces;

public interface IOrganizationSubscriptionService
{
    Task<AppMessage?> GetAccessFailureAsync(
        Guid? organizationId,
        string? role,
        CancellationToken cancellationToken = default);

    AppMessage? GetAccessFailure(Wokki.Domain.Entities.Organization? organization);

    string GetStatus(
        bool isActive,
        bool subscriptionEnabled,
        DateTime? subscriptionExpiresAt,
        DateTime utcNow);
}
