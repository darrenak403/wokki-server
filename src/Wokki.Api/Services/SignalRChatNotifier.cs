using Microsoft.AspNetCore.SignalR;
using Wokki.Api.Hubs;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Api.Services;

public sealed class SignalRChatNotifier(IHubContext<ChatHub> hubContext) : IChatRealtimeNotifier
{
    public Task NotifyMessageAsync(Guid channelId, object payload, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(channelId.ToString())
            .SendAsync("ReceiveMessage", payload, cancellationToken);
}
