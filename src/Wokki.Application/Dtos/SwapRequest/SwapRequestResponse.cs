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
    DateOnly? TargetShiftDate,
    Guid? DepartmentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
