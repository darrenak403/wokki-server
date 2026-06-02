namespace Wokki.Application.Dtos.Platform;

public sealed record PlatformUserResponse(
    Guid Id,
    string Email,
    string Role,
    Guid? OrganizationId,
    string? OrganizationName,
    DateTime CreatedAt);

public sealed class PlatformOrganizationListRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public int? ExpiringWithinDays { get; init; }
}

public sealed record PlatformOrganizationResponse(
    Guid Id,
    string Name,
    bool IsActive,
    string SubscriptionStatus,
    bool SubscriptionEnabled,
    int SubscriptionDurationDays,
    DateTime? SubscriptionActivatedAt,
    DateTime? SubscriptionExpiresAt,
    DateTime? SubscriptionUpdatedAt,
    DateTime CreatedAt,
    int? DaysUntilExpiry,
    bool IsExpiringSoon,
    int UserCount,
    int LocationCount,
    int EmployeeCount);

public sealed record UpdateOrganizationSubscriptionRequest(bool Enabled, int? DurationDays);

public sealed class PlatformSubscriptionLedgerListRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? Action { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed record PlatformSubscriptionLedgerEntryResponse(
    Guid Id,
    Guid OrganizationId,
    string Action,
    string PreviousStatus,
    string NewStatus,
    int PreviousDurationDays,
    int NewDurationDays,
    DateTime? PreviousExpiresAt,
    DateTime? NewExpiresAt,
    Guid ChangedByUserId,
    DateTime ChangedAt);

public sealed class PlatformSupportSearchRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Query { get; init; }
}

public sealed record PlatformSupportSearchResponse(
    string MatchType,
    Guid OrganizationId,
    string OrganizationName,
    string SubscriptionStatus,
    DateTime? SubscriptionExpiresAt,
    Guid? UserId,
    string? UserEmail,
    string? UserRole,
    string? UserName,
    int UserCount,
    int LocationCount,
    int EmployeeCount,
    DateTime? LatestOperationalActivityAt);

public sealed record PlatformSupportLatestLedgerResponse(
    Guid Id,
    string Action,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt,
    Guid ChangedByUserId);

public sealed record PlatformOrganizationSupportContextResponse(
    Guid OrganizationId,
    string OrganizationName,
    string SubscriptionStatus,
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
    DateTime? LatestOperationalActivityAt,
    PlatformSupportLatestLedgerResponse? LatestSubscriptionLedgerEntry);

public sealed record PlatformDiagnosticFailureResponse(
    DateTime? LastFailureAtUtc,
    string? LastFailureCode,
    string? LastFailureMessage);

public sealed record PlatformDiagnosticComponentResponse(
    string Name,
    string Status,
    DateTime CheckedAtUtc,
    PlatformDiagnosticFailureResponse LastFailure);

public sealed record PlatformHealthResponse(
    string Status,
    DateTime CheckedAtUtc,
    IReadOnlyList<PlatformDiagnosticComponentResponse> Components);

public sealed class PlatformUsageAnalyticsRequest
{
    public int? WindowDays { get; init; }
    public Guid? OrganizationId { get; init; }
}

public sealed record PlatformUsageOrgActivityResponse(
    Guid OrganizationId,
    string OrganizationName,
    DateTime LastActivityAt,
    int ActivityCount);

public sealed record PlatformUsageEventTypeCountResponse(string EventType, int Count);

public sealed record PlatformUsageWeeklyActiveResponse(DateOnly WeekStartDate, int ActiveOrgCount);

public sealed record PlatformUsageAnalyticsResponse(
    int WindowDays,
    DateTime FromUtc,
    DateTime ToUtc,
    int ActiveOrganizationCount,
    IReadOnlyList<PlatformUsageOrgActivityResponse> ActiveOrganizations,
    IReadOnlyList<PlatformUsageEventTypeCountResponse> CountsByEventType,
    IReadOnlyList<PlatformUsageWeeklyActiveResponse> WeeklyActiveOrganizations,
    IReadOnlyList<PlatformUsageOrgActivityResponse> TopOrganizations);
