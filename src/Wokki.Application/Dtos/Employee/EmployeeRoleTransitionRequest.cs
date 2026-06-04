namespace Wokki.Application.Dtos.Employee;

public sealed record EmployeeRoleTransitionRequest(
    string TargetRole,
    Guid? LocationId = null,
    Guid? DepartmentId = null,
    decimal? HourlyRate = null);
