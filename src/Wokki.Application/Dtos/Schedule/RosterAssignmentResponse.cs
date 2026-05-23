using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Schedule;

public sealed record RosterAssignmentResponse(
    Guid Id,
    Guid ScheduleId,
    ScheduleStatus ScheduleStatus,
    DateOnly WeekStartDate,
    Guid ShiftDefinitionId,
    string ShiftName,
    string ShiftColor,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid EmployeeId,
    string EmployeeFirstName,
    string EmployeeLastName,
    DateOnly Date,
    Guid DepartmentId,
    string DepartmentName,
    Guid LocationId,
    string LocationName,
    string? Note,
    DateTime CreatedAt);
