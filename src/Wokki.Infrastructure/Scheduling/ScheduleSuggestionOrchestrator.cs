using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Scheduling;

public sealed class ScheduleSuggestionOrchestrator(
    CpSatScheduleSuggestionService cpSat,
    BedrockScheduleSuggestionService bedrock,
    ILogger<ScheduleSuggestionOrchestrator> logger) : IScheduleSuggestionOrchestrator
{
    public async Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        bool useAi,
        CancellationToken cancellationToken = default)
    {
        if (!useAi)
            return await cpSat.GenerateAsync(scheduleId, cancellationToken);

        try
        {
            var result = await bedrock.GenerateAsync(scheduleId, cancellationToken);
            if (result.Suggestions.Count > 0)
                return result;

            logger.LogWarning(
                "Bedrock returned 0 valid suggestions for schedule {ScheduleId}; falling back to CP-SAT",
                scheduleId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Bedrock suggestion failed for schedule {ScheduleId}; falling back to CP-SAT",
                scheduleId);
        }

        var fallback = await cpSat.GenerateAsync(scheduleId, cancellationToken);
        return fallback with { Provider = "cpsat-fallback", FallbackUsed = true };
    }
}
