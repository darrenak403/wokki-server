namespace Wokki.Application.Dtos.Organization;

public sealed class OrgUsageAnalyticsRequest
{
    public int? WindowDays { get; init; }
}

public sealed record OrgUsageAnalyticsEventTypeCountResponse(string EventType, int Count);

public sealed record OrgUsageAnalyticsDailyCountResponse(DateOnly Date, string EventType, int Count);

public sealed record OrgUsageAnalyticsResponse(
    int WindowDays,
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalActivityCount,
    IReadOnlyList<OrgUsageAnalyticsEventTypeCountResponse> CountsByEventType,
    IReadOnlyList<OrgUsageAnalyticsDailyCountResponse> DailyCounts);
