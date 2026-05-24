using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;

namespace Wokki.Application.Scheduling;

public static class SchedulingAssignmentRules
{
    public static int MaxShiftsPerEmployeePerWeek(ScheduleSuggestionContext context) =>
        context.SchedulingPolicy?.MaxShiftsPerEmployeePerWeek
        ?? LocationSchedulingPolicyRules.GetInt(context.LocationSchedulingPolicy, "max_shifts_per_week", 20);

    public static bool MeetsWeeklyCap(
        Guid employeeId,
        List<ShiftAssignmentEntity> planned,
        ScheduleSuggestionContext context) =>
        planned.Count(a => a.EmployeeId == employeeId) < MaxShiftsPerEmployeePerWeek(context);

    public static bool MeetsSlotCapacity(
        Guid shiftDefinitionId,
        DateOnly date,
        ShiftDefinition shift,
        List<ShiftAssignmentEntity> planned) =>
        planned.Count(a => a.ShiftDefinitionId == shiftDefinitionId && a.Date == date) < shift.MaxStaffPerSlot;

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
            PreferenceType.Preferred => LocationSchedulingPolicyRules.GetInt(context.LocationSchedulingPolicy, "preferred_weight", 30),
            PreferenceType.Available => LocationSchedulingPolicyRules.GetInt(context.LocationSchedulingPolicy, "available_weight", 5),
            PreferenceType.Unavailable => int.MinValue,
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

    public static JobPosition? ResolveJobPosition(Employee employee, ScheduleSuggestionContext context)
    {
        if (employee.JobPositionId is Guid id)
            return context.JobPositions.FirstOrDefault(p => p.Id == id);

        return context.JobPositions.FirstOrDefault(p =>
            string.Equals(p.Code, employee.Position, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Name, employee.Position, StringComparison.OrdinalIgnoreCase));
    }
}
