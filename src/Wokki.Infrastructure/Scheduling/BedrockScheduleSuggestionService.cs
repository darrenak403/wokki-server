using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Bedrock.Interfaces;

namespace Wokki.Infrastructure.Scheduling;

public sealed class BedrockScheduleSuggestionService(
    ScheduleSuggestionContextLoader contextLoader,
    ScheduleSuggestionPromptBuilder promptBuilder,
    IBedrockService bedrockService,
    ILogger<BedrockScheduleSuggestionService> logger) : IScheduleSuggestionService
{
    /// <summary>Legacy provider, not wired into <see cref="ScheduleSuggestionOrchestrator"/> (CP-SAT only) — hints are not supported here.</summary>
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        ScheduleSuggestionHint? hint = null,
        CancellationToken cancellationToken = default)
    {
        var (context, reason) = await contextLoader.LoadAsync(scheduleId, cancellationToken);
        if (context is null)
            return new ScheduleSuggestionGenerationResult([], reason, Provider: "bedrock");

        if (context.Employees.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_employees", Provider: "bedrock");

        if (context.Shifts.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "no_shifts", Provider: "bedrock");

        var solverPolicy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(context.OrganizationSchedulingPolicy);
        if (solverPolicy.RequireSubmittedPreferences && context.SubmittedPreferences.Count == 0)
            return new ScheduleSuggestionGenerationResult([], "missing_preferences", Provider: "bedrock");

        try
        {
            var prompt = promptBuilder.Build(context);
            var result = await bedrockService.ConverseAsync(
                prompt,
                new BedrockConverseOptions(MaxTokens: 2000, Temperature: 0, TimeoutSeconds: 30),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result.Text))
                return new ScheduleSuggestionGenerationResult([], null, Provider: "bedrock");

            var suggestions = ParseSuggestions(result.Text, context);
            return new ScheduleSuggestionGenerationResult(suggestions, null, Provider: "bedrock");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bedrock suggestion failed for schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    private List<ScheduleSuggestionDto> ParseSuggestions(string text, ScheduleSuggestionContext context)
    {
        var shiftIds = context.Shifts.Select(s => s.Id).ToHashSet();
        var employeeIds = context.Employees.Select(e => e.Id).ToHashSet();
        var weekStart = context.Schedule.WeekStartDate;
        var weekEnd = weekStart.AddDays(6);

        List<JsonElement> items;
        try
        {
            items = JsonSerializer.Deserialize<List<JsonElement>>(text) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse Bedrock JSON response for schedule {ScheduleId}",
                context.Schedule.Id);
            return [];
        }

        var valid = new List<ScheduleSuggestionDto>();
        var dropped = 0;

        foreach (var item in items)
        {
            try
            {
                if (!item.TryGetProperty("shiftDefinitionId", out var shiftProp)
                    || !item.TryGetProperty("employeeId", out var empProp)
                    || !item.TryGetProperty("date", out var dateProp))
                {
                    dropped++;
                    continue;
                }

                if (!Guid.TryParse(shiftProp.GetString(), out var shiftId)
                    || !Guid.TryParse(empProp.GetString(), out var empId)
                    || !DateOnly.TryParse(dateProp.GetString(), out var date))
                {
                    dropped++;
                    continue;
                }

                if (!shiftIds.Contains(shiftId) || !employeeIds.Contains(empId)
                    || date < weekStart || date > weekEnd)
                {
                    dropped++;
                    continue;
                }

                valid.Add(new ScheduleSuggestionDto(Guid.NewGuid(), shiftId, empId, date, Score: 100));
            }
            catch
            {
                dropped++;
            }
        }

        if (dropped > 0)
            logger.LogInformation(
                "Dropped {Dropped}/{Total} invalid items from Bedrock response for schedule {ScheduleId}",
                dropped, items.Count, context.Schedule.Id);

        return valid;
    }
}
