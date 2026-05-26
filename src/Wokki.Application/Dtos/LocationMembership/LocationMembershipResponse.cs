using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.LocationMembership;

public sealed record LocationMembershipResponse(
    Guid Id,
    Guid LocationId,
    string LocationName,
    Guid EmployeeId,
    string EmployeeFirstName,
    string EmployeeLastName,
    LocationMembershipStatus Status,
    DateTime RequestedAt,
    Guid? ReviewedById,
    DateTime? ReviewedAt,
    string? Note);
