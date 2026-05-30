namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleRebalanceConflictResponse(
    Guid AssignmentId,
    Guid EmployeeId,
    string EmployeeName,
    Guid ShiftDefinitionId,
    string ShiftName,
    string Date);

public sealed record ScheduleRebalanceHintsResponse(
    bool HasRecentPreferenceChanges,
    int ConflictCount,
    int PendingLeaveCount,
    IReadOnlyList<ScheduleRebalanceConflictResponse> Conflicts)
{
    public static ScheduleRebalanceHintsResponse Empty { get; } = new(false, 0, 0, []);
}
