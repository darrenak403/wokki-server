namespace Wokki.Application.Dtos.Employee;

public sealed record CreateEmployeeResponse(
    Guid EmployeeId,
    Guid UserId,
    string Email,
    string TemporaryPassword);
