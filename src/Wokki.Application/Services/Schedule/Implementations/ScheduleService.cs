using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Mappings.Schedules;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class ScheduleService(
    IUnitOfWork unitOfWork,
    INotificationService notifications,
    IScheduleSuggestionOrchestrator scheduleSuggestions) : IScheduleService
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

    public async Task<ApiResponse<IReadOnlyList<RosterAssignmentResponse>>> ListRosterAsync(
        ScheduleRosterRequest request,
        CancellationToken cancellationToken = default)
    {
        var weekEnd = request.WeekEndDate ?? request.WeekStartDate.AddDays(6);
        if (weekEnd < request.WeekStartDate || weekEnd.DayNumber - request.WeekStartDate.DayNumber > 28)
            return ApiResponse<IReadOnlyList<RosterAssignmentResponse>>.FailureResponse(AppMessages.Schedule.RosterRangeInvalid);

        var (locationId, locationError) = await ResolveRosterLocationIdAsync(request.DepartmentId, cancellationToken);
        if (locationError is not null)
            return ApiResponse<IReadOnlyList<RosterAssignmentResponse>>.FailureResponse(locationError);
        if (locationId is null)
            return ApiResponse<IReadOnlyList<RosterAssignmentResponse>>.FailureResponse(AppMessages.Schedule.LocationNotFound);

        var rows = await unitOfWork.ShiftAssignments.ListRosterAsync(
            locationId.Value,
            request.WeekStartDate,
            weekEnd,
            request.DepartmentId,
            request.EmployeeId,
            cancellationToken);

        var locationNames = (await unitOfWork.Locations.ListAsync(activeOnly: true, cancellationToken))
            .ToDictionary(l => l.Id, l => l.Name);

        var responses = rows.Select(row => new RosterAssignmentResponse(
            row.Assignment.Id,
            row.Assignment.ScheduleId,
            row.Schedule.Status,
            row.Schedule.WeekStartDate,
            row.Assignment.ShiftDefinitionId,
            row.ShiftDefinition.Name,
            row.ShiftDefinition.Color,
            row.ShiftDefinition.StartTime,
            row.ShiftDefinition.EndTime,
            row.Assignment.EmployeeId,
            row.Employee.FirstName,
            row.Employee.LastName,
            row.Assignment.Date,
            row.Department.Id,
            row.Department.Name,
            row.Department.LocationId,
            locationNames.GetValueOrDefault(row.Department.LocationId) ?? string.Empty,
            row.Assignment.Note,
            row.Assignment.CreatedAt)).ToList();

        return ApiResponse<IReadOnlyList<RosterAssignmentResponse>>.SuccessResponse(
            responses,
            AppMessages.Schedule.RosterListed);
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

        var prepared = await TryPrepareAssignmentAsync(schedule, scheduleId, request, cancellationToken);
        if (prepared.Error is not null)
            return ApiResponse<ShiftAssignmentResponse>.FailureResponse(prepared.Error);

        await unitOfWork.ShiftAssignments.AddAsync(prepared.Assignment!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await MapAssignmentAsync(prepared.Assignment!, prepared.Shift!, cancellationToken);
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

    public async Task<ApiResponse<ScheduleSuggestionsResponse>> SuggestAsync(
        Guid scheduleId,
        SuggestScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleSuggestionsResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        var generated = await scheduleSuggestions.GenerateAsync(scheduleId, request.UseAi, cancellationToken);
        if (generated.Reason is "schedule_not_draft")
            return ApiResponse<ScheduleSuggestionsResponse>.FailureResponse(AppMessages.Schedule.NotDraft);

        if (generated.Reason is "schedule_not_found")
            return ApiResponse<ScheduleSuggestionsResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (generated.Reason is "department_not_found")
            return ApiResponse<ScheduleSuggestionsResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var items = new List<ScheduleSuggestionItem>(generated.Suggestions.Count);
        foreach (var suggestion in generated.Suggestions)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
                suggestion.ShiftDefinitionId,
                cancellationToken: cancellationToken);
            var employee = await unitOfWork.Employees.GetByIdAsync(
                suggestion.EmployeeId,
                cancellationToken: cancellationToken);
            if (shift is null || employee is null)
                continue;

            items.Add(new ScheduleSuggestionItem(
                suggestion.Id,
                suggestion.ShiftDefinitionId,
                shift.Name,
                suggestion.EmployeeId,
                $"{employee.FirstName} {employee.LastName}".Trim(),
                suggestion.Date,
                suggestion.Score));
        }

        return ApiResponse<ScheduleSuggestionsResponse>.SuccessResponse(
            new ScheduleSuggestionsResponse(items, generated.Reason, generated.Provider, generated.FallbackUsed),
            AppMessages.Schedule.SuggestionsGenerated);
    }

    private async Task<(Guid? LocationId, AppMessage? Error)> ResolveRosterLocationIdAsync(
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId.HasValue)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(departmentId.Value, cancellationToken: cancellationToken);
            if (department is null)
                return (null, AppMessages.Schedule.DepartmentNotFound);

            return (department.LocationId, null);
        }

        var locations = await unitOfWork.Locations.ListAsync(activeOnly: true, cancellationToken);
        return locations.Count switch
        {
            0 => (null, AppMessages.Schedule.LocationNotFound),
            1 => (locations[0].Id, null),
            _ => (null, AppMessages.Schedule.LocationAmbiguous)
        };
    }

    public async Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> ApplySuggestionsAsync(
        Guid scheduleId,
        ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Suggestions.Count == 0)
            return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Schedule.SuggestionsEmpty);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Schedule.NotDraft);

        var preparedItems = new List<(ShiftAssignmentEntity Assignment, Wokki.Domain.Entities.ShiftDefinition Shift)>();
        foreach (var item in request.Suggestions)
        {
            var createRequest = new CreateShiftAssignmentRequest(
                item.ShiftDefinitionId,
                item.EmployeeId,
                item.Date,
                item.Note);

            var prepared = await TryPrepareAssignmentAsync(schedule, scheduleId, createRequest, cancellationToken);
            if (prepared.Error is not null)
                return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(prepared.Error);

            preparedItems.Add((prepared.Assignment!, prepared.Shift!));
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (assignment, _) in preparedItems)
                await unitOfWork.ShiftAssignments.AddAsync(assignment, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var created = new List<ShiftAssignmentResponse>(preparedItems.Count);
        foreach (var (assignment, shift) in preparedItems)
            created.Add(await MapAssignmentAsync(assignment, shift, cancellationToken));

        return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.SuccessResponse(
            created,
            AppMessages.Schedule.SuggestionsApplied);
    }

    private async Task<(ShiftAssignmentEntity? Assignment, Wokki.Domain.Entities.ShiftDefinition? Shift, AppMessage? Error)> TryPrepareAssignmentAsync(
        ScheduleEntity schedule,
        Guid scheduleId,
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (schedule.Status != ScheduleStatus.Draft)
            return (null, null, AppMessages.Schedule.NotDraft);

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(request.ShiftDefinitionId, cancellationToken: cancellationToken);
        if (shift is null || !shift.IsActive)
            return (null, null, AppMessages.Schedule.ShiftInactive);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return (null, null, AppMessages.Schedule.DepartmentNotFound);

        if (shift.LocationId != department.LocationId)
            return (null, null, AppMessages.Schedule.ShiftWrongScope);

        if (shift.DepartmentId is Guid shiftDept && shiftDept != schedule.DepartmentId)
            return (null, null, AppMessages.Schedule.ShiftWrongScope);

        var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken: cancellationToken);
        if (employee is null || employee.TerminatedAt is not null)
            return (null, null, AppMessages.Schedule.EmployeeNotFound);

        if (employee.DepartmentId != schedule.DepartmentId)
            return (null, null, AppMessages.Schedule.EmployeeWrongDepartment);

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        if (request.Date < schedule.WeekStartDate || request.Date > weekEnd)
            return (null, null, AppMessages.Validation.Failed);

        if (await unitOfWork.ShiftAssignments.ExistsAsync(
                scheduleId,
                request.ShiftDefinitionId,
                request.EmployeeId,
                request.Date,
                cancellationToken))
            return (null, null, AppMessages.Schedule.AssignmentDuplicate);

        if (await unitOfWork.ShiftAssignments.HasTimeOverlapAsync(
                scheduleId,
                request.EmployeeId,
                request.Date,
                shift.StartTime,
                shift.EndTime,
                cancellationToken: cancellationToken))
            return (null, null, AppMessages.Schedule.AssignmentConflict);

        return (request.ToAssignmentEntity(scheduleId), shift, null);
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

        var context = new AssignmentResponseContext();
        var responses = new List<ShiftAssignmentResponse>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: cancellationToken);
            if (shift is null)
                continue;

            responses.Add(await MapAssignmentAsync(assignment, shift, context, cancellationToken));
        }

        return ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.SuccessResponse(responses, AppMessages.Schedule.MyScheduleListed);
    }

    private async Task<IReadOnlyList<ShiftAssignmentResponse>> BuildAssignmentResponsesAsync(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var assignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var context = new AssignmentResponseContext();
        var responses = new List<ShiftAssignmentResponse>(assignments.Count);

        foreach (var assignment in assignments)
        {
            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: cancellationToken);
            if (shift is null)
                continue;

            responses.Add(await MapAssignmentAsync(assignment, shift, context, cancellationToken));
        }

        return responses;
    }

    private async Task<ShiftAssignmentResponse> MapAssignmentAsync(
        ShiftAssignmentEntity assignment,
        Wokki.Domain.Entities.ShiftDefinition shift,
        CancellationToken cancellationToken) =>
        await MapAssignmentAsync(assignment, shift, new AssignmentResponseContext(), cancellationToken);

    private async Task<ShiftAssignmentResponse> MapAssignmentAsync(
        ShiftAssignmentEntity assignment,
        Wokki.Domain.Entities.ShiftDefinition shift,
        AssignmentResponseContext context,
        CancellationToken cancellationToken)
    {
        var schedule = await GetOrLoadAsync(
            context.Schedules,
            assignment.ScheduleId,
            id => unitOfWork.Schedules.GetByIdAsync(id, cancellationToken: cancellationToken));

        DepartmentEntity? department = null;
        LocationEntity? location = null;

        if (schedule is not null)
        {
            department = await GetOrLoadAsync(
                context.Departments,
                schedule.DepartmentId,
                id => unitOfWork.Departments.GetByIdAsync(id, cancellationToken: cancellationToken));
        }

        if (department is not null)
        {
            location = await GetOrLoadAsync(
                context.Locations,
                department.LocationId,
                id => unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken));
        }

        return new ShiftAssignmentResponse(
            assignment.Id,
            assignment.ScheduleId,
            assignment.ShiftDefinitionId,
            shift.Name,
            shift.Color,
            shift.StartTime,
            shift.EndTime,
            assignment.EmployeeId,
            assignment.Date,
            department?.Id,
            department?.Name,
            location?.Id,
            location?.Name,
            assignment.Note,
            assignment.CreatedAt);
    }

    private static async Task<T?> GetOrLoadAsync<T>(
        Dictionary<Guid, T?> cache,
        Guid id,
        Func<Guid, Task<T?>> loadAsync)
        where T : class
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;

        var value = await loadAsync(id);
        cache[id] = value;
        return value;
    }

    private sealed class AssignmentResponseContext
    {
        public Dictionary<Guid, ScheduleEntity?> Schedules { get; } = new();
        public Dictionary<Guid, DepartmentEntity?> Departments { get; } = new();
        public Dictionary<Guid, LocationEntity?> Locations { get; } = new();
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
