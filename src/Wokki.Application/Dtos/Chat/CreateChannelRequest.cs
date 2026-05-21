using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Chat;

public sealed record CreateChannelRequest(
    ChannelType Type,
    string? Name,
    IReadOnlyList<Guid> MemberEmployeeIds);
