using Wokki.Application.Dtos.Scheduling;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

/// <summary>
/// Typed org policy inputs for scheduling solvers (CP-SAT). Only enabled catalog rules apply.
/// </summary>
public sealed record OrganizationSchedulingSolverPolicy
{
    public bool RequireSubmittedPreferences { get; init; }
    public bool UnavailableIsHardBlock { get; init; }
    public bool RequireRoleMatch { get; init; }
    public bool RequireFullCoverage { get; init; }
    public bool MinStaffPerShiftEnabled { get; init; }
    public int MinStaffPerShift { get; init; }
    public bool MaxStaffPerShiftEnabled { get; init; }
    public int MaxStaffPerShift { get; init; }
    public bool MinRestMinutesEnabled { get; init; }
    public int MinRestMinutesBetweenShifts { get; init; }
    public bool MaxShiftsPerDayEnabled { get; init; }
    public int MaxShiftsPerEmployeePerDay { get; init; }
    public bool MaxShiftsPerWeekEnabled { get; init; }
    public int MaxShiftsPerEmployeePerWeek { get; init; }

    public bool HasAnyEnabledRule =>
        RequireSubmittedPreferences
        || UnavailableIsHardBlock
        || RequireRoleMatch
        || RequireFullCoverage
        || MinStaffPerShiftEnabled
        || MaxStaffPerShiftEnabled
        || MinRestMinutesEnabled
        || MaxShiftsPerDayEnabled
        || MaxShiftsPerWeekEnabled;

    public static OrganizationSchedulingSolverPolicy FromOrgPolicy(OrganizationSchedulingPolicy? policy) =>
        FromEffectiveRules(OrganizationSchedulingPolicyRules.GetEffectiveRules(policy));

    public static OrganizationSchedulingSolverPolicy FromEffectiveRules(IReadOnlyList<SchedulingRuleDto> rules)
    {
        var byKey = rules
            .Where(rule => rule.Enforcement == "enforced")
            .ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);

        return new OrganizationSchedulingSolverPolicy
        {
            RequireSubmittedPreferences = IsEnabledBool(byKey, "require_submitted_preferences"),
            UnavailableIsHardBlock = IsEnabledBool(byKey, "unavailable_is_hard_block"),
            RequireRoleMatch = IsEnabledBool(byKey, "require_role_match"),
            RequireFullCoverage = IsEnabledBool(byKey, "require_full_coverage"),
            MinStaffPerShiftEnabled = TryGetEnabledPositiveInt(byKey, "default_min_staff_per_shift", out var minStaff),
            MinStaffPerShift = minStaff,
            MaxStaffPerShiftEnabled = TryGetEnabledPositiveInt(byKey, "default_max_staff_per_shift", out var maxStaff),
            MaxStaffPerShift = maxStaff,
            MinRestMinutesEnabled = TryGetEnabledPositiveInt(byKey, "min_rest_minutes_between_shifts", out var rest),
            MinRestMinutesBetweenShifts = rest,
            MaxShiftsPerDayEnabled = TryGetEnabledPositiveInt(byKey, "max_shifts_per_employee_per_day", out var maxDay),
            MaxShiftsPerEmployeePerDay = maxDay,
            MaxShiftsPerWeekEnabled = TryGetEnabledPositiveInt(byKey, "max_shifts_per_employee_per_week", out var maxWeek),
            MaxShiftsPerEmployeePerWeek = maxWeek,
        };
    }

    private static bool IsEnabledBool(IReadOnlyDictionary<string, SchedulingRuleDto> byKey, string key) =>
        byKey.TryGetValue(key, out var rule)
        && rule.Enabled
        && OrganizationSchedulingPolicyRules.ReadBoolValue(rule.Value, fallback: true);

    private static bool TryGetEnabledPositiveInt(
        IReadOnlyDictionary<string, SchedulingRuleDto> byKey,
        string key,
        out int value)
    {
        value = 0;
        if (!byKey.TryGetValue(key, out var rule) || !rule.Enabled)
            return false;

        value = OrganizationSchedulingPolicyRules.ReadIntValue(rule.Value, fallback: 0);
        return value > 0;
    }
}
