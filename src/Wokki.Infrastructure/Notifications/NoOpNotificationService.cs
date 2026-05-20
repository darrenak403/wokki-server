using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Notifications;

public sealed class NoOpNotificationService(ILogger<NoOpNotificationService> logger) : INotificationService
{
    public Task SendAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification {EventName} for user {UserId}: {@Payload}",
            eventName,
            userId,
            payload);
        return Task.CompletedTask;
    }
}
