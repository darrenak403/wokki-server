namespace Wokki.Application.Dtos.Shift;

public sealed record CreateShiftDefinitionRequest(
    Guid LocationId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RequiredRole,
    string Color = "#3B82F6",
    Guid? DepartmentId = null);
