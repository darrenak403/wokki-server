namespace Wokki.Application.Dtos.Employee;

public sealed record EmployeeListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? DepartmentId = null,
    Guid? LocationId = null,
    bool IncludeTerminated = false,
    string? Search = null);
