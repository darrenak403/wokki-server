using Wokki.Application.Dtos.Organization;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;
using OrganizationEntity = Wokki.Domain.Entities.Organization;

namespace Wokki.Application.Services.Stats.Implementations;

public sealed class StatsService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    IOrganizationSubscriptionService organizationSubscription) : IStatsService
{
    public async Task<ApiResponse<PlatformStatsResponse>> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformStatsResponse>.FailureResponse(AppMessages.Stats.Forbidden);

        var stats = await unitOfWork.Organizations.GetPlatformStatsAsync(cancellationToken);
        return ApiResponse<PlatformStatsResponse>.SuccessResponse(
            new PlatformStatsResponse(stats.OrganizationCount, stats.UserCount, stats.LocationCount, stats.EmployeeCount),
            AppMessages.Stats.PlatformFound);
    }

    public async Task<ApiResponse<OrgStatsResponse>> GetOrgStatsAsync(CancellationToken cancellationToken = default)
    {
        if (organizationScope.IsPlatformOperator)
            return ApiResponse<OrgStatsResponse>.FailureResponse(AppMessages.Stats.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var organization = await unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
            return ApiResponse<OrgStatsResponse>.FailureResponse(AppMessages.Organization.Required);

        var stats = await unitOfWork.Organizations.GetOrgStatsAsync(organizationId, cancellationToken);
        var subscription = BuildSubscriptionSnapshot(organization);

        return ApiResponse<OrgStatsResponse>.SuccessResponse(
            new OrgStatsResponse(
                organizationId,
                stats.UserCount,
                stats.LocationCount,
                stats.DepartmentCount,
                stats.EmployeeCount,
                stats.ActiveLocationMembershipCount,
                subscription.Status,
                subscription.DurationDays,
                subscription.ExpiresAt,
                subscription.DaysRemaining),
            AppMessages.Stats.OrgFound);
    }

    public async Task<ApiResponse<OrgSubscriptionResponse>> GetOrgSubscriptionAsync(
        CancellationToken cancellationToken = default)
    {
        if (organizationScope.IsPlatformOperator)
            return ApiResponse<OrgSubscriptionResponse>.FailureResponse(AppMessages.Stats.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var organization = await unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
            return ApiResponse<OrgSubscriptionResponse>.FailureResponse(AppMessages.Organization.Required);

        var subscription = BuildSubscriptionSnapshot(organization);

        return ApiResponse<OrgSubscriptionResponse>.SuccessResponse(
            new OrgSubscriptionResponse(
                organizationId,
                subscription.Status,
                subscription.DurationDays,
                subscription.ExpiresAt,
                subscription.DaysRemaining),
            AppMessages.Stats.OrgSubscriptionFound);
    }

    private SubscriptionSnapshot BuildSubscriptionSnapshot(OrganizationEntity organization)
    {
        var now = DateTime.UtcNow;
        var status = organizationSubscription.GetStatus(
            organization.IsActive,
            organization.SubscriptionEnabled,
            organization.SubscriptionExpiresAt,
            now);

        int? daysRemaining = null;
        if (organization.SubscriptionExpiresAt is not null)
        {
            var totalDays = (organization.SubscriptionExpiresAt.Value - now).TotalDays;
            daysRemaining = totalDays < 0 ? 0 : (int)Math.Ceiling(totalDays);
        }

        return new SubscriptionSnapshot(
            status,
            organization.SubscriptionDurationDays,
            organization.SubscriptionExpiresAt,
            daysRemaining);
    }

    private sealed record SubscriptionSnapshot(
        string Status,
        int DurationDays,
        DateTime? ExpiresAt,
        int? DaysRemaining);
}
