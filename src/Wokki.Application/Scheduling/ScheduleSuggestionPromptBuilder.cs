using System.Text;

namespace Wokki.Application.Scheduling;

public sealed class ScheduleSuggestionPromptBuilder
{
    public string Build(ScheduleSuggestionContext context)
    {
        var policy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(context.OrganizationSchedulingPolicy);
        var weekEnd = context.Schedule.WeekStartDate.AddDays(6);
        var sb = new StringBuilder();

        sb.AppendLine($"Department: {context.Department.Name}");
        sb.AppendLine($"Week: {context.Schedule.WeekStartDate:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("EMPLOYEES (id | name | position)");
        foreach (var emp in context.Employees)
            sb.AppendLine($"{emp.Id} | {emp.FirstName} {emp.LastName} | {emp.Position ?? ""}");
        sb.AppendLine();

        sb.AppendLine("SHIFTS (shiftId | shiftName | date | requiredRole | startTime | endTime)");
        for (var d = 0; d < 7; d++)
        {
            var date = context.Schedule.WeekStartDate.AddDays(d);
            foreach (var shift in context.Shifts)
                sb.AppendLine($"{shift.Id} | {shift.Name} | {date:yyyy-MM-dd} | {shift.RequiredRole ?? ""} | {shift.StartTime:HH\\:mm} | {shift.EndTime:HH\\:mm}");
        }
        sb.AppendLine();

        if (context.SubmittedPreferences.Count > 0)
        {
            sb.AppendLine("PREFERENCES (employeeId | shiftId | date | preferenceType)");
            foreach (var pref in context.SubmittedPreferences)
                sb.AppendLine($"{pref.EmployeeId} | {pref.ShiftDefinitionId} | {pref.Date:yyyy-MM-dd} | {pref.PreferenceType}");
            sb.AppendLine();
        }

        sb.AppendLine("POLICY (enabled rules only)");
        sb.AppendLine($"requireSubmittedPreferences={policy.RequireSubmittedPreferences}");
        sb.AppendLine($"unavailableIsHardBlock={policy.UnavailableIsHardBlock}");
        sb.AppendLine($"requireRoleMatch={policy.RequireRoleMatch}");
        sb.AppendLine($"requireFullCoverage={policy.RequireFullCoverage}");
        sb.AppendLine($"minStaffPerShift={(policy.MinStaffPerShiftEnabled ? policy.MinStaffPerShift.ToString() : "off")}");
        sb.AppendLine($"maxStaffPerShift={(policy.MaxStaffPerShiftEnabled ? policy.MaxStaffPerShift.ToString() : "off")}");
        sb.AppendLine($"minRestMinutes={(policy.MinRestMinutesEnabled ? policy.MinRestMinutesBetweenShifts.ToString() : "off")}");
        sb.AppendLine($"maxShiftsPerDay={(policy.MaxShiftsPerDayEnabled ? policy.MaxShiftsPerEmployeePerDay.ToString() : "off")}");
        sb.AppendLine($"maxShiftsPerWeek={(policy.MaxShiftsPerWeekEnabled ? policy.MaxShiftsPerEmployeePerWeek.ToString() : "off")}");
        sb.AppendLine($"anyRuleEnabled={policy.HasAnyEnabledRule}");
        sb.AppendLine();

        sb.AppendLine("Respond ONLY with a JSON array. No markdown, no explanation.");
        sb.AppendLine("Format: [{\"shiftDefinitionId\":\"<guid>\",\"employeeId\":\"<guid>\",\"date\":\"YYYY-MM-DD\"}]");
        sb.AppendLine("Assign employees to shifts respecting all constraints above.");

        return sb.ToString();
    }
}
