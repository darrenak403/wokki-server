using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Application.Scheduling;

public static class SchedulingAssignmentRules
{
    public static int MaxShiftsPerEmployeePerWeek(ScheduleSuggestionContext context) =>
        SchedulingSolverDefaults.MaxShiftsPerEmployeePerWeek;

    public static int MaxShiftsPerEmployeePerDay(ScheduleSuggestionContext context) =>
        SchedulingSolverDefaults.MaxShiftsPerEmployeePerDaySafetyCap;

    public static int MinShiftsPerEmployeePerWeek(ScheduleSuggestionContext context) =>
        SchedulingSolverDefaults.MinShiftsPerWeek;

    public static bool MeetsWeeklyCap(
        Guid employeeId,
        List<ShiftAssignmentEntity> planned,
        ScheduleSuggestionContext context) =>
        planned.Count(a => a.EmployeeId == employeeId) < MaxShiftsPerEmployeePerWeek(context);

    public static bool MeetsDailyCap(
        Guid employeeId,
        DateOnly date,
        List<ShiftAssignmentEntity> planned,
        ScheduleSuggestionContext context) =>
        planned.Count(a => a.EmployeeId == employeeId && a.Date == date) < MaxShiftsPerEmployeePerDay(context);

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
