namespace Wokki.Application.Dtos.Department;

public sealed record CreateDepartmentRequest(Guid LocationId, string Name);
