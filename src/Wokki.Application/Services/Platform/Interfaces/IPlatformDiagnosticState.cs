using Wokki.Application.Dtos.Platform;

namespace Wokki.Application.Services.Platform.Interfaces;

public interface IPlatformDiagnosticState
{
    PlatformDiagnosticFailureResponse GetLastFailure(string componentName);

    void RecordFailure(string componentName, string? code, string? message, DateTime occurredAtUtc);
}
