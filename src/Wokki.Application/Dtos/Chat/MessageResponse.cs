namespace Wokki.Application.Dtos.Chat;

public sealed record MessageResponse(
    Guid Id,
    Guid ChannelId,
    Guid SenderId,
    string SenderName,
    string Body,
    bool IsDeleted,
    DateTime CreatedAt);

public sealed record MessageListResponse(
    IReadOnlyList<MessageResponse> Items,
    DateTime? NextCursor,
    bool HasMore);
