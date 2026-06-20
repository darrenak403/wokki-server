namespace Wokki.Application.Dtos.Chat;

public sealed record UnreadCountResponse(
    int Total,
    IReadOnlyList<ChannelUnreadCountResponse> Channels);

public sealed record ChannelUnreadCountResponse(
    Guid ChannelId,
    int Count);
