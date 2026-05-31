using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.SwapPost;

public sealed record SwapPostAuthorDto(
    Guid EmployeeId,
    string DisplayName);

public sealed record SwapPostShiftDto(
    Guid AssignmentId,
    DateOnly Date,
    Guid ShiftDefinitionId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SwapPostResponse(
    Guid Id,
    Guid ScheduleId,
    SwapPostType Type,
    SwapPostStatus Status,
    SwapPostAuthorDto Author,
    SwapPostShiftDto OfferedShift,
    SwapPostShiftDto? AcceptedShift,
    SwapPostAuthorDto? AcceptedBy,
    string? Note,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool CanAccept,
    bool CanCancel,
    bool IsMine,
    Guid? DepartmentId = null,
    string? DepartmentName = null);
