namespace Wokki.Application.Dtos.Shift;

public sealed record ShiftDefinitionResponse(
    Guid Id,
    Guid LocationId,
    Guid? DepartmentId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RequiredRole,
    int MaxStaffPerSlot,
    string Color,
    bool IsActive,
    DateTime CreatedAt);
