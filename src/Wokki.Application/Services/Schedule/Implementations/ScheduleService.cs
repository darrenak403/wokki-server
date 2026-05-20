using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Mappings.Schedules;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class ScheduleService(IUnitOfWork unitOfWork, INotificationService notifications) : IScheduleService
{
    public async Task<ApiResponse<PagedResponse<ScheduleResponse>>> ListAsync(
        ScheduleListRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.Schedules.ListAsync(
            page,
            pageSize,
            request.DepartmentId,
            request.WeekStartDate,
            cancellationToken);

        return ApiResponse<PagedResponse<ScheduleResponse>>.SuccessPagedResponse(
            items.Select(s => s.ToResponse()),
            page,
            pageSize,
            total,
            AppMessages.Schedule.Listed);
    }

    public async Task<ApiResponse<ScheduleDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleDetailResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        var assignments = await BuildAssignmentResponsesAsync(id, cancellationToken);
        return ApiResponse<ScheduleDetailResponse>.SuccessResponse(
            new ScheduleDetailResponse(schedule.ToResponse(), assignments),
            AppMessages.Schedule.Found);
    }

    public async Task<ApiResponse<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!ScheduleRules.IsMonday(request.WeekStartDate))
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.WeekNotMonday);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var existing = await unitOfWork.Schedules.GetByDepartmentAndWeekAsync(
            request.DepartmentId,
            request.WeekStartDate,
            cancellationToken);
        if (existing is not null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.AlreadyExists);

        var entity = request.ToEntity(createdByUserId);
        await unitOfWork.Schedules.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ScheduleResponse>.SuccessResponse(entity.ToResponse(), AppMessages.Schedule.Created);
    }

    public async Task<ApiResponse<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (schedule is null || schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<ScheduleResponse>.FailureResponse(
                schedule is null ? AppMessages.Schedule.NotFound : AppMessages.Schedule.NotDraft);

        if (!ScheduleRules.IsMonday(request.WeekStartDate))
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.WeekNotMonday);

        var duplicate = await unitOfWork.Schedules.GetByDepartmentAndWeekAsync(
            request.DepartmentId,
            request.WeekStartDate,
            cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.AlreadyExists);

        schedule.ApplyUpdate(request);
        unitOfWork.Schedules.Update(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ScheduleResponse>.SuccessResponse(schedule.ToResponse(), AppMessages.Schedule.Updated);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.NotDraft);

        unitOfWork.Schedules.Remove(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Schedule.Deleted);
    }

    public async Task<ApiResponse<ScheduleResponse>> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status == ScheduleStatus.Published)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.AlreadyPublished);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotDraft);

        schedule.Status = ScheduleStatus.Published;
        schedule.PublishedAt = DateTime.UtcNow;
        unitOfWork.Schedules.Update(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var assignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(id, cancellationToken);
        foreach (var employeeId in assignments.Select(a => a.EmployeeId).Distinct())
        {
            await NotifyEmployeeSafeAsync(
                employeeId,
                "schedule.published",
                new { scheduleId = schedule.Id, schedule.WeekStartDate },
                cancellationToken);
        }

        return ApiResponse<ScheduleResponse>.SuccessResponse(schedule.ToResponse(), AppMessages.Schedule.Published);
    }

    public async Task<ApiResponse<ScheduleResponse>> UnpublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status == ScheduleStatus.Draft)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotPublished);

        if (schedule.Status != ScheduleStatus.Published)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotPublished);

        schedule.Status = ScheduleStatus.Draft;
        schedule.PublishedAt = null;
        unitOfWork.Schedules.Update(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ScheduleResponse>.SuccessResponse(schedule.ToResponse(), AppMessages.Schedule.Unpublished);
    }

    public async Task<ApiResponse<ScheduleResponse>> CopyAsync(
        Guid id,
        CopyScheduleRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!ScheduleRules.IsMonday(request.TargetWeekStartDate))
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.WeekNotMonday);

        var source = await unitOfWork.Schedules.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (source is null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        var existing = await unitOfWork.Schedules.GetByDepartmentAndWeekAsync(
            source.DepartmentId,
            request.TargetWeekStartDate,
            cancellationToken);
        if (existing is not null)
            return ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Schedule.AlreadyExists);

        var sourceAssignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(id, cancellationToken);
        var weekOffset = request.TargetWeekStartDate.DayNumber - source.WeekStartDate.DayNumber;

        var target = new ScheduleEntity
        {
            Id = Guid.NewGuid(),
            DepartmentId = source.DepartmentId,
            WeekStartDate = request.TargetWeekStartDate,
            Status = ScheduleStatus.Draft,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Schedules.AddAsync(target, cancellationToken);

            foreach (var assignment in sourceAssignments)
            {
                var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                    assignment.ShiftDefinitionId,
                    cancellationToken: cancellationToken);
                if (shift is null || !shift.IsActive)
                    continue;

                var targetDate = assignment.Date.AddDays(weekOffset);
                if (await unitOfWork.ShiftAssignments.ExistsAsync(
                        target.Id,
                        assignment.ShiftDefinitionId,
                        assignment.EmployeeId,
                        targetDate,
                        cancellationToken))
                    continue;

                var clone = new ShiftAssignmentEntity
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = target.Id,
                    ShiftDefinitionId = assignment.ShiftDefinitionId,
                    EmployeeId = assignment.EmployeeId,
                    Date = targetDate,
                    Note = assignment.Note,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.ShiftAssignments.AddAsync(clone, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ApiResponse<ScheduleResponse>.SuccessResponse(target.ToResponse(), AppMessages.Schedule.Copied);
    }

    public async Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> ListAssignmentsAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Schedule.NotFound);

        var assignments = await BuildAssignmentResponsesAsync(scheduleId, cancellationToken);
        return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.SuccessResponse(
            assignments,
            AppMessages.Schedule.AssignmentListed);
    }

    public async Task<ApiResponse<ShiftAssignmentResponse>> CreateAssignmentAsync(
        Guid scheduleId,
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.NotDraft);

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(request.ShiftDefinitionId, cancellationToken: cancellationToken);
        if (shift is null || !shift.IsActive)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.ShiftInactive);

        var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken: cancellationToken);
        if (employee is null || employee.TerminatedAt is not null)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.EmployeeNotFound);

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        if (request.Date < schedule.WeekStartDate || request.Date > weekEnd)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Validation.Failed);

        if (await unitOfWork.ShiftAssignments.ExistsAsync(
                scheduleId,
                request.ShiftDefinitionId,
                request.EmployeeId,
                request.Date,
                cancellationToken))
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.AssignmentDuplicate);

        if (await unitOfWork.ShiftAssignments.HasTimeOverlapAsync(
                scheduleId,
                request.EmployeeId,
                request.Date,
                shift.StartTime,
                shift.EndTime,
                cancellationToken: cancellationToken))
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(AppMessages.Schedule.AssignmentConflict);

        var assignment = request.ToAssignmentEntity(scheduleId);
        await unitOfWork.ShiftAssignments.AddAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await MapAssignmentAsync(assignment, shift, cancellationToken);
        return ApiResponse<ShiftAssignmentResponse>.SuccessResponse(response, AppMessages.Schedule.AssignmentCreated);
    }

    public async Task<ApiResponse<object>> DeleteAssignmentAsync(
        Guid scheduleId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.NotDraft);

        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(assignmentId, cancellationToken: cancellationToken);
        if (assignment is null || assignment.ScheduleId != scheduleId)
            return ApiResponse<object>.FailureResponse(AppMessages.Schedule.AssignmentNotFound);

        unitOfWork.ShiftAssignments.Remove(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Schedule.AssignmentDeleted);
    }

    public async Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> GetMyScheduleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = fromDate.AddDays(28);
        var assignments = await unitOfWork.ShiftAssignments.ListByEmployeeInDateRangeAsync(
            employee.Id,
            fromDate,
            toDate,
            cancellationToken);

        var responses = new List<ShiftAssignmentResponse>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: cancellationToken);
            if (shift is null)
                continue;

            responses.Add(await MapAssignmentAsync(assignment, shift, cancellationToken));
        }

        return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.SuccessResponse(responses, AppMessages.Schedule.MyScheduleListed);
    }

    private async Task<IReadOnlyList<ShiftAssignmentResponse>> BuildAssignmentResponsesAsync(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var assignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var responses = new List<ShiftAssignmentResponse>(assignments.Count);

        foreach (var assignment in assignments)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: cancellationToken);
            if (shift is null)
                continue;

            responses.Add(await MapAssignmentAsync(assignment, shift, cancellationToken));
        }

        return responses;
    }

    private static Task<ShiftAssignmentResponse> MapAssignmentAsync(
        ShiftAssignmentEntity assignment,
        Wokki.Domain.Entities.ShiftDefinition shift,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ShiftAssignmentResponse(
            assignment.Id,
            assignment.ScheduleId,
            assignment.ShiftDefinitionId,
            shift.Name,
            shift.StartTime,
            shift.EndTime,
            assignment.EmployeeId,
            assignment.Date,
            assignment.Note,
            assignment.CreatedAt));

    private async Task NotifyEmployeeSafeAsync(
        Guid employeeId,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return;

        try
        {
            await notifications.SendAsync(employee.UserId, eventName, payload, cancellationToken);
        }
        catch
        {
            // Notifications must not roll back core workflow.
        }
    }
}
