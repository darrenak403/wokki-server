using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Common.Interfaces;

public interface IScheduleSuggestionOrchestrator
{
    Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        bool useAi,
        ScheduleSuggestionHint? hint = null,
        CancellationToken cancellationToken = default);
}
