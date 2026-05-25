using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.OvertimeRequest;

public sealed record OvertimeRequestResponse(
    Guid Id,
    Guid ShiftAssignmentId,
    Guid EmployeeId,
    string Reason,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int? OvertimeMinutes,
    int? ElapsedMinutes,
    OvertimeStatus Status,
    Guid? ReviewedById,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote,
    DateTimeOffset CreatedAt);
