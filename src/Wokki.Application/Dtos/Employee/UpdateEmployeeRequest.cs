namespace Wokki.Application.Dtos.Employee;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string? Phone,
    decimal HourlyRate,
    Guid DepartmentId,
    IReadOnlyList<Guid>? DepartmentIds = null);
