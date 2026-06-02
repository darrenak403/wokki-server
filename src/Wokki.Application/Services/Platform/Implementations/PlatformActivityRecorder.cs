using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformActivityRecorder(IUnitOfWork unitOfWork) : IPlatformActivityRecorder
{
    public async Task TryRecordAsync(
        Guid organizationId,
        Guid? userId,
        string eventType,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.PlatformActivityEvents.AddAsync(
                new PlatformActivityEvent
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = userId,
                    EventType = eventType,
                    OccurredAt = DateTime.UtcNow,
                    EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType,
                    EntityId = entityId
                },
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Platform analytics must never roll back normal org workflows.
        }
    }
}
