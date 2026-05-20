namespace Wokki.Application.Dtos.Shift;

public sealed record UpdateShiftDefinitionRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RequiredRole,
    string Color,
    bool IsActive);
