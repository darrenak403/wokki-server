using Wokki.Application.Dtos.SwapRequest;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.SwapRequests;

public static class SwapRequestMapper
{
    public static SwapRequestResponse ToResponse(
        this Wokki.Domain.Entities.SwapRequest swap,
        DateOnly? requesterShiftDate = null,
        ShiftDefinition? requesterShift = null,
        DateOnly? targetShiftDate = null,
        ShiftDefinition? targetShift = null,
        Guid? departmentId = null) =>
        new(
            swap.Id,
            swap.RequesterAssignmentId,
            swap.TargetAssignmentId,
            swap.RequesterId,
            swap.TargetEmployeeId,
            swap.Status,
            swap.RequesterNote,
            swap.TargetNote,
            swap.ManagerNote,
            swap.ReviewedBy,
            requesterShiftDate,
            requesterShift?.Name,
            requesterShift?.StartTime,
            requesterShift?.EndTime,
            targetShiftDate,
            targetShift?.Name,
            targetShift?.StartTime,
            targetShift?.EndTime,
            departmentId,
            swap.CreatedAt,
            swap.UpdatedAt);

    public static Wokki.Domain.Entities.SwapRequest ToEntity(
        CreateSwapRequestRequest request,
        Guid requesterId,
        Guid targetEmployeeId,
        Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RequesterAssignmentId = request.RequesterAssignmentId,
            TargetAssignmentId = request.TargetAssignmentId,
            RequesterId = requesterId,
            TargetEmployeeId = targetEmployeeId,
            RequesterNote = request.RequesterNote,
            Status = Domain.Enums.SwapStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
