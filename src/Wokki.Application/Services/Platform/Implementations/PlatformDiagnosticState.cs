using System.Collections.Concurrent;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.Platform.Interfaces;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformDiagnosticState : IPlatformDiagnosticState
{
    private readonly ConcurrentDictionary<string, PlatformDiagnosticFailureResponse> _failures =
        new(StringComparer.OrdinalIgnoreCase);

    public PlatformDiagnosticFailureResponse GetLastFailure(string componentName) =>
        _failures.TryGetValue(componentName, out var failure)
            ? failure
            : new PlatformDiagnosticFailureResponse(null, null, null);

    public void RecordFailure(string componentName, string? code, string? message, DateTime occurredAtUtc) =>
        _failures[componentName] = new PlatformDiagnosticFailureResponse(
            occurredAtUtc,
            code,
            string.IsNullOrWhiteSpace(message) ? null : message);
}
