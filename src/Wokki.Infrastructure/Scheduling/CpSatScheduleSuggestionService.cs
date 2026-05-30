using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Scheduling;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Scheduling;

public sealed class CpSatScheduleSuggestionService(
    ScheduleSuggestionContextLoader contextLoader,
    ILogger<CpSatScheduleSuggestionService> logger) : IScheduleSuggestionService
{
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var (context, reason) = await contextLoader.LoadAsync(scheduleId, cancellationToken);
        if (context is null)
            return new ScheduleSuggestionGenerationResult([], reason, Provider: "cpsat");

        if (context.Employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees", Provider: "cpsat");

        if (context.Shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts", Provider: "cpsat");

        var solverPolicy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(context.OrganizationSchedulingPolicy);

        if (solverPolicy.RequireSubmittedPreferences && context.SubmittedPreferences.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "missing_preferences", Provider: "cpsat");

        var employees = context.Employees.ToList();
        var shifts = context.Shifts.ToList();
        var days = Enumerable.Range(0, 7)
            .Select(i => context.Schedule.WeekStartDate.AddDays(i))
            .ToList();

        var existingSet = context.ExistingAssignments
            .Select(a => (a.EmployeeId, a.ShiftDefinitionId, a.Date))
            .ToHashSet();
        var shiftsById = shifts.ToDictionary(s => s.Id);
        var existingByEmployee = context.ExistingAssignments
            .Where(a => shiftsById.ContainsKey(a.ShiftDefinitionId))
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var minStaffPerSlot = Math.Max(0, solverPolicy.DefaultMinStaffPerShift);
        var minRestMinutes = Math.Max(0, solverPolicy.MinRestMinutesBetweenShifts);
        var slots = (from s in Enumerable.Range(0, shifts.Count)
                     from d in Enumerable.Range(0, days.Count)
                     select new ScheduleSlot(s, d, BuildSlot(days[d], shifts[s])))
            .ToList();

        var model = new CpModel();
        var x = new BoolVar[employees.Count, shifts.Count, days.Count];

        for (var e = 0; e < employees.Count; e++)
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
            x[e, s, d] = model.NewBoolVar($"x_{e}_{s}_{d}");

        // Hard-fix forbidden assignments to 0
        for (var e = 0; e < employees.Count; e++)
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            var emp = employees[e];
            var shift = shifts[s];
            var date = days[d];

            if (existingSet.Contains((emp.Id, shift.Id, date)))
            {
                model.Add(x[e, s, d] == 0);
                continue;
            }

            if (ConflictsWithExistingAssignments(
                    emp.Id,
                    date,
                    shift,
                    existingByEmployee,
                    shiftsById,
                    minRestMinutes))
            {
                model.Add(x[e, s, d] == 0);
                continue;
            }

            if (solverPolicy.UnavailableIsHardBlock
                && SchedulingAssignmentRules.IsUnavailableByPreference(emp.Id, shift.Id, date, context))
            {
                model.Add(x[e, s, d] == 0);
                continue;
            }

            if (solverPolicy.RequireRoleMatch && !RoleMatches(emp, shift))
            {
                model.Add(x[e, s, d] == 0);
                continue;
            }

            if (!IsAvailableForShift(emp.Id, date.DayOfWeek, shift.StartTime, shift.EndTime, context.Availabilities))
            {
                model.Add(x[e, s, d] == 0);
            }
        }

        // Daily cap per employee
        for (var e = 0; e < employees.Count; e++)
        for (var d = 0; d < days.Count; d++)
        {
            var dayVars = Enumerable.Range(0, shifts.Count).Select(s => (ILiteral)x[e, s, d]).ToArray();
            var existingCount = context.ExistingAssignments.Count(a =>
                a.EmployeeId == employees[e].Id && a.Date == days[d]);
            var remaining = Math.Max(
                0,
                SchedulingSolverDefaults.MaxShiftsPerEmployeePerDaySafetyCap - existingCount);
            model.Add(LinearExpr.Sum(dayVars) <= remaining);
        }

        // Weekly cap per employee
        for (var e = 0; e < employees.Count; e++)
        {
            var weekVars = (from s in Enumerable.Range(0, shifts.Count)
                            from d in Enumerable.Range(0, days.Count)
                            select (ILiteral)x[e, s, d]).ToArray();

            var existingCount = context.ExistingAssignments.Count(a => a.EmployeeId == employees[e].Id);
            var remaining = Math.Max(
                0,
                SchedulingSolverDefaults.MaxShiftsPerEmployeePerWeek - existingCount);
            model.Add(LinearExpr.Sum(weekVars) <= remaining);
        }

        // Cap each slot at the remaining staffing need. If under-staffing is not allowed,
        // the remaining need is also a hard lower bound.
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            var slotVars = Enumerable.Range(0, employees.Count).Select(e => (ILiteral)x[e, s, d]).ToArray();
            var existingCount = context.ExistingAssignments.Count(a =>
                a.ShiftDefinitionId == shifts[s].Id && a.Date == days[d]);
            var remainingNeed = Math.Max(0, minStaffPerSlot - existingCount);

            model.Add(LinearExpr.Sum(slotVars) <= remainingNeed);
            if (solverPolicy.RequireFullCoverage && !solverPolicy.AllowUnderstaffedSuggestions)
            {
                model.Add(LinearExpr.Sum(slotVars) >= remainingNeed);
            }
        }

        // No overlap and enforce configured rest between any two slots in the week.
        for (var e = 0; e < employees.Count; e++)
        for (var i = 0; i < slots.Count; i++)
        for (var j = i + 1; j < slots.Count; j++)
        {
            if (SlotsConflict(slots[i].Assignment, slots[j].Assignment, minRestMinutes))
            {
                model.AddBoolOr([
                    x[e, slots[i].ShiftIndex, slots[i].DayIndex].Not(),
                    x[e, slots[j].ShiftIndex, slots[j].DayIndex].Not()
                ]);
            }
        }

        // Objective: maximize preference score
        var objVars = new List<LinearExpr>();
        var objCoeffs = new List<long>();

        for (var e = 0; e < employees.Count; e++)
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            var score = 10 + SchedulingAssignmentRules.PreferenceScore(
                employees[e].Id, shifts[s].Id, days[d], context);
            objVars.Add(x[e, s, d]);
            objCoeffs.Add(score);
        }

        if (solverPolicy.MinShiftsPerWeekEnabled && solverPolicy.MinShiftsPerWeek > 0)
        {
            for (var e = 0; e < employees.Count; e++)
            {
                var weekVars = (from s in Enumerable.Range(0, shifts.Count)
                                from d in Enumerable.Range(0, days.Count)
                                select (ILiteral)x[e, s, d]).ToArray();
                var total = LinearExpr.Sum(weekVars);
                var missing = model.NewIntVar(0, solverPolicy.MinShiftsPerWeek, $"missing_min_week_{e}");
                model.Add(missing >= solverPolicy.MinShiftsPerWeek - total);
                objVars.Add(missing);
                objCoeffs.Add(-SchedulingSolverDefaults.MinShiftsPerWeekBoostPerMissingShift);
            }
        }

        model.Maximize(LinearExpr.WeightedSum(objVars.ToArray(), objCoeffs.ToArray()));

        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:10 num_search_workers:4";
        var status = await Task.Run(() => solver.Solve(model), cancellationToken);

        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            logger.LogWarning("CP-SAT returned {Status} for schedule {ScheduleId}", status, scheduleId);
            return new ScheduleSuggestionGenerationResult([], "infeasible", Provider: "cpsat");
        }

        var results = new List<ScheduleSuggestionDto>();
        for (var e = 0; e < employees.Count; e++)
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            if (!solver.BooleanValue(x[e, s, d])) continue;
            var prefScore = SchedulingAssignmentRules.PreferenceScore(
                employees[e].Id, shifts[s].Id, days[d], context);
            results.Add(new ScheduleSuggestionDto(
                Guid.NewGuid(), shifts[s].Id, employees[e].Id, days[d], Score: 10 + prefScore));
        }

        return new ScheduleSuggestionGenerationResult(results, null, Provider: "cpsat");
    }

    private static bool RoleMatches(Employee employee, ShiftDefinition shift)
    {
        if (string.IsNullOrWhiteSpace(shift.RequiredRole))
            return true;

        // Auth roles (User/Admin/Manager) stored in RequiredRole carry no job-position restriction.
        var required = shift.RequiredRole.Trim();
        if (string.Equals(required, RoleConstants.User, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            return true;

        // Only compare against employee job position when RequiredRole is an actual position value.
        var position = employee.Position?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(position))
            return false;

        return string.Equals(position, required, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAvailableForShift(
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

    private static bool ConflictsWithExistingAssignments(
        Guid employeeId,
        DateOnly date,
        ShiftDefinition shift,
        IReadOnlyDictionary<Guid, List<ShiftAssignment>> existingByEmployee,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        int minRestMinutes)
    {
        if (!existingByEmployee.TryGetValue(employeeId, out var assignments))
            return false;

        var candidate = BuildSlot(date, shift);
        foreach (var assignment in assignments)
        {
            if (!shiftsById.TryGetValue(assignment.ShiftDefinitionId, out var existingShift))
                continue;

            if (SlotsConflict(BuildSlot(assignment.Date, existingShift), candidate, minRestMinutes))
                return true;
        }

        return false;
    }

    private static AssignmentTimeSlot BuildSlot(DateOnly date, ShiftDefinition shift)
    {
        var start = date.ToDateTime(shift.StartTime);
        var end = date.ToDateTime(shift.EndTime);
        if (end <= start)
            end = end.AddDays(1);

        return new AssignmentTimeSlot(start, end);
    }

    private static bool SlotsConflict(
        AssignmentTimeSlot left,
        AssignmentTimeSlot right,
        int minRestMinutes)
    {
        var first = left.Start <= right.Start ? left : right;
        var second = left.Start <= right.Start ? right : left;
        return (second.Start - first.End).TotalMinutes < minRestMinutes;
    }

    private sealed record ScheduleSlot(int ShiftIndex, int DayIndex, AssignmentTimeSlot Assignment);

    private sealed record AssignmentTimeSlot(DateTime Start, DateTime End);
}
