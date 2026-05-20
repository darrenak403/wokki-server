namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleDetailResponse(
    ScheduleResponse Schedule,
    IReadOnlyList<ShiftAssignmentResponse> Assignments);
