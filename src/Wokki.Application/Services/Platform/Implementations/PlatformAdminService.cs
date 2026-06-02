using System.Text.Json;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformAdminService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    IOrganizationSubscriptionService organizationSubscription,
    ICurrentUserService currentUser) : IPlatformAdminService
{
    private const string SubscriptionUpdatedAuditAction = "platform.organization.subscription.updated";
    private const string SubscriptionEntityType = "OrganizationSubscription";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        PlatformOrganizationListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PagedResponse<PlatformOrganizationResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        var page = NormalizePage(request.Page);
        var pageSize = NormalizePageSize(request.PageSize);
        var expiringWithinDays = NormalizeExpiringWithinDays(request.ExpiringWithinDays);

        var (items, total) = await unitOfWork.Organizations.ListPlatformAsync(
            page,
            pageSize,
            request.Search,
            request.Status,
            request.SortBy,
            request.SortDirection,
            cancellationToken);
        var now = DateTime.UtcNow;

        return ApiResponse<PagedResponse<PlatformOrganizationResponse>>.SuccessPagedResponse(
            items.Select(x => ToResponse(x, now, expiringWithinDays)),
            page,
            pageSize,
            total,
            AppMessages.Platform.OrganizationsListed);
    }

    public async Task<ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>> ListSubscriptionLedgerAsync(
        PlatformSubscriptionLedgerListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>.FailureResponse(
                AppMessages.Auth.Forbidden);

        var page = NormalizePage(request.Page);
        var pageSize = NormalizePageSize(request.PageSize);
        var (items, total) = await unitOfWork.OrganizationSubscriptionLedgers.ListAsync(
            request.OrganizationId,
            request.Action,
            request.From,
            request.To,
            page,
            pageSize,
            cancellationToken);

        return ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>.SuccessPagedResponse(
            items.Select(ToLedgerResponse),
            page,
            pageSize,
            total,
            AppMessages.Platform.SubscriptionLedgerListed);
    }

    public async Task<ApiResponse<PagedResponse<PlatformSupportSearchResponse>>> SearchSupportAsync(
        PlatformSupportSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PagedResponse<PlatformSupportSearchResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        var page = NormalizePage(request.Page);
        var pageSize = NormalizePageSize(request.PageSize);
        var (items, total) = await unitOfWork.Organizations.SearchPlatformSupportAsync(
            page,
            pageSize,
            request.Query,
            cancellationToken);
        var now = DateTime.UtcNow;

        return ApiResponse<PagedResponse<PlatformSupportSearchResponse>>.SuccessPagedResponse(
            items.Select(x => ToSupportSearchResponse(x, now)),
            page,
            pageSize,
            total,
            AppMessages.Platform.SupportSearchListed);
    }

    public async Task<ApiResponse<PlatformOrganizationSupportContextResponse>> GetSupportOrganizationContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformOrganizationSupportContextResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var snapshot = await unitOfWork.Organizations.GetPlatformSupportContextAsync(
            organizationId,
            cancellationToken);
        if (snapshot is null)
            return ApiResponse<PlatformOrganizationSupportContextResponse>.FailureResponse(
                AppMessages.Platform.OrganizationNotFound);

        return ApiResponse<PlatformOrganizationSupportContextResponse>.SuccessResponse(
            ToSupportContextResponse(snapshot, DateTime.UtcNow),
            AppMessages.Platform.SupportContextFound);
    }

    public async Task<ApiResponse<PlatformOrganizationResponse>> UpdateOrganizationSubscriptionAsync(
        Guid organizationId,
        UpdateOrganizationSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        if (currentUser.UserId is not { } actorUserId)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var organization = await unitOfWork.Organizations.GetTrackedByIdAsync(organizationId, cancellationToken);
        if (organization is null)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(AppMessages.Platform.OrganizationNotFound);

        var now = DateTime.UtcNow;
        var before = ToSubscriptionAuditSnapshot(organization, now);

        var requestedDurationDays = request.Enabled
            ? request.DurationDays ?? organization.SubscriptionDurationDays
            : request.DurationDays;

        if (request.Enabled && requestedDurationDays is < 1 or > 3650)
            return ApiResponse<PlatformOrganizationResponse>.FailureResponse(
                AppMessages.Platform.SubscriptionDurationRequired);

        organization.SubscriptionEnabled = request.Enabled;
        organization.SubscriptionUpdatedAt = now;

        if (request.Enabled)
        {
            organization.SubscriptionDurationDays = requestedDurationDays!.Value;
            organization.SubscriptionActivatedAt ??= now;
            organization.SubscriptionExpiresAt = now.AddDays(requestedDurationDays.Value);
        }
        else if (requestedDurationDays is { } disabledDurationDays and >= 1 and <= 3650)
        {
            organization.SubscriptionDurationDays = disabledDurationDays;
        }

        var after = ToSubscriptionAuditSnapshot(organization, now);
        var beforeJson = JsonSerializer.Serialize(before, JsonOptions);
        var afterJson = JsonSerializer.Serialize(after, JsonOptions);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.OrganizationSubscriptionLedgers.AddAsync(
                new OrganizationSubscriptionLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    Action = ResolveLedgerAction(before, after),
                    PreviousStatus = before.Status,
                    NewStatus = after.Status,
                    PreviousDurationDays = before.SubscriptionDurationDays,
                    NewDurationDays = after.SubscriptionDurationDays,
                    PreviousExpiresAt = before.SubscriptionExpiresAt,
                    NewExpiresAt = after.SubscriptionExpiresAt,
                    ChangedByUserId = actorUserId,
                    ChangedAt = now,
                    BeforeJson = beforeJson,
                    AfterJson = afterJson
                },
                cancellationToken);

            await unitOfWork.AuditLogs.AddAsync(
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organization.Id,
                    ActorUserId = actorUserId,
                    Action = SubscriptionUpdatedAuditAction,
                    EntityType = SubscriptionEntityType,
                    EntityId = organization.Id,
                    BeforeJson = beforeJson,
                    AfterJson = afterJson,
                    OccurredAt = now
                },
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var stats = await unitOfWork.Organizations.GetOrgStatsAsync(organization.Id, cancellationToken);
        var response = ToResponse(organization, now, 7, stats.UserCount, stats.LocationCount, stats.EmployeeCount);
        return ApiResponse<PlatformOrganizationResponse>.SuccessResponse(response, AppMessages.Platform.OrganizationUpdated);
    }

    private PlatformOrganizationResponse ToResponse(
        PlatformOrganizationSnapshot snapshot,
        DateTime now,
        int expiringWithinDays)
    {
        var status = organizationSubscription.GetStatus(
            snapshot.IsActive,
            snapshot.SubscriptionEnabled,
            snapshot.SubscriptionExpiresAt,
            now);
        var daysUntilExpiry = GetDaysUntilExpiry(snapshot.SubscriptionExpiresAt, now);

        return new(
            snapshot.Id,
            snapshot.Name,
            snapshot.IsActive,
            status,
            snapshot.SubscriptionEnabled,
            snapshot.SubscriptionDurationDays,
            snapshot.SubscriptionActivatedAt,
            snapshot.SubscriptionExpiresAt,
            snapshot.SubscriptionUpdatedAt,
            snapshot.CreatedAt,
            daysUntilExpiry,
            IsExpiringSoon(status, daysUntilExpiry, expiringWithinDays),
            snapshot.UserCount,
            snapshot.LocationCount,
            snapshot.EmployeeCount);
    }

    private SubscriptionAuditSnapshot ToSubscriptionAuditSnapshot(
        Wokki.Domain.Entities.Organization organization,
        DateTime now) =>
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
            organization.SubscriptionUpdatedAt);

    private static string ResolveLedgerAction(SubscriptionAuditSnapshot before, SubscriptionAuditSnapshot after)
    {
        if (!after.SubscriptionEnabled || after.Status == "Disabled")
            return "Disabled";

        if (!before.SubscriptionEnabled || before.SubscriptionExpiresAt is null)
            return "Activated";

        if (before.SubscriptionDurationDays != after.SubscriptionDurationDays)
            return "DurationChanged";

        return "Renewed";
    }

    private sealed record SubscriptionAuditSnapshot(
        Guid OrganizationId,
        string OrganizationName,
        bool IsActive,
        string Status,
        bool SubscriptionEnabled,
        int SubscriptionDurationDays,
        DateTime? SubscriptionActivatedAt,
        DateTime? SubscriptionExpiresAt,
        DateTime? SubscriptionUpdatedAt);

    private PlatformOrganizationResponse ToResponse(
        Wokki.Domain.Entities.Organization organization,
        DateTime now,
        int expiringWithinDays,
        int userCount,
        int locationCount,
        int employeeCount)
    {
        var status = organizationSubscription.GetStatus(
            organization.IsActive,
            organization.SubscriptionEnabled,
            organization.SubscriptionExpiresAt,
            now);
        var daysUntilExpiry = GetDaysUntilExpiry(organization.SubscriptionExpiresAt, now);

        return new(
            organization.Id,
            organization.Name,
            organization.IsActive,
            status,
            organization.SubscriptionEnabled,
            organization.SubscriptionDurationDays,
            organization.SubscriptionActivatedAt,
            organization.SubscriptionExpiresAt,
            organization.SubscriptionUpdatedAt,
            organization.CreatedAt,
            daysUntilExpiry,
            IsExpiringSoon(status, daysUntilExpiry, expiringWithinDays),
            userCount,
            locationCount,
            employeeCount);
    }

    private static PlatformSubscriptionLedgerEntryResponse ToLedgerResponse(
        OrganizationSubscriptionLedgerEntry entry) =>
        new(
            entry.Id,
            entry.OrganizationId,
            entry.Action,
            entry.PreviousStatus,
            entry.NewStatus,
            entry.PreviousDurationDays,
            entry.NewDurationDays,
            entry.PreviousExpiresAt,
            entry.NewExpiresAt,
            entry.ChangedByUserId,
            entry.ChangedAt);

    private PlatformSupportSearchResponse ToSupportSearchResponse(
        PlatformSupportSearchSnapshot snapshot,
        DateTime now)
    {
        var subscriptionStatus = organizationSubscription.GetStatus(
            snapshot.OrganizationIsActive,
            snapshot.SubscriptionEnabled,
            snapshot.SubscriptionExpiresAt,
            now);
        var userName = BuildUserName(snapshot.UserFirstName, snapshot.UserLastName);

        return new PlatformSupportSearchResponse(
            snapshot.MatchType,
            snapshot.OrganizationId,
            snapshot.OrganizationName,
            subscriptionStatus,
            snapshot.SubscriptionExpiresAt,
            snapshot.UserId,
            snapshot.UserEmail,
            snapshot.UserRole,
            userName,
            snapshot.UserCount,
            snapshot.LocationCount,
            snapshot.EmployeeCount,
            GetLatestOperationalActivityAt(
                snapshot.LatestScheduleCreatedAt,
                snapshot.LatestSchedulePublishedAt,
                snapshot.LatestAttendanceClockIn,
                snapshot.LatestChatMessageAt));
    }

    private PlatformOrganizationSupportContextResponse ToSupportContextResponse(
        PlatformOrganizationSupportContextSnapshot snapshot,
        DateTime now)
    {
        var subscriptionStatus = organizationSubscription.GetStatus(
            snapshot.OrganizationIsActive,
            snapshot.SubscriptionEnabled,
            snapshot.SubscriptionExpiresAt,
            now);
        var latestLedger = snapshot.LatestLedgerEntryId is null
            ? null
            : new PlatformSupportLatestLedgerResponse(
                snapshot.LatestLedgerEntryId.Value,
                snapshot.LatestLedgerAction!,
                snapshot.LatestLedgerPreviousStatus!,
                snapshot.LatestLedgerNewStatus!,
                snapshot.LatestLedgerChangedAt!.Value,
                snapshot.LatestLedgerChangedByUserId!.Value);

        return new PlatformOrganizationSupportContextResponse(
            snapshot.OrganizationId,
            snapshot.OrganizationName,
            subscriptionStatus,
            snapshot.SubscriptionDurationDays,
            snapshot.SubscriptionActivatedAt,
            snapshot.SubscriptionExpiresAt,
            snapshot.SubscriptionUpdatedAt,
            snapshot.OrganizationCreatedAt,
            snapshot.UserCount,
            snapshot.EmployeeCount,
            snapshot.LocationCount,
            snapshot.DepartmentCount,
            snapshot.LatestScheduleCreatedAt,
            snapshot.LatestSchedulePublishedAt,
            snapshot.LatestAttendanceClockIn,
            snapshot.LatestChatMessageAt,
            GetLatestOperationalActivityAt(
                snapshot.LatestScheduleCreatedAt,
                snapshot.LatestSchedulePublishedAt,
                snapshot.LatestAttendanceClockIn,
                snapshot.LatestChatMessageAt),
            latestLedger);
    }

    private static int NormalizePage(int? page) => page is null or < 1 ? 1 : page.Value;

    private static int NormalizePageSize(int? pageSize) => pageSize is null or < 1 or > 100 ? 20 : pageSize.Value;

    private static int NormalizeExpiringWithinDays(int? expiringWithinDays) =>
        expiringWithinDays is < 1 or > 3650 or null ? 7 : expiringWithinDays.Value;

    private static int? GetDaysUntilExpiry(DateTime? expiresAt, DateTime now) =>
        expiresAt is null ? null : (int)Math.Ceiling((expiresAt.Value - now).TotalDays);

    private static bool IsExpiringSoon(string status, int? daysUntilExpiry, int expiringWithinDays) =>
        string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
        && daysUntilExpiry is >= 0
        && daysUntilExpiry <= expiringWithinDays;

    private static string? BuildUserName(string? firstName, string? lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static DateTime? GetLatestOperationalActivityAt(
        DateTime? latestScheduleCreatedAt,
        DateTime? latestSchedulePublishedAt,
        DateTimeOffset? latestAttendanceClockIn,
        DateTime? latestChatMessageAt)
    {
        var values = new[]
        {
            latestScheduleCreatedAt,
            latestSchedulePublishedAt,
            latestAttendanceClockIn?.UtcDateTime,
            latestChatMessageAt
        };

        return values.Where(x => x.HasValue).Max();
    }
}
