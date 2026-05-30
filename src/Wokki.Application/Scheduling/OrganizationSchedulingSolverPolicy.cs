using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

/// <summary>
/// Typed org policy inputs for scheduling solvers (CP-SAT / heuristic).
/// Apply/review workflow is not configured here — see <see cref="SchedulingSolverDefaults.SuggestionsRequireExplicitApply"/>.
/// </summary>
public sealed record OrganizationSchedulingSolverPolicy(
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
    public static OrganizationSchedulingSolverPolicy FromOrgPolicy(OrganizationSchedulingPolicy? policy) =>
        new(
            OrganizationSchedulingPolicyRules.GetBool(policy, "require_submitted_preferences", true),
            OrganizationSchedulingPolicyRules.GetBool(policy, "unavailable_is_hard_block", true),
            OrganizationSchedulingPolicyRules.GetBool(policy, "require_role_match", true),
            SchedulingSolverDefaults.RequireFullCoverage,
            SchedulingSolverDefaults.AllowUnderstaffedSuggestions,
            OrganizationSchedulingPolicyRules.GetInt(policy, "default_min_staff_per_shift", 1),
            SchedulingSolverDefaults.MinRestMinutesBetweenShifts,
            SchedulingSolverDefaults.MinShiftsPerWeek,
            MinShiftsPerWeekEnabled: false,
            SchedulingSolverDefaults.AllowOvertime);
}
