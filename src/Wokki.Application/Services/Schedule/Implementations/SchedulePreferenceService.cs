using Wokki.Application.Common;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class SchedulePreferenceService(IUnitOfWork unitOfWork) : ISchedulePreferenceService
{
    public async Task<ApiResponse<EmployeeDraftScheduleResponse?>> GetDraftScheduleForEmployeeAsync(
        Guid userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        if (!ScheduleRules.IsMonday(weekStartDate))
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.WeekNotMonday);

        var schedule = await unitOfWork.Schedules.GetByDepartmentAndWeekAsync(
            employee.DepartmentId,
            weekStartDate,
            cancellationToken);

        if (schedule is null || schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<EmployeeDraftScheduleResponse?>.SuccessResponse(null, AppMessages.SchedulePreference.Found);

        var department = await unitOfWork.Departments.GetByIdAsync(
            schedule.DepartmentId,
            cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);

        return ApiResponse<EmployeeDraftScheduleResponse?>.SuccessResponse(
            new EmployeeDraftScheduleResponse(
                schedule.Id,
                schedule.WeekStartDate,
                schedule.Status,
                shifts
                    .OrderBy(s => s.StartTime)
                    .Select(s => new SchedulePreferenceBoardShiftColumn(
                        s.Id,
                        s.Name,
                        s.StartTime,
                        s.EndTime))
                    .ToList()),
            AppMessages.SchedulePreference.Found);
    }

    public async Task<ApiResponse<EmployeeDraftScheduleResponse?>> GetScheduleForEmployeePreferencesAsync(
        Guid userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        if (!ScheduleRules.IsMonday(weekStartDate))
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.WeekNotMonday);

        var schedule = await unitOfWork.Schedules.GetByDepartmentAndWeekAsync(
            employee.DepartmentId,
            weekStartDate,
            cancellationToken);

        if (schedule is null)
            return ApiResponse<EmployeeDraftScheduleResponse?>.SuccessResponse(null, AppMessages.SchedulePreference.Found);

        var department = await unitOfWork.Departments.GetByIdAsync(
            schedule.DepartmentId,
            cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);

        return ApiResponse<EmployeeDraftScheduleResponse?>.SuccessResponse(
            new EmployeeDraftScheduleResponse(
                schedule.Id,
                schedule.WeekStartDate,
                schedule.Status,
                shifts
                    .OrderBy(s => s.StartTime)
                    .Select(s => new SchedulePreferenceBoardShiftColumn(
                        s.Id,
                        s.Name,
                        s.StartTime,
                        s.EndTime))
                    .ToList()),
            AppMessages.SchedulePreference.Found);
    }

    public async Task<ApiResponse<MySchedulePreferenceResponse>> GetMineAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.DepartmentId != employee.DepartmentId)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.WrongDepartment);

        var submission = await unitOfWork.SchedulePreferences.GetByScheduleAndEmployeeAsync(
            scheduleId,
            employee.Id,
            includeLines: true,
            cancellationToken);

        return ApiResponse<MySchedulePreferenceResponse>.SuccessResponse(
            MapMine(scheduleId, submission),
            AppMessages.SchedulePreference.Found);
    }

    public async Task<ApiResponse<MySchedulePreferenceResponse>> SaveMineAsync(
        Guid userId,
        Guid scheduleId,
        SaveSchedulePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NotDraft);

        if (schedule.DepartmentId != employee.DepartmentId)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.WrongDepartment);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);
        var shiftIds = shifts.Select(s => s.Id).ToHashSet();

        var submission = await unitOfWork.SchedulePreferences.GetByScheduleAndEmployeeAsync(
            scheduleId,
            employee.Id,
            includeLines: true,
            cancellationToken);

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        var lines = new List<SchedulePreferenceLine>();
        foreach (var input in request.Lines)
        {
            if (!shiftIds.Contains(input.ShiftDefinitionId))
                return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.InvalidShift);

            if (input.Date < schedule.WeekStartDate || input.Date > weekEnd)
                return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.DateOutOfRange);

            if (!Enum.TryParse<PreferenceType>(input.PreferenceType, true, out var preferenceType))
                return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.InvalidPreferenceType);

            lines.Add(new SchedulePreferenceLine
            {
                Id = Guid.NewGuid(),
                ShiftDefinitionId = input.ShiftDefinitionId,
                Date = input.Date,
                PreferenceType = preferenceType
            });
        }
        lines = lines
            .GroupBy(l => (l.ShiftDefinitionId, l.Date))
            .Select(g => g.Last())
            .ToList();

        if (submission is null)
        {
            var submissionId = Guid.NewGuid();
            foreach (var line in lines)
                line.SubmissionId = submissionId;

            submission = new SchedulePreferenceSubmission
            {
                Id = submissionId,
                ScheduleId = scheduleId,
                EmployeeId = employee.Id,
                Status = SchedulePreferenceStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                Lines = lines
            };
            await unitOfWork.SchedulePreferences.AddAsync(submission, cancellationToken);
        }
        else
        {
            unitOfWork.SchedulePreferences.RemoveLines(submission.Lines.ToList());
            foreach (var line in lines)
                line.SubmissionId = submission.Id;
            if (lines.Count > 0)
                await unitOfWork.SchedulePreferences.AddLinesAsync(lines, cancellationToken);

            submission.Status = SchedulePreferenceStatus.Draft;
            submission.UpdatedAt = DateTime.UtcNow;
            submission.SubmittedAt = null;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.SchedulePreferences.GetByScheduleAndEmployeeAsync(
            scheduleId,
            employee.Id,
            includeLines: true,
            cancellationToken);

        return ApiResponse<MySchedulePreferenceResponse>.SuccessResponse(
            MapMine(scheduleId, saved ?? submission),
            AppMessages.SchedulePreference.Saved);
    }

    public async Task<ApiResponse<MySchedulePreferenceResponse>> SubmitMineAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NoEmployeeProfile);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        if (schedule.Status != ScheduleStatus.Draft)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Schedule.NotDraft);

        if (schedule.DepartmentId != employee.DepartmentId)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.WrongDepartment);

        var submission = await unitOfWork.SchedulePreferences.GetByScheduleAndEmployeeAsync(
            scheduleId,
            employee.Id,
            includeLines: true,
            cancellationToken);

        if (submission is null || submission.Lines.Count == 0)
            return ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.SchedulePreference.Empty);

        // Re-submit: SaveMineAsync sets Draft before POST submit; idempotent if already Submitted.
        submission.Status = SchedulePreferenceStatus.Submitted;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<MySchedulePreferenceResponse>.SuccessResponse(
            MapMine(scheduleId, submission),
            AppMessages.SchedulePreference.Submitted);
    }

    public async Task<ApiResponse<SchedulePreferenceBoardResponse>> GetBoardAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<SchedulePreferenceBoardResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<SchedulePreferenceBoardResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            500,
            schedule.DepartmentId,
            locationIds: new HashSet<Guid> { department.LocationId },
            cancellationToken: cancellationToken);
        var employees = employeePage.Items.Where(e => e.TerminatedAt is null).ToList();

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);

        var submissions = await unitOfWork.SchedulePreferences.ListByScheduleAsync(
            scheduleId,
            includeLines: true,
            cancellationToken: cancellationToken);
        var submissionByEmployee = submissions.ToDictionary(s => s.EmployeeId);

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        var weekDates = Enumerable.Range(0, 7).Select(i => schedule.WeekStartDate.AddDays(i)).ToList();

        var rows = employees.Select(emp =>
        {
            submissionByEmployee.TryGetValue(emp.Id, out var submission);
            var lineLookup = submission?.Lines.ToDictionary(l => (l.ShiftDefinitionId, l.Date)) ?? [];

            var cells = new List<SchedulePreferenceCellResponse>();
            foreach (var shift in shifts)
            {
                foreach (var date in weekDates)
                {
                    lineLookup.TryGetValue((shift.Id, date), out var line);
                    cells.Add(new SchedulePreferenceCellResponse(
                        shift.Id,
                        date,
                        line?.PreferenceType.ToString()));
                }
            }

            return new SchedulePreferenceBoardEmployeeRow(
                emp.Id,
                $"{emp.FirstName} {emp.LastName}".Trim(),
                emp.Position,
                submission?.Status.ToString(),
                cells);
        }).ToList();

        var submittedCount = await unitOfWork.SchedulePreferences.CountSubmittedByScheduleAsync(scheduleId, cancellationToken);

        return ApiResponse<SchedulePreferenceBoardResponse>.SuccessResponse(
            new SchedulePreferenceBoardResponse(
                scheduleId,
                schedule.WeekStartDate,
                employees.Count,
                submittedCount,
                rows,
                shifts
                    .OrderBy(s => s.StartTime)
                    .Select(s => new SchedulePreferenceBoardShiftColumn(
                        s.Id,
                        s.Name,
                        s.StartTime,
                        s.EndTime))
                    .ToList()),
            AppMessages.SchedulePreference.BoardListed);
    }

    private static MySchedulePreferenceResponse MapMine(Guid scheduleId, SchedulePreferenceSubmission? submission) =>
        new(
            scheduleId,
            submission?.Id,
            submission?.Status.ToString() ?? SchedulePreferenceStatus.Draft.ToString(),
            submission?.Lines.Select(l => new SchedulePreferenceLineResponse(
                l.ShiftDefinitionId,
                l.Date,
                l.PreferenceType.ToString())).ToList() ?? []);
}
