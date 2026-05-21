using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Infrastructure.Scheduling;

public sealed class HeuristicScheduleSuggestionService(IUnitOfWork unitOfWork) : IScheduleSuggestionService
{
    private const int HistoryWeeks = 4;
    private const int MinHistoryAssignments = 3;

    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return new ScheduleSuggestionGenerationResult([], "schedule_not_found");

        if (schedule.Status != ScheduleStatus.Draft)
            return new ScheduleSuggestionGenerationResult([], "schedule_not_draft");

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return new ScheduleSuggestionGenerationResult([], "department_not_found");

        var historyFrom = schedule.WeekStartDate.AddDays(-7 * HistoryWeeks);
        var historyTo = schedule.WeekStartDate.AddDays(-1);
        var historical = await unitOfWork.ShiftAssignments.ListPublishedByDepartmentInDateRangeAsync(
            schedule.DepartmentId,
            historyFrom,
            historyTo,
            cancellationToken);

        if (historical.Count < MinHistoryAssignments)
            return new ScheduleSuggestionGenerationResult([], "insufficient_history");

        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            500,
            schedule.DepartmentId,
            cancellationToken: cancellationToken);
        var employees = employeePage.Items.Where(e => e.TerminatedAt is null).ToList();
        if (employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees");

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);
        if (shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts");

        var availabilities = await unitOfWork.EmployeeAvailabilities.ListByEmployeeIdsAsync(
            employees.Select(e => e.Id),
            cancellationToken);

        var existing = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var planned = new List<ShiftAssignmentEntity>(existing);
        var shiftMap = shifts.ToDictionary(s => s.Id);

        var frequency = historical
            .GroupBy(a => (a.ShiftDefinitionId, a.Date.DayOfWeek, a.EmployeeId))
            .ToDictionary(g => g.Key, g => g.Count());

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        var suggestions = new List<ScheduleSuggestionDto>();

        for (var date = schedule.WeekStartDate; date <= weekEnd; date = date.AddDays(1))
        {
            foreach (var shift in shifts)
            {
                if (existing.Any(a => a.ShiftDefinitionId == shift.Id && a.Date == date))
                    continue;

                var best = RankEmployees(
                    employees,
                    shift,
                    date,
                    frequency,
                    availabilities,
                    planned,
                    shiftMap);

                if (best is null || best.Value.Score <= 0)
                    continue;

                var suggestion = new ScheduleSuggestionDto(
                    Guid.NewGuid(),
                    shift.Id,
                    best.Value.EmployeeId,
                    date,
                    best.Value.Score);

                suggestions.Add(suggestion);

                planned.Add(new ShiftAssignmentEntity
                {
                    Id = suggestion.Id,
                    ScheduleId = scheduleId,
                    ShiftDefinitionId = shift.Id,
                    EmployeeId = best.Value.EmployeeId,
                    Date = date,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return new ScheduleSuggestionGenerationResult(suggestions, null);
    }

    private static (Guid EmployeeId, int Score)? RankEmployees(
        IReadOnlyList<Wokki.Domain.Entities.Employee> employees,
        Wokki.Domain.Entities.ShiftDefinition shift,
        DateOnly date,
        Dictionary<(Guid ShiftDefinitionId, DayOfWeek DayOfWeek, Guid EmployeeId), int> frequency,
        IReadOnlyList<Wokki.Domain.Entities.EmployeeAvailability> availabilities,
        List<ShiftAssignmentEntity> planned,
        Dictionary<Guid, Wokki.Domain.Entities.ShiftDefinition> shiftMap)
    {
        (Guid EmployeeId, int Score)? best = null;

        foreach (var employee in employees)
        {
            var score = 0;

            if (!RoleMatches(employee.Position, shift.RequiredRole))
                continue;

            score += 10;

            var freqKey = (shift.Id, date.DayOfWeek, employee.Id);
            if (frequency.TryGetValue(freqKey, out var count))
                score += Math.Min(count * 5, 25);

            if (!IsAvailable(employee.Id, date.DayOfWeek, shift.StartTime, shift.EndTime, availabilities))
                continue;

            if (HasOverlapInPlan(planned, shiftMap, employee.Id, date, shift.StartTime, shift.EndTime))
                continue;

            if (best is null || score > best.Value.Score)
                best = (employee.Id, score);
        }

        return best;
    }

    private static bool RoleMatches(string position, string requiredRole) =>
        !string.IsNullOrWhiteSpace(requiredRole)
        && string.Equals(position.Trim(), requiredRole.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool IsAvailable(
        Guid employeeId,
        DayOfWeek dayOfWeek,
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        IReadOnlyList<Wokki.Domain.Entities.EmployeeAvailability> availabilities)
    {
        var rows = availabilities.Where(a => a.EmployeeId == employeeId).ToList();
        if (rows.Count == 0)
            return true;

        return rows.Any(a =>
            a.IsAvailable
            && a.DayOfWeek == dayOfWeek
            && a.StartTime <= shiftStart
            && a.EndTime >= shiftEnd);
    }

    private static bool HasOverlapInPlan(
        List<ShiftAssignmentEntity> planned,
        Dictionary<Guid, Wokki.Domain.Entities.ShiftDefinition> shiftMap,
        Guid employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        foreach (var assignment in planned.Where(a => a.EmployeeId == employeeId && a.Date == date))
        {
            if (!shiftMap.TryGetValue(assignment.ShiftDefinitionId, out var existing))
                continue;

            if (TimeRangesOverlap(startTime, endTime, existing.StartTime, existing.EndTime))
                return true;
        }

        return false;
    }

    private static bool TimeRangesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2) =>
        start1 < end2 && end1 > start2;
}
