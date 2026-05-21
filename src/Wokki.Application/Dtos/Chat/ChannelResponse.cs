using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Chat;

public sealed record ChannelResponse(
    Guid Id,
    string? Name,
    ChannelType Type,
    Guid CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<ChannelMemberResponse> Members);

public sealed record ChannelMemberResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    DateTime JoinedAt);
