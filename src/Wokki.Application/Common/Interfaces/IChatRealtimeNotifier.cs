namespace Wokki.Application.Common.Interfaces;

public interface IChatRealtimeNotifier
{
    Task NotifyMessageAsync(Guid channelId, object payload, CancellationToken cancellationToken = default);
}
