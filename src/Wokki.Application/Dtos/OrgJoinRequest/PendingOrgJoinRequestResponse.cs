namespace Wokki.Application.Dtos.OrgJoinRequest;

public sealed record PendingOrgJoinRequestResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    DateTime SubmittedAt);
