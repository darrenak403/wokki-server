using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class ScheduleRebalanceAnalyzer(IUnitOfWork unitOfWork) : IScheduleRebalanceAnalyzer
{
    public async Task<ScheduleRebalanceHintsResponse> AnalyzeAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null || schedule.Status != ScheduleStatus.Draft)
            return ScheduleRebalanceHintsResponse.Empty;

        var pendingLeaveCount = await unitOfWork.ScheduleLeaveRequests.CountPendingByScheduleAsync(
            scheduleId,
            cancellationToken);

        var assignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var submissions = await unitOfWork.SchedulePreferences.ListByScheduleAsync(
            scheduleId,
            includeLines: true,
            status: SchedulePreferenceStatus.Submitted,
            cancellationToken);

        if (assignments.Count == 0)
        {
            return pendingLeaveCount > 0
                ? new ScheduleRebalanceHintsResponse(false, 0, pendingLeaveCount, [])
                : ScheduleRebalanceHintsResponse.Empty;
        }

        var unavailableKeys = new HashSet<(Guid EmployeeId, Guid ShiftDefinitionId, DateOnly Date)>();
        foreach (var submission in submissions)
        {
            foreach (var line in submission.Lines.Where(l => l.PreferenceType == PreferenceType.Unavailable))
                unavailableKeys.Add((submission.EmployeeId, line.ShiftDefinitionId, line.Date));
        }

        var conflictAssignments = assignments
            .Where(a => unavailableKeys.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date)))
            .ToList();

        var preferenceBaseline = ScheduleRebalanceBaseline.GetPreferenceChangeBaseline(schedule, assignments);
        var hasRecentPreferenceChanges = ScheduleRebalanceBaseline.HasPreferenceChangesAfterBaseline(
            submissions,
            preferenceBaseline);

        if (conflictAssignments.Count == 0 && !hasRecentPreferenceChanges && pendingLeaveCount == 0)
            return ScheduleRebalanceHintsResponse.Empty;

        var employeeIds = conflictAssignments.Select(a => a.EmployeeId).Distinct().ToList();
        var shiftIds = conflictAssignments.Select(a => a.ShiftDefinitionId).Distinct().ToList();
        var employees = employeeIds.Count == 0
            ? []
            : (await unitOfWork.Employees.GetByIdsAsync(employeeIds, cancellationToken)).ToDictionary(e => e.Id);
        var shifts = shiftIds.Count == 0
            ? []
            : (await unitOfWork.ShiftDefinitions.GetByIdsAsync(shiftIds, cancellationToken)).ToDictionary(s => s.Id);

        var conflicts = conflictAssignments
            .Select(a =>
            {
                employees.TryGetValue(a.EmployeeId, out var employee);
                shifts.TryGetValue(a.ShiftDefinitionId, out var shift);
                var employeeName = employee is null
                    ? "Unknown"
                    : $"{employee.FirstName} {employee.LastName}".Trim();
                return new ScheduleRebalanceConflictResponse(
                    a.Id,
                    a.EmployeeId,
                    employeeName,
                    a.ShiftDefinitionId,
                    shift?.Name ?? "Unknown",
                    a.Date.ToString("yyyy-MM-dd"));
            })
            .ToList();

        return new ScheduleRebalanceHintsResponse(
            hasRecentPreferenceChanges,
            conflicts.Count,
            pendingLeaveCount,
            conflicts);
    }
}
