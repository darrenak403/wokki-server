using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Dtos.Schedule;

/// <summary>
/// <paramref name="Hint"/> is request-scoped only — applies to this single Suggest call and is never
/// persisted to <c>OrganizationSchedulingPolicy</c> or any other store.
/// </summary>
public sealed record SuggestScheduleRequest(bool UseAi = false, ScheduleSuggestionHint? Hint = null);
