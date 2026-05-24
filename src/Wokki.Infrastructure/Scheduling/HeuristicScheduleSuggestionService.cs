using Wokki.Application.Common.Interfaces;
using Wokki.Application.Scheduling;
using Wokki.Domain.Entities;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Infrastructure.Scheduling;

public sealed class HeuristicScheduleSuggestionService(ScheduleSuggestionContextLoader contextLoader)
    : IScheduleSuggestionService
{
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var (context, reason) = await contextLoader.LoadAsync(scheduleId, cancellationToken);
        if (context is null)
            return new ScheduleSuggestionGenerationResult([], reason);

        if (context.LocationSchedulingPolicy is null)
            return new ScheduleSuggestionGenerationResult([], "missing_location_rules");

        if (context.Employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees");

        if (context.Shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts");

        var solverPolicy = LocationSchedulingSolverPolicy.FromLocationPolicy(context.LocationSchedulingPolicy);

        if (solverPolicy.RequireSubmittedPreferences && context.SubmittedPreferences.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "missing_preferences");

        var planned = new List<ShiftAssignmentEntity>(context.ExistingAssignments);
        var shiftMap = context.Shifts.ToDictionary(s => s.Id);

        var frequency = context.HistoricalAssignments
            .GroupBy(a => (a.ShiftDefinitionId, a.Date.DayOfWeek, a.EmployeeId))
            .ToDictionary(g => g.Key, g => g.Count());

        var weekEnd = context.Schedule.WeekStartDate.AddDays(6);
        var suggestions = new List<ScheduleSuggestionDto>();

        for (var date = context.Schedule.WeekStartDate; date <= weekEnd; date = date.AddDays(1))
        {
            foreach (var shift in context.Shifts)
            {
                var slotAssigned = 0;
                const int maxPerSlot = 1; // default: one employee per shift slot
                while (slotAssigned < maxPerSlot)
                {
                    var best = RankEmployees(context, shift, date, frequency, planned, shiftMap);
                    if (best is null || best.Value.Score <= 0)
                        break;

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

                    slotAssigned++;
                }
            }
        }

        return new ScheduleSuggestionGenerationResult(suggestions, null);
    }

    private static (Guid EmployeeId, int Score)? RankEmployees(
        ScheduleSuggestionContext context,
        ShiftDefinition shift,
        DateOnly date,
        Dictionary<(Guid ShiftDefinitionId, DayOfWeek DayOfWeek, Guid EmployeeId), int> frequency,
        List<ShiftAssignmentEntity> planned,
        Dictionary<Guid, ShiftDefinition> shiftMap)
    {
        (Guid EmployeeId, int Score)? best = null;
        var solverPolicy = LocationSchedulingSolverPolicy.FromLocationPolicy(context.LocationSchedulingPolicy);

        foreach (var employee in context.Employees)
        {
            if (solverPolicy.UnavailableIsHardBlock
                && SchedulingAssignmentRules.IsUnavailableByPreference(employee.Id, shift.Id, date, context))
                continue;

            if (solverPolicy.RequireRoleMatch && !RoleMatches(employee, shift))
                continue;

            if (!SchedulingAssignmentRules.MeetsWeeklyCap(employee.Id, planned, context))
                continue;

            if (!SchedulingAssignmentRules.MeetsDailyCap(employee.Id, date, planned, context))
                continue;

            if (!IsAvailable(employee.Id, date.DayOfWeek, shift.StartTime, shift.EndTime, context.Availabilities))
                continue;

            if (HasOverlapInPlan(planned, shiftMap, employee.Id, date, shift.StartTime, shift.EndTime))
                continue;

            var score = 10;
            score += SchedulingAssignmentRules.PreferenceScore(employee.Id, shift.Id, date, context);

            var freqKey = (shift.Id, date.DayOfWeek, employee.Id);
            if (frequency.TryGetValue(freqKey, out var count))
                score += Math.Min(count * 5, 25);

            var weeklyLoad = planned.Count(a => a.EmployeeId == employee.Id);
            score -= weeklyLoad * 2;

            if (solverPolicy.MinShiftsPerWeekEnabled
                && solverPolicy.MinShiftsPerWeek > 0
                && weeklyLoad < solverPolicy.MinShiftsPerWeek)
            {
                score += (solverPolicy.MinShiftsPerWeek - weeklyLoad)
                    * SchedulingSolverDefaults.MinShiftsPerWeekBoostPerMissingShift;
            }

            if (!string.IsNullOrWhiteSpace(employee.Position))
            {
                var roleLoad = planned.Count(a =>
                {
                    var emp = context.Employees.FirstOrDefault(e => e.Id == a.EmployeeId);
                    return emp is not null
                           && string.Equals(
                               emp.Position,
                               employee.Position,
                               StringComparison.OrdinalIgnoreCase);
                });
                score -= roleLoad * SchedulingSolverDefaults.RoleBalancePenaltyPerShift;
            }

            if (best is null || score > best.Value.Score
                || (score == best.Value.Score && weeklyLoad < planned.Count(a => a.EmployeeId == best.Value.EmployeeId)))
                best = (employee.Id, score);
        }

        return best;
    }

    private static bool RoleMatches(Employee employee, ShiftDefinition shift)
    {
        if (string.IsNullOrWhiteSpace(shift.RequiredRole))
            return true;

        var position = employee.Position?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(position))
            return false;

        return string.Equals(position, shift.RequiredRole.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAvailable(
        Guid employeeId,
        DayOfWeek dayOfWeek,
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        IReadOnlyList<EmployeeAvailability> availabilities)
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
        Dictionary<Guid, ShiftDefinition> shiftMap,
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
