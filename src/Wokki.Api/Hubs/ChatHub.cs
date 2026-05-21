using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Wokki.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public async Task JoinChannel(string channelId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);

    public async Task LeaveChannel(string channelId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
}
