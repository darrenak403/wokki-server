namespace Wokki.Application.Services.Platform.Interfaces;

public interface IPlatformActivityRecorder
{
    Task TryRecordAsync(
        Guid organizationId,
        Guid? userId,
        string eventType,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken cancellationToken = default);
}
