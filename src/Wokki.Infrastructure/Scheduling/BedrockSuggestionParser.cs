using System.Text.Json;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Scheduling;

internal static class BedrockSuggestionParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<ScheduleSuggestionDto> ParseAndValidate(
        string responseText,
        ScheduleSuggestionValidationContext validation)
    {
        var json = ExtractJsonObject(responseText);
        var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("suggestions", out var suggestionsElement)
            || suggestionsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var weekEnd = validation.WeekStartDate.AddDays(6);
        var validEmployeeIds = validation.EmployeeIds.ToHashSet();
        var validShiftIds = validation.ShiftIds.ToHashSet();
        var existingKeys = validation.ExistingSlotKeys.ToHashSet();

        var results = new List<ScheduleSuggestionDto>();
        foreach (var item in suggestionsElement.EnumerateArray())
        {
            if (!item.TryGetProperty("shiftDefinitionId", out var shiftProp)
                || !item.TryGetProperty("employeeId", out var employeeProp)
                || !item.TryGetProperty("date", out var dateProp))
                continue;

            if (!Guid.TryParse(shiftProp.GetString(), out var shiftId)
                || !Guid.TryParse(employeeProp.GetString(), out var employeeId)
                || !DateOnly.TryParse(dateProp.GetString(), out var date))
                continue;

            if (!validShiftIds.Contains(shiftId) || !validEmployeeIds.Contains(employeeId))
                continue;

            if (date < validation.WeekStartDate || date > weekEnd)
                continue;

            var key = (shiftId, employeeId, date);
            if (existingKeys.Contains(key))
                continue;

            results.Add(new ScheduleSuggestionDto(Guid.NewGuid(), shiftId, employeeId, date, 100));
            existingKeys.Add(key);
        }

        return results;
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new JsonException("No JSON object found in model response.");

        return text[start..(end + 1)];
    }
}

internal sealed record ScheduleSuggestionValidationContext(
    DateOnly WeekStartDate,
    IReadOnlySet<Guid> EmployeeIds,
    IReadOnlySet<Guid> ShiftIds,
    IReadOnlySet<(Guid ShiftDefinitionId, Guid EmployeeId, DateOnly Date)> ExistingSlotKeys);
