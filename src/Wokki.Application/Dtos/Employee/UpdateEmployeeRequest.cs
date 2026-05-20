namespace Wokki.Application.Dtos.Employee;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Position,
    decimal HourlyRate,
    Guid DepartmentId);
