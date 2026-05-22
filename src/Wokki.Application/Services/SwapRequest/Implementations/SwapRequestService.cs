using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.SwapRequest;
using Wokki.Application.Mappings.SwapRequests;
using SwapMapper = Wokki.Application.Mappings.SwapRequests.SwapRequestMapper;
using Wokki.Application.Services.SwapRequest.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using SwapRequestEntity = Wokki.Domain.Entities.SwapRequest;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;
using ShiftDefinitionEntity = Wokki.Domain.Entities.ShiftDefinition;

namespace Wokki.Application.Services.SwapRequest.Implementations;

public sealed class SwapRequestService(IUnitOfWork unitOfWork, INotificationService notifications) : ISwapRequestService
{
    public async Task<ApiResponse<SwapRequestResponse>> CreateAsync(
        CreateSwapRequestRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var requesterEmployee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (requesterEmployee is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NoEmployeeProfile);

        var requesterContext = await LoadAssignmentContextAsync(request.RequesterAssignmentId, cancellationToken);
        if (requesterContext is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.AssignmentNotFound);

        if (requesterContext.Assignment.EmployeeId != requesterEmployee.Id)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotOwner);

        if (requesterContext.Schedule.Status != ScheduleStatus.Published)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.ScheduleNotPublished);

        var targetContext = await LoadAssignmentContextAsync(request.TargetAssignmentId, cancellationToken);
        if (targetContext is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.AssignmentNotFound);

        if (targetContext.Schedule.Status != ScheduleStatus.Published)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.ScheduleNotPublished);

        if (targetContext.Assignment.EmployeeId == requesterEmployee.Id)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.SameEmployee);

        var timeZone = SwapCutoffRules.ResolveTimeZone(requesterContext.Location.TimeZone);
        if (SwapCutoffRules.IsCutoffExceeded(requesterContext.Assignment.Date, timeZone, isCreate: true))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.CutoffExceeded);

        if (await unitOfWork.SwapRequests.HasOpenSwapForAssignmentAsync(request.RequesterAssignmentId, cancellationToken))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.OpenSwapExists);

        var entity = SwapMapper.ToEntity(request, requesterEmployee.Id, targetContext.Assignment.EmployeeId);
        await unitOfWork.SwapRequests.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyEmployeeSafeAsync(
            targetContext.Assignment.EmployeeId,
            "swap.request.created",
            new { entity.Id },
            cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(entity, cancellationToken),
            AppMessages.Swap.Created);
    }

    public async Task<ApiResponse<PagedResponse<SwapRequestResponse>>> ListAsync(
        SwapRequestListRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.SwapRequests.ListAsync(
            page,
            pageSize,
            request.Status,
            request.DepartmentId,
            request.WeekStartDate,
            cancellationToken);

        var responses = new List<SwapRequestResponse>(items.Count);
        foreach (var item in items)
            responses.Add(await MapResponseAsync(item, cancellationToken));

        return ApiResponse<PagedResponse<SwapRequestResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.Swap.Listed);
    }

    public async Task<ApiResponse<IReadOnlyList<SwapRequestResponse>>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<SwapRequestResponse>>.FailureResponse(AppMessages.Swap.NoEmployeeProfile);

        var items = await unitOfWork.SwapRequests.ListByEmployeeAsync(employee.Id, cancellationToken);
        var responses = new List<SwapRequestResponse>(items.Count);
        foreach (var item in items)
            responses.Add(await MapResponseAsync(item, cancellationToken));

        return ApiResponse<IReadOnlyList<SwapRequestResponse>>.SuccessResponse(responses, AppMessages.Swap.Listed);
    }

    public async Task<ApiResponse<SwapRequestResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        string? role,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        if (!await CanAccessAsync(role, userId, swap, cancellationToken))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.Forbidden);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.Found);
    }

    public async Task<ApiResponse<SwapRequestResponse>> AcceptAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        var targetEmployee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (targetEmployee is null || targetEmployee.Id != swap.TargetEmployeeId)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.Forbidden);

        if (swap.Status != SwapStatus.Pending)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition);

        var requesterContext = await LoadAssignmentContextAsync(swap.RequesterAssignmentId, cancellationToken);
        if (requesterContext is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.AssignmentNotFound);

        var timeZone = SwapCutoffRules.ResolveTimeZone(requesterContext.Location.TimeZone);
        if (SwapCutoffRules.IsCutoffExceeded(requesterContext.Assignment.Date, timeZone, isCreate: false))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.CutoffExceeded);

        if (await unitOfWork.SwapRequests.HasPeerAcceptedForAssignmentAsync(swap.RequesterAssignmentId, swap.Id, cancellationToken)
            || await unitOfWork.SwapRequests.HasPeerAcceptedForAssignmentAsync(swap.TargetAssignmentId, swap.Id, cancellationToken))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.PeerAcceptedExists);

        swap.TargetNote = request?.Note;
        swap.Status = SwapStatus.PeerAccepted;
        swap.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            unitOfWork.SwapRequests.Update(swap);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var applied = await ApplyAssignmentSwapAsync(swap, cancellationToken);
            if (!applied.Success)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return applied.Response!;
            }

            swap.Status = SwapStatus.ManagerApproved;
            swap.UpdatedAt = DateTime.UtcNow;
            unitOfWork.SwapRequests.Update(swap);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await NotifyEmployeeSafeAsync(swap.RequesterId, "swap.peer.accepted", new { swap.Id }, cancellationToken);
        await NotifyEmployeeSafeAsync(swap.TargetEmployeeId, "swap.peer.accepted", new { swap.Id }, cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.Accepted);
    }

    public async Task<ApiResponse<SwapRequestResponse>> DeclineAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        var targetEmployee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (targetEmployee is null || targetEmployee.Id != swap.TargetEmployeeId)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.Forbidden);

        if (swap.Status != SwapStatus.Pending)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition);

        var requesterContext = await LoadAssignmentContextAsync(swap.RequesterAssignmentId, cancellationToken);
        if (requesterContext is not null)
        {
            var timeZone = SwapCutoffRules.ResolveTimeZone(requesterContext.Location.TimeZone);
            if (SwapCutoffRules.IsCutoffExceeded(requesterContext.Assignment.Date, timeZone, isCreate: false))
                return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.CutoffExceeded);
        }

        swap.TargetNote = request?.Note;
        swap.Status = SwapStatus.PeerDeclined;
        swap.UpdatedAt = DateTime.UtcNow;
        unitOfWork.SwapRequests.Update(swap);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyEmployeeSafeAsync(swap.RequesterId, "swap.peer.declined", new { swap.Id }, cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.Declined);
    }

    public async Task<ApiResponse<SwapRequestResponse>> CancelAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        var requesterEmployee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (requesterEmployee is null || requesterEmployee.Id != swap.RequesterId)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.Forbidden);

        if (swap.Status != SwapStatus.Pending)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition);

        swap.RequesterNote = request?.Note ?? swap.RequesterNote;
        swap.Status = SwapStatus.Cancelled;
        swap.UpdatedAt = DateTime.UtcNow;
        unitOfWork.SwapRequests.Update(swap);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyEmployeeSafeAsync(swap.TargetEmployeeId, "swap.cancelled", new { swap.Id }, cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.Cancelled);
    }

    public async Task<ApiResponse<SwapRequestResponse>> OverrideApproveAsync(
        Guid id,
        Guid managerUserId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        if (swap.Status is not (SwapStatus.Pending or SwapStatus.PeerDeclined))
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition);

        swap.ManagerNote = request?.Note;
        swap.ReviewedBy = managerUserId;
        swap.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            unitOfWork.SwapRequests.Update(swap);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var applied = await ApplyAssignmentSwapAsync(swap, cancellationToken);
            if (!applied.Success)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return applied.Response!;
            }

            swap.Status = SwapStatus.ManagerApproved;
            swap.UpdatedAt = DateTime.UtcNow;
            unitOfWork.SwapRequests.Update(swap);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await NotifyEmployeeSafeAsync(swap.RequesterId, "swap.manager.approved", new { swap.Id }, cancellationToken);
        await NotifyEmployeeSafeAsync(swap.TargetEmployeeId, "swap.manager.approved", new { swap.Id }, cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.OverrideApproved);
    }

    public async Task<ApiResponse<SwapRequestResponse>> OverrideRejectAsync(
        Guid id,
        Guid managerUserId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (swap is null)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.NotFound);

        if (swap.Status is SwapStatus.ManagerApproved or SwapStatus.Cancelled or SwapStatus.ManagerRejected)
            return ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition);

        swap.ManagerNote = request?.Note;
        swap.ReviewedBy = managerUserId;
        swap.Status = SwapStatus.ManagerRejected;
        swap.UpdatedAt = DateTime.UtcNow;
        unitOfWork.SwapRequests.Update(swap);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyEmployeeSafeAsync(swap.RequesterId, "swap.manager.rejected", new { swap.Id }, cancellationToken);
        await NotifyEmployeeSafeAsync(swap.TargetEmployeeId, "swap.manager.rejected", new { swap.Id }, cancellationToken);

        return ApiResponse<SwapRequestResponse>.SuccessResponse(
            await MapResponseAsync(swap, cancellationToken),
            AppMessages.Swap.OverrideRejected);
    }

    private async Task<(bool Success, ApiResponse<SwapRequestResponse>? Response)> ApplyAssignmentSwapAsync(
        SwapRequestEntity swap,
        CancellationToken cancellationToken)
    {
        var requesterAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
            swap.RequesterAssignmentId,
            track: true,
            cancellationToken);
        var targetAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
            swap.TargetAssignmentId,
            track: true,
            cancellationToken);

        if (requesterAssignment is null || targetAssignment is null)
            return (false, ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.AssignmentNotFound));

        if (requesterAssignment.EmployeeId != swap.RequesterId
            || targetAssignment.EmployeeId != swap.TargetEmployeeId)
            return (false, ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Swap.InvalidTransition));

        await unitOfWork.ShiftAssignments.SwapEmployeeIdsAsync(
            requesterAssignment.Id,
            targetAssignment.Id,
            cancellationToken);

        return (true, null);
    }

    private async Task<SwapRequestResponse> MapResponseAsync(
        SwapRequestEntity swap,
        CancellationToken cancellationToken)
    {
        var requesterAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
            swap.RequesterAssignmentId,
            cancellationToken: cancellationToken);
        var targetAssignment = await unitOfWork.ShiftAssignments.GetByIdAsync(
            swap.TargetAssignmentId,
            cancellationToken: cancellationToken);
        Guid? departmentId = null;
        ShiftDefinitionEntity? requesterShift = null;
        ShiftDefinitionEntity? targetShift = null;

        if (requesterAssignment is not null)
        {
            requesterShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                requesterAssignment.ShiftDefinitionId,
                cancellationToken: cancellationToken);
            var schedule = await unitOfWork.Schedules.GetByIdAsync(requesterAssignment.ScheduleId, cancellationToken: cancellationToken);
            departmentId = schedule?.DepartmentId;
        }

        if (targetAssignment is not null)
        {
            targetShift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                targetAssignment.ShiftDefinitionId,
                cancellationToken: cancellationToken);
        }

        return swap.ToResponse(
            requesterAssignment?.Date,
            requesterShift,
            targetAssignment?.Date,
            targetShift,
            departmentId);
    }

    private async Task<AssignmentContext?> LoadAssignmentContextAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(assignmentId, cancellationToken: cancellationToken);
        if (assignment is null)
            return null;

        var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return null;

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return null;

        var location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);
        if (location is null)
            return null;

        return new AssignmentContext(assignment, schedule, department, location);
    }

    private async Task NotifyEmployeeSafeAsync(
        Guid employeeId,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return;

        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken);
        if (user is null)
            return;

        try
        {
            await notifications.SendAsync(user.Id, eventName, payload, cancellationToken);
        }
        catch
        {
            // Notifications must not roll back core workflow.
        }
    }

    private async Task<bool> CanAccessAsync(
        string? role,
        Guid userId,
        SwapRequestEntity swap,
        CancellationToken cancellationToken)
    {
        if (role is RoleConstants.Admin or RoleConstants.Manager)
            return true;

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return false;

        return swap.RequesterId == employee.Id || swap.TargetEmployeeId == employee.Id;
    }

    private sealed record AssignmentContext(
        ShiftAssignment Assignment,
        ScheduleEntity Schedule,
        DepartmentEntity Department,
        LocationEntity Location);
}
