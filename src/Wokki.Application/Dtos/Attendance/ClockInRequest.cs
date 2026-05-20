namespace Wokki.Application.Dtos.Attendance;

public sealed record ClockInRequest(Guid? AssignmentId = null);
