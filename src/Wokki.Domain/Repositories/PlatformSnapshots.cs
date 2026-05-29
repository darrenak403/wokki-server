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
