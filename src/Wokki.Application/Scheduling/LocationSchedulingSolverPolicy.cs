using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

/// <summary>
/// Typed branch policy inputs for scheduling solvers (CP-SAT / heuristic).
/// Apply/review workflow is not configured here — see <see cref="SchedulingSolverDefaults.SuggestionsRequireExplicitApply"/>.
/// </summary>
public sealed record LocationSchedulingSolverPolicy(
    bool RequireSubmittedPreferences,
    bool UnavailableIsHardBlock,
    bool RequireRoleMatch,
    bool RequireFullCoverage,
    bool AllowUnderstaffedSuggestions,
    int DefaultMinStaffPerShift,
    int MinRestMinutesBetweenShifts,
    int MinShiftsPerWeek,
    bool MinShiftsPerWeekEnabled,
    bool AllowOvertime)
{
    public static LocationSchedulingSolverPolicy FromLocationPolicy(LocationSchedulingPolicy? policy)
    {
        var minWeekly = LocationSchedulingPolicyRules.GetInt(policy, "min_shifts_per_week", 0);
        var minWeeklyRule = policy is null
            ? null
            : LocationSchedulingPolicyRules.GetEffectiveRules(policy)
                .FirstOrDefault(r => r.Key.Equals("min_shifts_per_week", StringComparison.OrdinalIgnoreCase));

        return new LocationSchedulingSolverPolicy(
            LocationSchedulingPolicyRules.GetBool(policy, "require_submitted_preferences", true),
            LocationSchedulingPolicyRules.GetBool(policy, "unavailable_is_hard_block", true),
            LocationSchedulingPolicyRules.GetBool(policy, "require_role_match", true),
            LocationSchedulingPolicyRules.GetBool(policy, "require_full_coverage", true),
            LocationSchedulingPolicyRules.GetBool(policy, "allow_understaffed_suggestions", false),
            LocationSchedulingPolicyRules.GetInt(policy, "default_min_staff_per_shift", 1),
            LocationSchedulingPolicyRules.GetInt(policy, "min_rest_minutes_between_shifts", 660),
            minWeekly,
            minWeeklyRule?.Enabled ?? minWeekly > 0,
            LocationSchedulingPolicyRules.GetBool(policy, "allow_overtime", false));
    }
}
