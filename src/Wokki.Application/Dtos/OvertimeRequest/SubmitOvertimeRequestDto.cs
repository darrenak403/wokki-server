namespace Wokki.Application.Dtos.OvertimeRequest;

public sealed record SubmitOvertimeRequestDto(
    Guid ShiftAssignmentId,
    string Reason);
