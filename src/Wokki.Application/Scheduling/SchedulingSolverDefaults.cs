namespace Wokki.Application.Scheduling;

/// <summary>
/// Solver technical parameters only — not org business rules (those live in org scheduling policy catalog).
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

    /// <summary>Apply is always explicit (review sheet + Apply button); never auto-applied from policy.</summary>
    public const bool SuggestionsRequireExplicitApply = true;

    public const int CpSatMaxTimeSeconds = 10;
    public const int CpSatSearchWorkers = 4;
}
