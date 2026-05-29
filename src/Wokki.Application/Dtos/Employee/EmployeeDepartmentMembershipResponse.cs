using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Employee;

public sealed record EmployeeDepartmentMembershipResponse(
    Guid DepartmentId,
    string? DepartmentName,
    Guid? LocationId,
    string? LocationName,
    DepartmentMembershipStatus Status,
    bool IsPrimary,
    DateTime JoinedAt,
    DateTime? LeftAt);
