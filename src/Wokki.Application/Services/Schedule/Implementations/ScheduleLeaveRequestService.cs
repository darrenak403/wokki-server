using Wokki.Application.Common;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using ShiftDefinitionEntity = Wokki.Domain.Entities.ShiftDefinition;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class ScheduleLeaveRequestService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope) : IScheduleLeaveRequestService
{
    public async Task<ApiResponse<ScheduleLeaveRequestResponse>> CreateMineAsync(
        Guid userId,
        CreateScheduleLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        if (!DateOnly.TryParse(request.Date, out var date))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.InvalidDate);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(request.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null || !organizationScope.IsSameOrganization(schedule.OrganizationId))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.AlreadyPublished);

        if (schedule.DepartmentId != employee.DepartmentId)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.SchedulePreference.WrongDepartment);

        if (!IsDateInWeek(date, schedule.WeekStartDate))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.SchedulePreference.DateOutOfRange);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(request.ShiftDefinitionId, cancellationToken: cancellationToken);
        if (shift is null || !shift.IsActive || shift.LocationId != department.LocationId)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.SchedulePreference.InvalidShift);

        if (shift.DepartmentId.HasValue && shift.DepartmentId != schedule.DepartmentId)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.SchedulePreference.InvalidShift);

        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.ReasonRequired);

        if (await unitOfWork.ScheduleLeaveRequests.ExistsPendingForSlotAsync(
                schedule.Id,
                employee.Id,
                shift.Id,
                date,
                cancellationToken))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.DuplicatePending);

        var entity = new ScheduleLeaveRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = employee.OrganizationId,
            ScheduleId = schedule.Id,
            EmployeeId = employee.Id,
            ShiftDefinitionId = shift.Id,
            Date = date,
            Reason = reason,
            Status = ScheduleLeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.ScheduleLeaveRequests.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ScheduleLeaveRequestResponse>.SuccessResponse(
            Map(entity, employee, shift),
            AppMessages.ScheduleLeaveRequest.Created);
    }

    public async Task<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>> ListMineAsync(
        Guid userId,
        Guid? scheduleId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var items = await unitOfWork.ScheduleLeaveRequests.ListByEmployeeAsync(
            employee.Id,
            scheduleId,
            cancellationToken);

        var responses = await MapManyAsync(items, cancellationToken);
        return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.SuccessResponse(
            responses,
            AppMessages.ScheduleLeaveRequest.Listed);
    }

    public async Task<ApiResponse<object>> CancelMineAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var request = await unitOfWork.ScheduleLeaveRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (request is null || !organizationScope.IsSameOrganization(request.OrganizationId))
            return ApiResponse<object>.FailureResponse(AppMessages.ScheduleLeaveRequest.NotFound);

        if (request.EmployeeId != employee.Id)
            return ApiResponse<object>.FailureResponse(AppMessages.ScheduleLeaveRequest.Forbidden);

        if (request.Status != ScheduleLeaveRequestStatus.Pending)
            return ApiResponse<object>.FailureResponse(AppMessages.ScheduleLeaveRequest.InvalidTransition);

        request.Status = ScheduleLeaveRequestStatus.Cancelled;
        unitOfWork.ScheduleLeaveRequests.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.ScheduleLeaveRequest.Cancelled);
    }

    public async Task<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>> ListForReviewAsync(
        Guid userId,
        string role,
        Guid? scheduleId,
        string? status,
        IReadOnlySet<Guid>? locationIds,
        CancellationToken cancellationToken = default)
    {
        if (scheduleId is null)
            return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.ScheduleLeaveRequest.ScheduleRequired);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId.Value, cancellationToken: cancellationToken);
        if (schedule is null || !organizationScope.IsSameOrganization(schedule.OrganizationId))
            return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.Schedule.NotFound);

        if (!await CanReviewScheduleAsync(userId, role, schedule.DepartmentId, locationIds, cancellationToken))
            return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        ScheduleLeaveRequestStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ScheduleLeaveRequestStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var items = await unitOfWork.ScheduleLeaveRequests.ListByScheduleAsync(
            scheduleId.Value,
            statusFilter,
            cancellationToken);

        var responses = await MapManyAsync(items, cancellationToken);
        return ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.SuccessResponse(
            responses,
            AppMessages.ScheduleLeaveRequest.Listed);
    }

    public async Task<ApiResponse<ScheduleLeaveRequestResponse>> ApproveAsync(
        Guid id,
        Guid reviewerUserId,
        string role,
        IReadOnlySet<Guid>? locationIds,
        ReviewScheduleLeaveRequest? request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await unitOfWork.ScheduleLeaveRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (leaveRequest is null || !organizationScope.IsSameOrganization(leaveRequest.OrganizationId))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.NotFound);

        if (leaveRequest.Status != ScheduleLeaveRequestStatus.Pending)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.InvalidTransition);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(leaveRequest.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.AlreadyPublished);

        if (!await CanReviewScheduleAsync(reviewerUserId, role, schedule.DepartmentId, locationIds, cancellationToken))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        await UpsertUnavailableAndSubmitAsync(
            leaveRequest.ScheduleId,
            leaveRequest.EmployeeId,
            leaveRequest.OrganizationId,
            leaveRequest.ShiftDefinitionId,
            leaveRequest.Date,
            cancellationToken);

        var assignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(leaveRequest.ScheduleId, cancellationToken);
        var conflict = assignments.FirstOrDefault(a =>
            a.EmployeeId == leaveRequest.EmployeeId &&
            a.ShiftDefinitionId == leaveRequest.ShiftDefinitionId &&
            a.Date == leaveRequest.Date);

        if (conflict is not null)
            unitOfWork.ShiftAssignments.Remove(conflict);

        leaveRequest.Status = ScheduleLeaveRequestStatus.Approved;
        leaveRequest.ReviewedByUserId = reviewerUserId;
        leaveRequest.ReviewedAt = DateTime.UtcNow;
        unitOfWork.ScheduleLeaveRequests.Update(leaveRequest);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var employee = await unitOfWork.Employees.GetByIdAsync(leaveRequest.EmployeeId, cancellationToken: cancellationToken);
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(leaveRequest.ShiftDefinitionId, cancellationToken: cancellationToken);
        return ApiResponse<ScheduleLeaveRequestResponse>.SuccessResponse(
            Map(leaveRequest, employee!, shift!),
            AppMessages.ScheduleLeaveRequest.Approved);
    }

    public async Task<ApiResponse<ScheduleLeaveRequestResponse>> RejectAsync(
        Guid id,
        Guid reviewerUserId,
        string role,
        IReadOnlySet<Guid>? locationIds,
        ReviewScheduleLeaveRequest? request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await unitOfWork.ScheduleLeaveRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (leaveRequest is null || !organizationScope.IsSameOrganization(leaveRequest.OrganizationId))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.NotFound);

        if (leaveRequest.Status != ScheduleLeaveRequestStatus.Pending)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.ScheduleLeaveRequest.InvalidTransition);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(leaveRequest.ScheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (!await CanReviewScheduleAsync(reviewerUserId, role, schedule.DepartmentId, locationIds, cancellationToken))
            return ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        leaveRequest.Status = ScheduleLeaveRequestStatus.Rejected;
        leaveRequest.ReviewedByUserId = reviewerUserId;
        leaveRequest.ReviewedAt = DateTime.UtcNow;
        unitOfWork.ScheduleLeaveRequests.Update(leaveRequest);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var employee = await unitOfWork.Employees.GetByIdAsync(leaveRequest.EmployeeId, cancellationToken: cancellationToken);
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(leaveRequest.ShiftDefinitionId, cancellationToken: cancellationToken);
        return ApiResponse<ScheduleLeaveRequestResponse>.SuccessResponse(
            Map(leaveRequest, employee!, shift!),
            AppMessages.ScheduleLeaveRequest.Rejected);
    }

    private async Task UpsertUnavailableAndSubmitAsync(
        Guid scheduleId,
        Guid employeeId,
        Guid organizationId,
        Guid shiftDefinitionId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var submission = await unitOfWork.SchedulePreferences.GetByScheduleAndEmployeeAsync(
            scheduleId,
            employeeId,
            includeLines: true,
            cancellationToken);

        if (submission is null)
        {
            var line = new SchedulePreferenceLine
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ShiftDefinitionId = shiftDefinitionId,
                Date = date,
                PreferenceType = PreferenceType.Unavailable
            };

            submission = new SchedulePreferenceSubmission
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ScheduleId = scheduleId,
                EmployeeId = employeeId,
                Status = SchedulePreferenceStatus.Submitted,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Lines = [line]
            };
            line.SubmissionId = submission.Id;
            await unitOfWork.SchedulePreferences.AddAsync(submission, cancellationToken);
            return;
        }

        var existingLine = submission.Lines.FirstOrDefault(l =>
            l.ShiftDefinitionId == shiftDefinitionId && l.Date == date);

        if (existingLine is null)
        {
            var line = new SchedulePreferenceLine
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                SubmissionId = submission.Id,
                ShiftDefinitionId = shiftDefinitionId,
                Date = date,
                PreferenceType = PreferenceType.Unavailable
            };
            await unitOfWork.SchedulePreferences.AddLinesAsync([line], cancellationToken);
        }
        else
        {
            existingLine.PreferenceType = PreferenceType.Unavailable;
        }

        submission.Status = SchedulePreferenceStatus.Submitted;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<bool> CanReviewScheduleAsync(
        Guid userId,
        string role,
        Guid departmentId,
        IReadOnlySet<Guid>? locationIds,
        CancellationToken cancellationToken)
    {
        if (role == RoleConstants.Admin)
            return true;

        if (role != RoleConstants.Manager)
            return false;

        if (locationIds is null)
            return false;

        var department = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: cancellationToken);
        return department is not null && locationIds.Contains(department.LocationId);
    }

    private static bool IsDateInWeek(DateOnly date, DateOnly weekStart) =>
        date >= weekStart && date <= weekStart.AddDays(6);

    private async Task<IReadOnlyList<ScheduleLeaveRequestResponse>> MapManyAsync(
        IReadOnlyList<ScheduleLeaveRequest> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return [];

        var employeeIds = items.Select(i => i.EmployeeId).Distinct().ToList();
        var shiftIds = items.Select(i => i.ShiftDefinitionId).Distinct().ToList();
        var employees = (await unitOfWork.Employees.GetByIdsAsync(employeeIds, cancellationToken)).ToDictionary(e => e.Id);
        var shifts = (await unitOfWork.ShiftDefinitions.GetByIdsAsync(shiftIds, cancellationToken)).ToDictionary(s => s.Id);

        return items
            .Select(item =>
            {
                employees.TryGetValue(item.EmployeeId, out var employee);
                shifts.TryGetValue(item.ShiftDefinitionId, out var shift);
                return Map(item, employee, shift);
            })
            .ToList();
    }

    private static ScheduleLeaveRequestResponse Map(
        ScheduleLeaveRequest request,
        EmployeeEntity? employee,
        ShiftDefinitionEntity? shift)
    {
        var employeeName = employee is null
            ? "Unknown"
            : $"{employee.FirstName} {employee.LastName}".Trim();

        return new ScheduleLeaveRequestResponse(
            request.Id,
            request.ScheduleId,
            request.EmployeeId,
            employeeName,
            request.ShiftDefinitionId,
            shift?.Name ?? "Unknown",
            request.Date.ToString("yyyy-MM-dd"),
            request.Reason,
            request.Status.ToString(),
            request.ReviewedAt,
            request.CreatedAt);
    }
}
