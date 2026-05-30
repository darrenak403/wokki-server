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

        if (!solverPolicy.HasAnyEnabledRule)
        {
            logger.LogInformation(
                "Schedule {ScheduleId}: no org scheduling rules enabled — pure preference mode",
                scheduleId);
        }

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

        var useStrictCoverage = solverPolicy.RequireFullCoverage && solverPolicy.MinStaffPerShiftEnabled;

        var strictResult = await SolveAsync(
            scheduleId,
            context,
            solverPolicy,
            employees,
            shifts,
            days,
            existingSet,
            existingByEmployee,
            shiftsById,
            enforceFullCoverageLowerBound: useStrictCoverage,
            cancellationToken);

        if (strictResult is not null)
            return strictResult;

        if (!useStrictCoverage)
            return new ScheduleSuggestionGenerationResult([], "infeasible", Provider: "cpsat");

        logger.LogInformation(
            "CP-SAT strict coverage infeasible for schedule {ScheduleId}; retrying with partial coverage",
            scheduleId);

        var relaxedResult = await SolveAsync(
            scheduleId,
            context,
            solverPolicy,
            employees,
            shifts,
            days,
            existingSet,
            existingByEmployee,
            shiftsById,
            enforceFullCoverageLowerBound: false,
            cancellationToken);

        if (relaxedResult is not null)
        {
            return relaxedResult with
            {
                Reason = "partial_coverage",
                FallbackUsed = true
            };
        }

        return new ScheduleSuggestionGenerationResult([], "infeasible", Provider: "cpsat");
    }

    private async Task<ScheduleSuggestionGenerationResult?> SolveAsync(
        Guid scheduleId,
        ScheduleSuggestionContext context,
        OrganizationSchedulingSolverPolicy solverPolicy,
        IReadOnlyList<Employee> employees,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<DateOnly> days,
        HashSet<(Guid EmployeeId, Guid ShiftDefinitionId, DateOnly Date)> existingSet,
        IReadOnlyDictionary<Guid, List<ShiftAssignment>> existingByEmployee,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        bool enforceFullCoverageLowerBound,
        CancellationToken cancellationToken)
    {
        var minRestMinutes = solverPolicy.MinRestMinutesEnabled
            ? solverPolicy.MinRestMinutesBetweenShifts
            : 0;

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

            if (solverPolicy.MinRestMinutesEnabled
                && ConflictsWithExistingAssignments(
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

        if (solverPolicy.MaxShiftsPerDayEnabled)
        {
            for (var e = 0; e < employees.Count; e++)
            for (var d = 0; d < days.Count; d++)
            {
                var dayVars = Enumerable.Range(0, shifts.Count).Select(s => (ILiteral)x[e, s, d]).ToArray();
                var existingCount = context.ExistingAssignments.Count(a =>
                    a.EmployeeId == employees[e].Id && a.Date == days[d]);
                var remaining = Math.Max(0, solverPolicy.MaxShiftsPerEmployeePerDay - existingCount);
                model.Add(LinearExpr.Sum(dayVars) <= remaining);
            }
        }

        if (solverPolicy.MaxShiftsPerWeekEnabled)
        {
            for (var e = 0; e < employees.Count; e++)
            {
                var weekVars = (from s in Enumerable.Range(0, shifts.Count)
                                from d in Enumerable.Range(0, days.Count)
                                select (ILiteral)x[e, s, d]).ToArray();

                var existingCount = context.ExistingAssignments.Count(a => a.EmployeeId == employees[e].Id);
                var remaining = Math.Max(0, solverPolicy.MaxShiftsPerEmployeePerWeek - existingCount);
                model.Add(LinearExpr.Sum(weekVars) <= remaining);
            }
        }

        if (solverPolicy.MinStaffPerShiftEnabled)
        {
            ApplySlotStaffingConstraints(
                model,
                x,
                employees.Count,
                shifts,
                days,
                context,
                solverPolicy.MinStaffPerShift,
                enforceFullCoverageLowerBound);
        }

        if (solverPolicy.MinRestMinutesEnabled && minRestMinutes > 0)
        {
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
        }
        else
        {
            for (var e = 0; e < employees.Count; e++)
            for (var i = 0; i < slots.Count; i++)
            for (var j = i + 1; j < slots.Count; j++)
            {
                if (SlotsOverlap(slots[i].Assignment, slots[j].Assignment))
                {
                    model.AddBoolOr([
                        x[e, slots[i].ShiftIndex, slots[i].DayIndex].Not(),
                        x[e, slots[j].ShiftIndex, slots[j].DayIndex].Not()
                    ]);
                }
            }
        }

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

        model.Maximize(LinearExpr.WeightedSum(objVars.ToArray(), objCoeffs.ToArray()));

        var solver = new CpSolver();
        solver.StringParameters =
            $"max_time_in_seconds:{SchedulingSolverDefaults.CpSatMaxTimeSeconds} num_search_workers:{SchedulingSolverDefaults.CpSatSearchWorkers}";
        var status = await Task.Run(() => solver.Solve(model), cancellationToken);

        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            logger.LogWarning(
                "CP-SAT returned {Status} for schedule {ScheduleId} (fullCoverage={FullCoverage})",
                status,
                scheduleId,
                enforceFullCoverageLowerBound);
            return null;
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

    private static void ApplySlotStaffingConstraints(
        CpModel model,
        BoolVar[,,] x,
        int employeeCount,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<DateOnly> days,
        ScheduleSuggestionContext context,
        int minStaffPerSlot,
        bool enforceFullCoverageLowerBound)
    {
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            var slotVars = Enumerable.Range(0, employeeCount).Select(e => (ILiteral)x[e, s, d]).ToArray();
            var existingCount = context.ExistingAssignments.Count(a =>
                a.ShiftDefinitionId == shifts[s].Id && a.Date == days[d]);
            var remainingNeed = Math.Max(0, minStaffPerSlot - existingCount);

            model.Add(LinearExpr.Sum(slotVars) <= remainingNeed);
            if (enforceFullCoverageLowerBound && remainingNeed > 0)
                model.Add(LinearExpr.Sum(slotVars) >= remainingNeed);
        }
    }

    private static bool RoleMatches(Employee employee, ShiftDefinition shift)
    {
        if (string.IsNullOrWhiteSpace(shift.RequiredRole))
            return true;

        var required = shift.RequiredRole.Trim();
        if (string.Equals(required, RoleConstants.User, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            return true;

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
        int minRestMinutes) =>
        SlotsOverlap(left, right)
        || (GetGapMinutes(left, right) < minRestMinutes);

    private static bool SlotsOverlap(AssignmentTimeSlot left, AssignmentTimeSlot right)
    {
        var first = left.Start <= right.Start ? left : right;
        var second = left.Start <= right.Start ? right : left;
        return second.Start < first.End;
    }

    private static double GetGapMinutes(AssignmentTimeSlot left, AssignmentTimeSlot right)
    {
        var first = left.Start <= right.Start ? left : right;
        var second = left.Start <= right.Start ? right : left;
        return (second.Start - first.End).TotalMinutes;
    }

    private sealed record ScheduleSlot(int ShiftIndex, int DayIndex, AssignmentTimeSlot Assignment);

    private sealed record AssignmentTimeSlot(DateTime Start, DateTime End);
}
