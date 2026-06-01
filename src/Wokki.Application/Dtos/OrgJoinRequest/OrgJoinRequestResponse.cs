using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.OrgJoinRequest;

public sealed record OrgJoinRequestResponse(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    OrgJoinRequestStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectNote);
