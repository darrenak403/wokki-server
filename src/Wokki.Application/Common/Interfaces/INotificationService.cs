namespace Wokki.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default);
}
