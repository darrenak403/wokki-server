using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class SchedulingShiftConflictRules
{
    public static bool HasTimeOverlap(
        TimeOnly startTime,
        TimeOnly endTime,
        TimeOnly otherStart,
        TimeOnly otherEnd) =>
        startTime < otherEnd && otherStart < endTime;

    public static bool ConflictsWithRestPolicy(
        Guid employeeId,
        DateOnly date,
        ShiftDefinition candidateShift,
        IReadOnlyList<ShiftAssignment> assignments,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlySet<Guid> ignoredAssignmentIds,
        int minRestMinutes)
    {
        if (minRestMinutes <= 0)
            return false;

        var candidate = BuildSlot(date, candidateShift);
        foreach (var assignment in assignments)
        {
            if (assignment.EmployeeId != employeeId || ignoredAssignmentIds.Contains(assignment.Id))
                continue;

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
        SlotsOverlap(left, right) || GetGapMinutes(left, right) < minRestMinutes;

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

    private sealed record AssignmentTimeSlot(DateTime Start, DateTime End);
}
