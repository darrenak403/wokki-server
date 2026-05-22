using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.SwapRequest;

public sealed record SwapRequestResponse(
    Guid Id,
    Guid RequesterAssignmentId,
    Guid TargetAssignmentId,
    Guid RequesterId,
    Guid TargetEmployeeId,
    SwapStatus Status,
    string? RequesterNote,
    string? TargetNote,
    string? ManagerNote,
    Guid? ReviewedBy,
    DateOnly? RequesterShiftDate,
    string? RequesterShiftName,
    TimeOnly? RequesterStartTime,
    TimeOnly? RequesterEndTime,
    DateOnly? TargetShiftDate,
    string? TargetShiftName,
    TimeOnly? TargetStartTime,
    TimeOnly? TargetEndTime,
    Guid? DepartmentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
