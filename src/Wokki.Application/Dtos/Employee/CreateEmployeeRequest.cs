using Wokki.Domain.Constants;

namespace Wokki.Application.Dtos.Employee;

public sealed record CreateEmployeeRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    decimal HourlyRate,
    Guid? DepartmentId,
    string Role = RoleConstants.User,
    string? Password = null,
    IReadOnlyList<Guid>? DepartmentIds = null,
    IReadOnlyList<Guid>? LocationIds = null);
