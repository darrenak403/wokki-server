using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Scheduling;

public sealed class ScheduleSuggestionOrchestrator(
    HeuristicScheduleSuggestionService heuristic,
    BedrockScheduleSuggestionService bedrock) : IScheduleSuggestionOrchestrator
{
    public Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        bool useAi,
        CancellationToken cancellationToken = default) =>
        useAi
            ? bedrock.GenerateAsync(scheduleId, cancellationToken)
            : heuristic.GenerateAsync(scheduleId, cancellationToken);
}
