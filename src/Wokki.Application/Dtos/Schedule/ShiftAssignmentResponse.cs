namespace Wokki.Application.Dtos.Schedule;

public sealed record ShiftAssignmentResponse(
    Guid Id,
    Guid ScheduleId,
    Guid ShiftDefinitionId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid EmployeeId,
    DateOnly Date,
    string? Note,
    DateTime CreatedAt);
