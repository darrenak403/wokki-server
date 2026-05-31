using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Scheduling;

public sealed class ScheduleSuggestionContextLoader(IUnitOfWork unitOfWork)
{
    private const int HistoryWeeks = 4;

    public async Task<(ScheduleSuggestionContext? Context, string? Reason)> LoadAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return (null, "schedule_not_found");

        if (schedule.Status != ScheduleStatus.Draft)
            return (null, "schedule_not_draft");

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return (null, "department_not_found");

        var historyFrom = schedule.WeekStartDate.AddDays(-7 * HistoryWeeks);
        var historyTo = schedule.WeekStartDate.AddDays(-1);
        var historical = await unitOfWork.ShiftAssignments.ListPublishedByDepartmentInDateRangeAsync(
            schedule.DepartmentId,
            historyFrom,
            historyTo,
            cancellationToken);

        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            500,
            department.OrganizationId,
            schedule.DepartmentId,
            locationIds: new HashSet<Guid> { department.LocationId },
            cancellationToken: cancellationToken);
        var employees = employeePage.Items.Where(e => e.TerminatedAt is null).ToList();

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);

        var existing = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var availabilities = await unitOfWork.EmployeeAvailabilities.ListByEmployeeIdsAsync(
            employees.Select(e => e.Id),
            cancellationToken);

        var submissions = await unitOfWork.SchedulePreferences.ListByScheduleAsync(
            scheduleId,
            includeLines: true,
            status: SchedulePreferenceStatus.Submitted,
            cancellationToken);
        var submittedPreferences = submissions
            .SelectMany(s => s.Lines.Select(l => new SubmittedPreferenceChoice(
                s.EmployeeId,
                l.ShiftDefinitionId,
                l.Date,
                l.PreferenceType)))
            .ToList();

        var orgPolicy = await unitOfWork.OrganizationSchedulingPolicies.GetByOrganizationIdAsync(
            department.OrganizationId,
            cancellationToken: cancellationToken);

        return (new ScheduleSuggestionContext
        {
            Schedule = schedule,
            Department = department,
            Employees = employees,
            Shifts = shifts,
            ExistingAssignments = existing,
            HistoricalAssignments = historical,
            Availabilities = availabilities,
            SubmittedPreferences = submittedPreferences,
            PreferenceSubmissions = submissions,
            OrganizationSchedulingPolicy = orgPolicy
        }, null);
    }
}
