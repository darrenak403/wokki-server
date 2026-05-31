using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

/// <summary>
/// Baseline for "preference changed after last schedule action" (rebalance banner + CP-SAT lock).
/// After apply-suggestions, <see cref="Schedule.SuggestionsAppliedAt"/> becomes the watermark until employees submit again.
/// </summary>
public static class ScheduleRebalanceBaseline
{
    public static DateTime GetPreferenceChangeBaseline(
        Schedule schedule,
        IReadOnlyList<ShiftAssignment> assignments)
    {
        if (schedule.SuggestionsAppliedAt is { } appliedAt)
            return appliedAt;

        return assignments.Count == 0 ? DateTime.MinValue : assignments.Max(a => a.CreatedAt);
    }

    public static bool HasPreferenceChangesAfterBaseline(
        IEnumerable<SchedulePreferenceSubmission> submissions,
        DateTime baseline) =>
        GetPreferenceChangedEmployeeIdsAfterBaseline(submissions, baseline).Count > 0;

    public static HashSet<Guid> GetPreferenceChangedEmployeeIdsAfterBaseline(
        IEnumerable<SchedulePreferenceSubmission> submissions,
        DateTime baseline) =>
        submissions.Where(s =>
        {
            var changedAt = s.SubmittedAt ?? s.UpdatedAt ?? s.CreatedAt;
            return changedAt > baseline;
        }).Select(s => s.EmployeeId).ToHashSet();
}
