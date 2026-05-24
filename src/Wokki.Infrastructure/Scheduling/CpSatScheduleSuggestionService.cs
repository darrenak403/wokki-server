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

        if (context.LocationSchedulingPolicy is null)
            return new ScheduleSuggestionGenerationResult([], "missing_location_rules", Provider: "cpsat");

        if (context.Employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees", Provider: "cpsat");

        if (context.Shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts", Provider: "cpsat");

        var solverPolicy = LocationSchedulingSolverPolicy.FromLocationPolicy(context.LocationSchedulingPolicy);

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
            model.Add(LinearExpr.Sum(dayVars) <= SchedulingSolverDefaults.MaxShiftsPerEmployeePerDaySafetyCap);
        }

        // Weekly cap per employee
        for (var e = 0; e < employees.Count; e++)
        {
            var weekVars = (from s in Enumerable.Range(0, shifts.Count)
                            from d in Enumerable.Range(0, days.Count)
                            select (ILiteral)x[e, s, d]).ToArray();

            model.Add(LinearExpr.Sum(weekVars) <= SchedulingSolverDefaults.MaxShiftsPerEmployeePerWeek);

            if (solverPolicy.MinShiftsPerWeekEnabled && solverPolicy.MinShiftsPerWeek > 0)
                model.Add(LinearExpr.Sum(weekVars) >= solverPolicy.MinShiftsPerWeek);
        }

        // Min staff per slot (if RequireFullCoverage)
        if (solverPolicy.RequireFullCoverage)
        {
            for (var s = 0; s < shifts.Count; s++)
            for (var d = 0; d < days.Count; d++)
            {
                var slotVars = Enumerable.Range(0, employees.Count).Select(e => (ILiteral)x[e, s, d]).ToArray();
                model.Add(LinearExpr.Sum(slotVars) >= solverPolicy.DefaultMinStaffPerShift);
            }
        }

        // No time overlap for same employee on same day
        for (var e = 0; e < employees.Count; e++)
        for (var d = 0; d < days.Count; d++)
        for (var s1 = 0; s1 < shifts.Count; s1++)
        for (var s2 = s1 + 1; s2 < shifts.Count; s2++)
        {
            if (TimeRangesOverlap(shifts[s1].StartTime, shifts[s1].EndTime,
                                  shifts[s2].StartTime, shifts[s2].EndTime))
                model.AddBoolOr([x[e, s1, d].Not(), x[e, s2, d].Not()]);
        }

        // Objective: maximize preference score
        var objVars = new List<ILiteral>();
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

        model.Maximize(LinearExpr.WeightedSum(objVars.ToArray(), objCoeffs.ToArray()));

        if (solverPolicy.MinRestMinutesBetweenShifts > 0)
            logger.LogDebug(
                "MinRestMinutesBetweenShifts={Minutes} is configured but not enforced by the CP-SAT model for schedule {ScheduleId}",
                solverPolicy.MinRestMinutesBetweenShifts, scheduleId);

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

    private static bool TimeRangesOverlap(TimeOnly s1, TimeOnly e1, TimeOnly s2, TimeOnly e2) =>
        s1 < e2 && e1 > s2;
}
