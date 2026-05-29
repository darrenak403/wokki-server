namespace Wokki.Application.Dtos.Platform;

public sealed record PlatformUserResponse(
    Guid Id,
    string Email,
    string Role,
    Guid? OrganizationId,
    string? OrganizationName,
    DateTime CreatedAt);

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
    int UserCount,
    int LocationCount,
    int EmployeeCount);

public sealed record UpdateOrganizationSubscriptionRequest(bool Enabled, int? DurationDays);
