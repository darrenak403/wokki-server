namespace Wokki.Domain.Repositories;

public sealed record PlatformUserSnapshot(
    Guid Id,
    string Email,
    string Role,
    Guid? OrganizationId,
    string? OrganizationName,
    DateTime CreatedAt);

public sealed record PlatformOrganizationSnapshot(
    Guid Id,
    string Name,
    bool IsActive,
    bool SubscriptionEnabled,
    int SubscriptionDurationDays,
    DateTime? SubscriptionActivatedAt,
    DateTime? SubscriptionExpiresAt,
    DateTime? SubscriptionUpdatedAt,
    DateTime CreatedAt,
    int UserCount,
    int LocationCount,
    int EmployeeCount);

public sealed record PlatformSupportSearchSnapshot(
    string MatchType,
    Guid OrganizationId,
    string OrganizationName,
    bool OrganizationIsActive,
    bool SubscriptionEnabled,
    int SubscriptionDurationDays,
    DateTime? SubscriptionActivatedAt,
    DateTime? SubscriptionExpiresAt,
    DateTime? SubscriptionUpdatedAt,
    DateTime OrganizationCreatedAt,
    Guid? UserId,
    string? UserEmail,
    string? UserRole,
    string? UserFirstName,
    string? UserLastName,
    DateTime? UserCreatedAt,
    int UserCount,
    int LocationCount,
    int EmployeeCount,
    DateTime? LatestScheduleCreatedAt,
    DateTime? LatestSchedulePublishedAt,
    DateTimeOffset? LatestAttendanceClockIn,
    DateTime? LatestChatMessageAt);

public sealed record PlatformOrganizationSupportContextSnapshot(
    Guid OrganizationId,
    string OrganizationName,
    bool OrganizationIsActive,
    bool SubscriptionEnabled,
    int SubscriptionDurationDays,
    DateTime? SubscriptionActivatedAt,
    DateTime? SubscriptionExpiresAt,
    DateTime? SubscriptionUpdatedAt,
    DateTime OrganizationCreatedAt,
    int UserCount,
    int EmployeeCount,
    int LocationCount,
    int DepartmentCount,
    DateTime? LatestScheduleCreatedAt,
    DateTime? LatestSchedulePublishedAt,
    DateTimeOffset? LatestAttendanceClockIn,
    DateTime? LatestChatMessageAt,
    Guid? LatestLedgerEntryId,
    string? LatestLedgerAction,
    string? LatestLedgerPreviousStatus,
    string? LatestLedgerNewStatus,
    DateTime? LatestLedgerChangedAt,
    Guid? LatestLedgerChangedByUserId);

public sealed record PlatformUsageOrgActivitySnapshot(
    Guid OrganizationId,
    string OrganizationName,
    DateTime LastActivityAt,
    int ActivityCount);

public sealed record PlatformUsageEventTypeCountSnapshot(string EventType, int Count);

public sealed record PlatformUsageWeeklyActiveSnapshot(DateOnly WeekStartDate, int ActiveOrgCount);
