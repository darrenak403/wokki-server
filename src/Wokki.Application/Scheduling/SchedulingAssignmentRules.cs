using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Application.Scheduling;

public static class SchedulingAssignmentRules
{
    public static bool MeetsWeeklyCap(
        Guid employeeId,
        List<ShiftAssignment> planned,
        ScheduleSuggestionContext context)
    {
        var policy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(context.OrganizationSchedulingPolicy);
        if (!policy.MaxShiftsPerWeekEnabled)
            return true;

        return planned.Count(a => a.EmployeeId == employeeId) < policy.MaxShiftsPerEmployeePerWeek;
    }

    public static bool MeetsDailyCap(
        Guid employeeId,
        DateOnly date,
        List<ShiftAssignment> planned,
        ScheduleSuggestionContext context)
    {
        var policy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(context.OrganizationSchedulingPolicy);
        if (!policy.MaxShiftsPerDayEnabled)
            return true;

        return planned.Count(a => a.EmployeeId == employeeId && a.Date == date)
               < policy.MaxShiftsPerEmployeePerDay;
    }

    public static int PreferenceScore(
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        ScheduleSuggestionContext context)
    {
        var line = context.SubmittedPreferences.FirstOrDefault(l =>
            l.EmployeeId == employeeId
            && l.ShiftDefinitionId == shiftDefinitionId
            && l.Date == date);

        if (line is null)
            return 0;

        return line.PreferenceType switch
        {
            PreferenceType.Preferred => SchedulingSolverDefaults.PreferencePreferredScore,
            PreferenceType.Available => SchedulingSolverDefaults.PreferenceAvailableScore,
            PreferenceType.Unavailable => SchedulingSolverDefaults.PreferenceUnavailablePenalty,
            _ => 0
        };
    }

    public static bool IsUnavailableByPreference(
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        ScheduleSuggestionContext context) =>
        context.SubmittedPreferences.Any(l =>
            l.EmployeeId == employeeId
            && l.ShiftDefinitionId == shiftDefinitionId
            && l.Date == date
            && l.PreferenceType == PreferenceType.Unavailable);

    /// <summary>
    /// Employee submitted a preference board for this schedule: only Preferred/Available lines may be assigned.
    /// Missing line (UI "Trống") or Unavailable → cannot assign.
    /// </summary>
    public static bool MayAssignBySubmittedPreference(
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        ScheduleSuggestionContext context)
    {
        var hasSubmittedBoard = context.PreferenceSubmissions.Any(s =>
            s.EmployeeId == employeeId && s.Status == SchedulePreferenceStatus.Submitted);

        if (!hasSubmittedBoard)
            return true;

        var line = context.SubmittedPreferences.FirstOrDefault(l =>
            l.EmployeeId == employeeId
            && l.ShiftDefinitionId == shiftDefinitionId
            && l.Date == date);

        if (line is null)
            return false;

        return line.PreferenceType is PreferenceType.Preferred or PreferenceType.Available;
    }
}
