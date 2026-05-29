using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformAdminService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    IOrganizationSubscriptionService organizationSubscription) : IPlatformAdminService
{
    public async Task<ApiResponse<PagedResponse<PlatformUserResponse>>> ListUsersAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PagedResponse<PlatformUserResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.Users.ListPlatformAsync(
            page,
            pageSize,
            organizationId,
            role,
            search,
            cancellationToken);

        return ApiResponse<PagedResponse<PlatformUserResponse>>.SuccessPagedResponse(
            items.Select(x => new PlatformUserResponse(
                x.Id,
                x.Email,
                x.Role,
                x.OrganizationId,
                x.OrganizationName,
                x.CreatedAt)),
            page,
            pageSize,
            total,
            AppMessages.Platform.UsersListed);
    }

    public async Task<ApiResponse<PagedResponse<PlatformOrganizationResponse>>> ListOrganizationsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PagedResponse<PlatformOrganizationResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.Organizations.ListPlatformAsync(page, pageSize, search, cancellationToken);
        var now = DateTime.UtcNow;

        return ApiResponse<PagedResponse<PlatformOrganizationResponse>>.SuccessPagedResponse(
            items.Select(x => ToResponse(x, now)),
            page,
            pageSize,
            total,
            AppMessages.Platform.OrganizationsListed);
    }

    public async Task<ApiResponse<PlatformOrganizationResponse>> UpdateOrganizationSubscriptionAsync(
        Guid organizationId,
        UpdateOrganizationSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var organization = await unitOfWork.Organizations.GetTrackedByIdAsync(organizationId, cancellationToken);
        if (organization is null)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(AppMessages.Platform.OrganizationNotFound);

        var now = DateTime.UtcNow;
        organization.SubscriptionEnabled = request.Enabled;
        organization.SubscriptionUpdatedAt = now;

        if (request.Enabled)
        {
            var durationDays = request.DurationDays;
            if (durationDays is null or < 1 or > 3650)
                return ApiResponse<PlatformOrganizationResponse>.FailureResponse(
                    AppMessages.Platform.SubscriptionDurationRequired);

            organization.SubscriptionDurationDays = durationDays.Value;
            organization.SubscriptionActivatedAt ??= now;
            organization.SubscriptionExpiresAt = now.AddDays(durationDays.Value);
        }
        else if (request.DurationDays is { } disabledDurationDays and >= 1 and <= 3650)
        {
            organization.SubscriptionDurationDays = disabledDurationDays;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var stats = await unitOfWork.Organizations.GetOrgStatsAsync(organization.Id, cancellationToken);
        var response = ToResponse(organization, now, stats.UserCount, stats.LocationCount, stats.EmployeeCount);
        return ApiResponse<PlatformOrganizationResponse>.SuccessResponse(response, AppMessages.Platform.OrganizationUpdated);
    }

    private PlatformOrganizationResponse ToResponse(PlatformOrganizationSnapshot snapshot, DateTime now) =>
        new(
            snapshot.Id,
            snapshot.Name,
            snapshot.IsActive,
            organizationSubscription.GetStatus(
                snapshot.IsActive,
                snapshot.SubscriptionEnabled,
                snapshot.SubscriptionExpiresAt,
                now),
            snapshot.SubscriptionEnabled,
            snapshot.SubscriptionDurationDays,
            snapshot.SubscriptionActivatedAt,
            snapshot.SubscriptionExpiresAt,
            snapshot.SubscriptionUpdatedAt,
            snapshot.CreatedAt,
            snapshot.UserCount,
            snapshot.LocationCount,
            snapshot.EmployeeCount);

    private PlatformOrganizationResponse ToResponse(
        Wokki.Domain.Entities.Organization organization,
        DateTime now,
        int userCount,
        int locationCount,
        int employeeCount) =>
        new(
            organization.Id,
            organization.Name,
            organization.IsActive,
            organizationSubscription.GetStatus(
                organization.IsActive,
                organization.SubscriptionEnabled,
                organization.SubscriptionExpiresAt,
                now),
            organization.SubscriptionEnabled,
            organization.SubscriptionDurationDays,
            organization.SubscriptionActivatedAt,
            organization.SubscriptionExpiresAt,
            organization.SubscriptionUpdatedAt,
            organization.CreatedAt,
            userCount,
            locationCount,
            employeeCount);
}
