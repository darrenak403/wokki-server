using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Scheduling;
using Wokki.Application.Validators.Scheduling;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Scheduling;

public sealed class CpSatScheduleSuggestionService(
    ScheduleSuggestionContextLoader contextLoader,
    ILogger<CpSatScheduleSuggestionService> logger) : IScheduleSuggestionService
{
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        ScheduleSuggestionHint? hint = null,
        CancellationToken cancellationToken = default)
    {
        var (context, reason) = await contextLoader.LoadAsync(scheduleId, cancellationToken);
        if (context is null)
            return new ScheduleSuggestionGenerationResult([], reason, Provider: "cpsat");

        if (context.Employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees", Provider: "cpsat");

        if (context.Shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts", Provider: "cpsat");

        if (hint is not null)
        {
            var hintValidation = ScheduleSuggestionHintValidator.Validate(hint, context.Employees);
            if (!hintValidation.IsValid)
                return new ScheduleSuggestionGenerationResult([], "invalid_hint", Provider: "cpsat");
        }

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

        var lockState = SchedulingAssignmentLockPolicy.Compute(context);
        var openSlots = SchedulingAssignmentLockPolicy.CountOpenSlots(
            shifts.Count,
            days.Count,
            lockState);
        var restrictToUnlockedEmployees = context.ExistingAssignments.Count > 0
            && lockState.UnlockedEmployeeIds.Count > 0;

        if (openSlots == 0 && context.ExistingAssignments.Count > 0)
        {
            logger.LogInformation(
                "Schedule {ScheduleId}: all {SlotCount} slots locked by existing assignments — re-suggest needs preference change or manual clear",
                scheduleId,
                shifts.Count * days.Count);
            return new ScheduleSuggestionGenerationResult([], "fully_assigned", Provider: "cpsat");
        }

        if (lockState.HasPreferenceChangesAfterAssignments)
        {
            logger.LogInformation(
                "Schedule {ScheduleId}: unlocking assignments for re-suggest after preference updates",
                scheduleId);
        }

        var existingSet = lockState.LockedAssignmentKeys;
        var lockedStaffCountBySlot = context.ExistingAssignments
            .Where(a => existingSet.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date)))
            .GroupBy(a => (a.ShiftDefinitionId, a.Date))
            .ToDictionary(g => g.Key, g => g.Count());
        var shiftsById = shifts.ToDictionary(s => s.Id);
        var existingByEmployee = context.ExistingAssignments
            .Where(a => existingSet.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date))
                        && shiftsById.ContainsKey(a.ShiftDefinitionId))
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
            lockedStaffCountBySlot,
            existingByEmployee,
            shiftsById,
            restrictToUnlockedEmployees ? lockState.UnlockedEmployeeIds : null,
            enforceFullCoverageLowerBound: useStrictCoverage,
            hint,
            cancellationToken);

        if (strictResult is not null)
            return strictResult;

        if (!useStrictCoverage)
            return new ScheduleSuggestionGenerationResult(
                [], hint is not null ? "hint_infeasible" : "infeasible", Provider: "cpsat");

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
            lockedStaffCountBySlot,
            existingByEmployee,
            shiftsById,
            restrictToUnlockedEmployees ? lockState.UnlockedEmployeeIds : null,
            enforceFullCoverageLowerBound: false,
            hint,
            cancellationToken);

        if (relaxedResult is not null)
        {
            return relaxedResult with
            {
                Reason = "partial_coverage",
                FallbackUsed = true
            };
        }

        return new ScheduleSuggestionGenerationResult(
            [], hint is not null ? "hint_infeasible" : "infeasible", Provider: "cpsat");
    }

    private async Task<ScheduleSuggestionGenerationResult?> SolveAsync(
        Guid scheduleId,
        ScheduleSuggestionContext context,
        OrganizationSchedulingSolverPolicy solverPolicy,
        IReadOnlyList<Employee> employees,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<DateOnly> days,
        HashSet<(Guid EmployeeId, Guid ShiftDefinitionId, DateOnly Date)> existingSet,
        IReadOnlyDictionary<(Guid ShiftDefinitionId, DateOnly Date), int> lockedStaffCountBySlot,
        IReadOnlyDictionary<Guid, List<ShiftAssignment>> existingByEmployee,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlySet<Guid>? unlockedEmployeeIds,
        bool enforceFullCoverageLowerBound,
        ScheduleSuggestionHint? hint,
        CancellationToken cancellationToken)
    {
        var hintTargetIds = hint?.Group is not null
            ? ResolveHintTargetEmployeeIds(hint.Group, employees)
            : null;

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

            if (unlockedEmployeeIds is not null && !unlockedEmployeeIds.Contains(emp.Id))
            {
                model.Add(x[e, s, d] == 0);
                continue;
            }

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

            if (!SchedulingAssignmentRules.MayAssignBySubmittedPreference(emp.Id, shift.Id, date, context))
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
                    a.EmployeeId == employees[e].Id
                    && a.Date == days[d]
                    && existingSet.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date)));
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

                var existingCount = context.ExistingAssignments.Count(a =>
                    a.EmployeeId == employees[e].Id
                    && existingSet.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date)));
                var remaining = Math.Max(0, solverPolicy.MaxShiftsPerEmployeePerWeek - existingCount);
                model.Add(LinearExpr.Sum(weekVars) <= remaining);
            }
        }

        if (hint?.Kind == ScheduleSuggestionHintKind.TempMinMax && hintTargetIds is { Count: > 0 })
        {
            var hintVars = (from e in Enumerable.Range(0, employees.Count)
                            where hintTargetIds.Contains(employees[e].Id)
                            from s in Enumerable.Range(0, shifts.Count)
                            from d in Enumerable.Range(0, days.Count)
                            select (ILiteral)x[e, s, d]).ToArray();

            if (hintVars.Length > 0)
            {
                var hintSum = LinearExpr.Sum(hintVars);
                if (hint.MaxCount.HasValue)
                    model.Add(hintSum <= hint.MaxCount.Value);
                if (hint.MinCount.HasValue)
                    model.Add(hintSum >= hint.MinCount.Value);
            }
        }

        if (hint?.Kind == ScheduleSuggestionHintKind.AvoidPairing
            && hint.EmployeeId1 is { } pairEmployeeId1
            && hint.EmployeeId2 is { } pairEmployeeId2)
        {
            var pairIndex1 = -1;
            var pairIndex2 = -1;
            for (var i = 0; i < employees.Count; i++)
            {
                if (employees[i].Id == pairEmployeeId1) pairIndex1 = i;
                else if (employees[i].Id == pairEmployeeId2) pairIndex2 = i;
            }

            if (pairIndex1 >= 0 && pairIndex2 >= 0)
            {
                for (var s = 0; s < shifts.Count; s++)
                for (var d = 0; d < days.Count; d++)
                    model.Add(x[pairIndex1, s, d] + x[pairIndex2, s, d] <= 1);
            }
        }

        if (solverPolicy.MinStaffPerShiftEnabled || solverPolicy.MaxStaffPerShiftEnabled)
        {
            ApplySlotStaffingConstraints(
                model,
                x,
                employees.Count,
                shifts,
                days,
                lockedStaffCountBySlot,
                solverPolicy.MinStaffPerShiftEnabled ? solverPolicy.MinStaffPerShift : 0,
                solverPolicy.MaxStaffPerShiftEnabled ? solverPolicy.MaxStaffPerShift : null,
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

            if (hint?.Kind == ScheduleSuggestionHintKind.PreferenceWeight
                && hintTargetIds is not null
                && hintTargetIds.Contains(employees[e].Id))
            {
                score += hint.Direction == HintWeightDirection.Reduce
                    ? -SchedulingSolverDefaults.HintPreferenceWeightDelta
                    : SchedulingSolverDefaults.HintPreferenceWeightDelta;
            }

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

        if (results.Count == 0 && context.ExistingAssignments.Count > 0)
            return new ScheduleSuggestionGenerationResult([], "fully_assigned", Provider: "cpsat");

        return new ScheduleSuggestionGenerationResult(results, null, Provider: "cpsat");
    }

    private static void ApplySlotStaffingConstraints(
        CpModel model,
        BoolVar[,,] x,
        int employeeCount,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<DateOnly> days,
        IReadOnlyDictionary<(Guid ShiftDefinitionId, DateOnly Date), int> lockedStaffCountBySlot,
        int minStaffPerSlot,
        int? maxStaffPerSlot,
        bool enforceFullCoverageLowerBound)
    {
        for (var s = 0; s < shifts.Count; s++)
        for (var d = 0; d < days.Count; d++)
        {
            var slotKey = (shifts[s].Id, days[d]);
            var slotVars = Enumerable.Range(0, employeeCount).Select(e => (ILiteral)x[e, s, d]).ToArray();
            var lockedCount = lockedStaffCountBySlot.GetValueOrDefault(slotKey, 0);
            var sum = LinearExpr.Sum(slotVars);

            if (maxStaffPerSlot.HasValue)
            {
                var remainingCapacity = Math.Max(0, maxStaffPerSlot.Value - lockedCount);
                model.Add(sum <= remainingCapacity);
            }
            else if (minStaffPerSlot > 0)
            {
                var remainingNeed = Math.Max(0, minStaffPerSlot - lockedCount);
                model.Add(sum <= remainingNeed);
            }

            if (enforceFullCoverageLowerBound && minStaffPerSlot > 0)
            {
                var remainingNeed = Math.Max(0, minStaffPerSlot - lockedCount);
                if (remainingNeed > 0)
                    model.Add(sum >= remainingNeed);
            }
        }
    }

    private static HashSet<Guid> ResolveHintTargetEmployeeIds(
        HintGroupRef group,
        IReadOnlyList<Employee> employees)
    {
        if (group.GroupType == HintGroupType.ExplicitEmployeeIds)
            return group.EmployeeIds is null ? [] : new HashSet<Guid>(group.EmployeeIds);

        if (group.GroupType == HintGroupType.RoleDepartment && !string.IsNullOrWhiteSpace(group.Role))
        {
            var role = group.Role.Trim();
            return employees
                .Where(e => string.Equals(e.Position?.Trim(), role, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Id)
                .ToHashSet();
        }

        return [];
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
