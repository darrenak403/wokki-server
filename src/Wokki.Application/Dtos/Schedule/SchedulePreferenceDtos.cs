namespace Wokki.Application.Dtos.Schedule;

public sealed record SchedulePreferenceLineInput(
    Guid ShiftDefinitionId,
    DateOnly Date,
    string PreferenceType);

public sealed record SaveSchedulePreferencesRequest(
    IReadOnlyList<SchedulePreferenceLineInput> Lines);

public sealed record SchedulePreferenceLineResponse(
    Guid ShiftDefinitionId,
    DateOnly Date,
    string PreferenceType);

public sealed record MySchedulePreferenceResponse(
    Guid ScheduleId,
    Guid? SubmissionId,
    string Status,
    IReadOnlyList<SchedulePreferenceLineResponse> Lines);

public sealed record EmployeeDraftScheduleResponse(
    Guid ScheduleId,
    DateOnly WeekStartDate,
    IReadOnlyList<SchedulePreferenceBoardShiftColumn> Shifts);

public sealed record SchedulePreferenceBoardResponse(
    Guid ScheduleId,
    DateOnly WeekStartDate,
    int EmployeeCount,
    int SubmittedCount,
    IReadOnlyList<SchedulePreferenceBoardEmployeeRow> Employees,
    IReadOnlyList<SchedulePreferenceBoardShiftColumn> Shifts);

public sealed record SchedulePreferenceBoardEmployeeRow(
    Guid EmployeeId,
    string EmployeeName,
    string Position,
    Guid? JobPositionId,
    string? Status,
    IReadOnlyList<SchedulePreferenceCellResponse> Cells);

public sealed record SchedulePreferenceCellResponse(
    Guid ShiftDefinitionId,
    DateOnly Date,
    string? PreferenceType);

public sealed record SchedulePreferenceBoardShiftColumn(
    Guid ShiftDefinitionId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxStaffPerSlot);
