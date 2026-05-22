namespace Wokki.Application.Dtos.Schedule;

public sealed record ShiftAssignmentResponse(
    Guid Id,
    Guid ScheduleId,
    Guid ShiftDefinitionId,
    string ShiftName,
    string ShiftColor,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid EmployeeId,
    DateOnly Date,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LocationId,
    string? LocationName,
    string? Note,
    DateTime CreatedAt);
