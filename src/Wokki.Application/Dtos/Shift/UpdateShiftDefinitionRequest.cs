namespace Wokki.Application.Dtos.Shift;

public sealed record UpdateShiftDefinitionRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RequiredRole,
    int MaxStaffPerSlot,
    string Color,
    bool IsActive);
