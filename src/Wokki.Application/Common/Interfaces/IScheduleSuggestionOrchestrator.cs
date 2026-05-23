namespace Wokki.Application.Common.Interfaces;

public interface IScheduleSuggestionOrchestrator
{
    Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        bool useAi,
        CancellationToken cancellationToken = default);
}
