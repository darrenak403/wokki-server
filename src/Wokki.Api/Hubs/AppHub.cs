using Microsoft.AspNetCore.SignalR;

namespace Wokki.Api.Hubs;

/// <summary>
/// Compatibility hub for legacy app-level realtime clients.
/// The server currently pushes chat messages through /ws/chat only.
/// </summary>
public sealed class AppHub : Hub
{
}
