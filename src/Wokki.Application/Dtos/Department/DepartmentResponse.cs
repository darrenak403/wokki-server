namespace Wokki.Application.Dtos.Department;

public sealed record DepartmentResponse(
    Guid Id,
    Guid LocationId,
    string Name,
    bool IsActive,
    DateTime CreatedAt);
