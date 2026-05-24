using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Scheduling;

public sealed class ScheduleSuggestionOrchestrator(
    HeuristicScheduleSuggestionService heuristic) : IScheduleSuggestionOrchestrator
{
    public Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        bool useAi,
        CancellationToken cancellationToken = default) =>
        heuristic.GenerateAsync(scheduleId, cancellationToken);
}
