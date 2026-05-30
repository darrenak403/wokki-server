namespace Wokki.Application.Dtos.Schedule;

public sealed record CreateScheduleLeaveRequest(
    Guid ScheduleId,
    Guid ShiftDefinitionId,
    string Date,
    string Reason);

public sealed record ScheduleLeaveRequestResponse(
    Guid Id,
    Guid ScheduleId,
    Guid EmployeeId,
    string EmployeeName,
    Guid ShiftDefinitionId,
    string ShiftName,
    string Date,
    string Reason,
    string Status,
    DateTime? ReviewedAt,
    DateTime CreatedAt);

public sealed record ReviewScheduleLeaveRequest(string? Note);
