namespace Wokki.Application.Scheduling;

/// <summary>
/// Solver parameters fixed in code (not exposed on branch-policy UI). Used by heuristic and future CP-SAT.
/// </summary>
public static class SchedulingSolverDefaults
{
    public const bool RequireDepartmentMembership = true;
    public const bool RequireActiveEmployee = true;
    public const bool AllowTerminatedEmployees = false;

    public const int PreferencePreferredScore = 30;
    public const int PreferenceAvailableScore = 5;
    public const int PreferenceUnavailablePenalty = -30;

    public const int RoleBalancePenaltyPerShift = 3;
    public const int MinShiftsPerWeekBoostPerMissingShift = 8;
    public const int MaxShiftsPerEmployeePerDaySafetyCap = 2;

    /// <summary>Hard weekly cap per employee (not configurable per org).</summary>
    public const int MaxShiftsPerEmployeePerWeek = 20;

    /// <summary>Minimum rest between consecutive shifts (11 hours).</summary>
    public const int MinRestMinutesBetweenShifts = 660;

    /// <summary>Solver tries to fill each shift to min staff; not exposed in org policy UI.</summary>
    public const bool RequireFullCoverage = true;

    /// <summary>When false, solver may return understaffed suggestions if infeasible.</summary>
    public const bool AllowUnderstaffedSuggestions = false;

    public const int MinShiftsPerWeek = 0;

    public const bool AllowOvertime = false;

    /// <summary>Apply is always explicit (review sheet + Apply button); never auto-applied from policy.</summary>
    public const bool SuggestionsRequireExplicitApply = true;
}
