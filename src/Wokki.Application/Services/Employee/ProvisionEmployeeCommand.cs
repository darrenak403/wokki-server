namespace Wokki.Application.Services.Employee;

public sealed record ProvisionEmployeeCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid DepartmentId,
    string FirstName,
    string LastName,
    string? Phone,
    decimal HourlyRate);
