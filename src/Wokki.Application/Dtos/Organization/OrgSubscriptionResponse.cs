namespace Wokki.Application.Dtos.Organization;

public sealed record OrgSubscriptionResponse(
    Guid OrganizationId,
    string SubscriptionStatus,
    int SubscriptionDurationDays,
    DateTime? SubscriptionExpiresAt,
    int? DaysRemaining);
