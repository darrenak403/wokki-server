using System.Text;
using System.Text.Json;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class ScheduleSuggestionPromptBuilder
{
    public static string BuildUserPrompt(ScheduleSuggestionContext context)
    {
        var weekEnd = context.Schedule.WeekStartDate.AddDays(6);
        var maxWeekly = SchedulingAssignmentRules.MaxShiftsPerEmployeePerWeek(context);
        var payload = new
        {
            scheduleId = context.Schedule.Id,
            departmentId = context.Schedule.DepartmentId,
            weekStartDate = context.Schedule.WeekStartDate.ToString("yyyy-MM-dd"),
            weekEndDate = weekEnd.ToString("yyyy-MM-dd"),
            rules = new
            {
                maxShiftsPerEmployeePerWeek = maxWeekly,
                jobPositions = context.JobPositions.Select(p => new
                {
                    id = p.Id,
                    code = p.Code,
                    name = p.Name,
                    targetHeadcount = p.TargetHeadcount
                }),
                shiftCapacity = context.Shifts.Select(s => new
                {
                    shiftDefinitionId = s.Id,
                    maxStaffPerSlot = s.MaxStaffPerSlot
                })
            },
            employees = context.Employees.Select(e => new
            {
                id = e.Id,
                name = $"{e.FirstName} {e.LastName}".Trim(),
                position = e.Position,
                jobPositionId = e.JobPositionId
            }),
            shifts = context.Shifts.Select(s => new
            {
                id = s.Id,
                name = s.Name,
                startTime = s.StartTime.ToString("HH:mm"),
                endTime = s.EndTime.ToString("HH:mm"),
                requiredRole = s.RequiredRole,
                maxStaffPerSlot = s.MaxStaffPerSlot
            }),
            employeePreferences = context.SubmittedPreferences.Select(p => new
            {
                employeeId = p.EmployeeId,
                shiftDefinitionId = p.ShiftDefinitionId,
                date = p.Date.ToString("yyyy-MM-dd"),
                preference = p.PreferenceType.ToString()
            }),
            existingAssignments = context.ExistingAssignments.Select(a => new
            {
                shiftDefinitionId = a.ShiftDefinitionId,
                employeeId = a.EmployeeId,
                date = a.Date.ToString("yyyy-MM-dd")
            }),
            historicalSummary = context.HistoricalAssignments
                .GroupBy(a => new { a.ShiftDefinitionId, a.EmployeeId, Day = a.Date.DayOfWeek })
                .Select(g => new
                {
                    shiftDefinitionId = g.Key.ShiftDefinitionId,
                    employeeId = g.Key.EmployeeId,
                    dayOfWeek = g.Key.Day.ToString(),
                    count = g.Count()
                })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var sb = new StringBuilder();
        sb.AppendLine("You are a workforce scheduling assistant for a single coffee shop branch.");
        sb.AppendLine("Assign employees to shift slots for the target week.");
        sb.AppendLine("Hard rules: respect requiredRole; maxStaffPerSlot per shift+date; maxShiftsPerEmployeePerWeek; no overlapping shifts same day; honor Unavailable preferences; prefer Preferred.");
        sb.AppendLine("Fairness: distribute shifts evenly among employees sharing the same job position (targetHeadcount is staffing guidance).");
        sb.AppendLine("Do not duplicate existingAssignments.");
        sb.AppendLine("Return ONLY valid JSON matching this schema:");
        sb.AppendLine("""{"suggestions":[{"shiftDefinitionId":"guid","employeeId":"guid","date":"yyyy-MM-dd"}]}""");
        sb.AppendLine();
        sb.AppendLine("Input data:");
        sb.Append(json);
        return sb.ToString();
    }
}
