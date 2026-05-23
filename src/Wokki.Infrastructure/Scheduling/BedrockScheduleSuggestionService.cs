using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Infrastructure.Bedrock;

namespace Wokki.Infrastructure.Scheduling;

public sealed class BedrockScheduleSuggestionService(
    HeuristicScheduleSuggestionService heuristic,
    ScheduleSuggestionContextLoader contextLoader,
    IBedrockService bedrockService,
    IOptions<BedrockSettings> bedrockOptions,
    ILogger<BedrockScheduleSuggestionService> logger) : IScheduleSuggestionService
{
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var (context, reason) = await contextLoader.LoadAsync(scheduleId, cancellationToken);
        if (context is null)
            return new ScheduleSuggestionGenerationResult([], reason, "bedrock", false);

        try
        {
            var bedrock = bedrockOptions.Value;
            var prompt = ScheduleSuggestionPromptBuilder.BuildUserPrompt(context);
            var response = await bedrockService.ConverseAsync(
                prompt,
                new BedrockConverseOptions(bedrock.MaxTokens, 0.2f, bedrock.TimeoutSeconds),
                cancellationToken);

            if (string.Equals(response.StopReason, "max_tokens", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Bedrock suggest truncated (max_tokens) for schedule {ScheduleId}", scheduleId);
                return await FallbackAsync(scheduleId, "bedrock_truncated", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                logger.LogWarning("Bedrock suggest empty output for schedule {ScheduleId}", scheduleId);
                return await FallbackAsync(scheduleId, "bedrock_empty", cancellationToken);
            }

            var validation = BuildValidationContext(context);
            var suggestions = BedrockSuggestionParser.ParseAndValidate(response.Text, validation);
            if (suggestions.Count == 0)
            {
                logger.LogWarning("Bedrock suggest produced no valid rows for schedule {ScheduleId}", scheduleId);
                return await FallbackAsync(scheduleId, "bedrock_invalid_ids", cancellationToken);
            }

            if (response.InputTokens is not null || response.OutputTokens is not null)
            {
                logger.LogInformation(
                    "Bedrock suggest schedule {ScheduleId}: inputTokens={Input}, outputTokens={Output}",
                    scheduleId,
                    response.InputTokens,
                    response.OutputTokens);
            }

            return new ScheduleSuggestionGenerationResult(suggestions, null, "bedrock", false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bedrock suggest failed for schedule {ScheduleId}", scheduleId);
            return await FallbackAsync(scheduleId, "bedrock_error", cancellationToken);
        }
    }

    private async Task<ScheduleSuggestionGenerationResult> FallbackAsync(
        Guid scheduleId,
        string trigger,
        CancellationToken cancellationToken)
    {
        var fallback = await heuristic.GenerateAsync(scheduleId, cancellationToken);
        return fallback with { Provider = "heuristic", FallbackUsed = true, Reason = fallback.Reason ?? trigger };
    }

    private static ScheduleSuggestionValidationContext BuildValidationContext(ScheduleSuggestionContext context)
    {
        var existingKeys = context.ExistingAssignments
            .Select(a => (a.ShiftDefinitionId, a.EmployeeId, a.Date))
            .ToHashSet();

        return new ScheduleSuggestionValidationContext(
            context.Schedule.WeekStartDate,
            context.Employees.Select(e => e.Id).ToHashSet(),
            context.Shifts.Select(s => s.Id).ToHashSet(),
            existingKeys);
    }
}
