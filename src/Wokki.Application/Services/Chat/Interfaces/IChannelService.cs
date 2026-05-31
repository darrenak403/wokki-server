using Wokki.Application.Dtos.Chat;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Chat.Interfaces;

public interface IChannelService
{
    Task<ApiResponse<IReadOnlyList<ChannelResponse>>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<OrgChatMemberResponse>>> ListOrgMembersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ChannelResponse>> CreateAsync(
        CreateChannelRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<MessageListResponse>> ListMessagesAsync(
        Guid channelId,
        Guid userId,
        string? role,
        DateTime? before,
        int limit,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<MessageResponse>> SendMessageAsync(
        Guid channelId,
        SendMessageRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteMessageAsync(
        Guid channelId,
        Guid messageId,
        Guid userId,
        string? role,
        CancellationToken cancellationToken = default);
}
