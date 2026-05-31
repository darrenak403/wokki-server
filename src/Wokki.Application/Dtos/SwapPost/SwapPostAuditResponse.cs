using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.SwapPost;

public sealed record SwapPostAuditResponse(
    Guid Id,
    SwapPostType Type,
    DateTime CompletedAt,
    SwapPostAuthorDto Author,
    SwapPostAuthorDto? AcceptedBy,
    SwapPostShiftDto OfferedShift,
    SwapPostShiftDto? AcceptedShift,
    Guid ScheduleId,
    Guid LocationId,
    Guid DepartmentId,
    string? DepartmentName = null);
