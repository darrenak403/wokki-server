using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Application.Scheduling;

/// <summary>
/// Which existing draft assignments CP-SAT treats as fixed when re-suggesting.
/// </summary>
public static class SchedulingAssignmentLockPolicy
{
    public sealed record LockState(
        HashSet<(Guid EmployeeId, Guid ShiftDefinitionId, DateOnly Date)> LockedAssignmentKeys,
        HashSet<(Guid ShiftDefinitionId, DateOnly Date)> LockedSlotKeys,
        bool HasPreferenceChangesAfterAssignments,
        int ConflictAssignmentCount,
        HashSet<Guid> UnlockedEmployeeIds);

    public static LockState Compute(ScheduleSuggestionContext context)
    {
        var assignments = context.ExistingAssignments;
        if (assignments.Count == 0)
        {
            return new LockState([], [], false, 0, []);
        }

        var unavailableKeys = context.SubmittedPreferences
            .Where(p => p.PreferenceType == PreferenceType.Unavailable)
            .Select(p => (p.EmployeeId, p.ShiftDefinitionId, p.Date))
            .ToHashSet();

        var preferenceBaseline = ScheduleRebalanceBaseline.GetPreferenceChangeBaseline(
            context.Schedule,
            assignments);
        var changedEmployeeIds = ScheduleRebalanceBaseline.GetPreferenceChangedEmployeeIdsAfterBaseline(
            context.PreferenceSubmissions,
            preferenceBaseline);

        var conflictAssignments = assignments
            .Where(a => unavailableKeys.Contains((a.EmployeeId, a.ShiftDefinitionId, a.Date)))
            .ToList();
        var conflictCount = conflictAssignments.Count;
        var unlockedEmployeeIds = changedEmployeeIds
            .Concat(conflictAssignments.Select(a => a.EmployeeId))
            .ToHashSet();

        var lockedKeys = new HashSet<(Guid, Guid, DateOnly)>();
        var lockedSlots = new HashSet<(Guid, DateOnly)>();

        foreach (var group in assignments.GroupBy(a => (a.ShiftDefinitionId, a.Date)))
        {
            var slotAssignments = group.ToList();
            var hasUnlockedAssignment = false;

            foreach (var assignment in slotAssignments)
            {
                if (unlockedEmployeeIds.Contains(assignment.EmployeeId))
                {
                    hasUnlockedAssignment = true;
                    continue;
                }

                lockedKeys.Add((assignment.EmployeeId, assignment.ShiftDefinitionId, assignment.Date));
            }

            if (!hasUnlockedAssignment)
                lockedSlots.Add(group.Key);
        }

        return new LockState(
            lockedKeys,
            lockedSlots,
            changedEmployeeIds.Count > 0,
            conflictCount,
            unlockedEmployeeIds);
    }

    public static int CountOpenSlots(
        int shiftCount,
        int dayCount,
        LockState lockState) =>
        Math.Max(0, shiftCount * dayCount - lockState.LockedSlotKeys.Count);
}
