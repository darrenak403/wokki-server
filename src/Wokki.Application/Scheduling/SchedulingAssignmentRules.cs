using Wokki.Application.Scheduling;
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
}
