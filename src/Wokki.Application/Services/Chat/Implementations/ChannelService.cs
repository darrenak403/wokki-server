using System.Text.RegularExpressions;
using Wokki.Application.Dtos.Chat;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;
using ChannelEntity = Wokki.Domain.Entities.Channel;
using ChannelMemberEntity = Wokki.Domain.Entities.ChannelMember;
using MessageEntity = Wokki.Domain.Entities.Message;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Chat.Implementations;

public sealed partial class ChannelService(
    IUnitOfWork unitOfWork,
    IOrgChannelService orgChannelService,
    IChatRealtimeNotifier realtime,
    IOrganizationScopeService organizationScope,
    IOrgAdminEmployeeProvisioner orgAdminEmployeeProvisioner) : IChannelService
{
    private const int MaxBodyLength = 4000;
    private const int MaxOrgMembers = 200;
    private const string DeletedPlaceholder = "[Message deleted]";

    private async Task<Domain.Entities.Employee?> ResolveEmployeeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is not null)
            return employee;

        return await orgAdminEmployeeProvisioner.EnsureByUserIdAsync(userId, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<ChannelResponse>>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await ResolveEmployeeAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<ChannelResponse>>.FailureResponse(AppMessages.Chat.NoEmployeeProfile);

        await orgChannelService.EnsureOrgChannelAsync(employee.OrganizationId, userId, cancellationToken);
        await orgChannelService.EnsureMemberAsync(employee.OrganizationId, employee.Id, cancellationToken);

        var channels = await unitOfWork.Channels.ListByEmployeeAsync(employee.Id, cancellationToken);
        var filtered = channels
            .Where(c => c.Type is ChannelType.Organization or ChannelType.Direct)
            .ToList();

        var latestByChannel = await unitOfWork.Messages.GetLatestCreatedAtByChannelsAsync(
            filtered.Select(c => c.Id),
            cancellationToken);

        var responses = new List<ChannelResponse>(filtered.Count);
        foreach (var channel in filtered)
            responses.Add(await MapChannelAsync(channel, latestByChannel, cancellationToken));

        var ordered = responses
            .OrderByDescending(c => c.Type == ChannelType.Organization)
            .ThenByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToList();

        return ApiResponse<IReadOnlyList<ChannelResponse>>.SuccessResponse(ordered, AppMessages.Chat.Listed);
    }

    public async Task<ApiResponse<IReadOnlyList<OrgChatMemberResponse>>> ListOrgMembersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await ResolveEmployeeAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<OrgChatMemberResponse>>.FailureResponse(AppMessages.Chat.NoEmployeeProfile);

        await orgChannelService.EnsureOrgChannelAsync(employee.OrganizationId, userId, cancellationToken);

        await orgAdminEmployeeProvisioner.RepairOrgAdminMemberAsync(
            employee.OrganizationId,
            employee.UserId,
            employee.Id,
            cancellationToken);

        var orgCreator = await unitOfWork.Users.GetOldestByOrganizationIdAsync(
            employee.OrganizationId,
            cancellationToken);

        var (items, _) = await unitOfWork.Employees.ListAsync(
            page: 1,
            pageSize: MaxOrgMembers,
            organizationId: employee.OrganizationId,
            includeTerminated: false,
            cancellationToken: cancellationToken);

        if (orgCreator is not null)
        {
            var creatorEmployee = items.FirstOrDefault(e => e.UserId == orgCreator.Id);
            if (creatorEmployee is not null)
            {
                await orgAdminEmployeeProvisioner.RepairOrgAdminMemberAsync(
                    employee.OrganizationId,
                    orgCreator.Id,
                    creatorEmployee.Id,
                    cancellationToken);
            }
        }

        var responses = new List<OrgChatMemberResponse>(items.Count);
        foreach (var member in items
                     .Where(m => m.Id != employee.Id)
                     .OrderBy(e => e.LastName)
                     .ThenBy(e => e.FirstName))
        {
            var user = await unitOfWork.Users.GetByIdAsync(member.UserId, cancellationToken: cancellationToken);
            var role = user?.Role ?? RoleConstants.User;
            var isOrgAdmin =
                string.Equals(role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
                || (orgCreator is not null && orgCreator.Id == member.UserId);

            DepartmentEntity? department = null;
            LocationEntity? location = null;
            if (!isOrgAdmin && member.DepartmentId.HasValue)
            {
                department = await unitOfWork.Departments.GetByIdAsync(
                    member.DepartmentId.Value,
                    cancellationToken: cancellationToken);
                if (department is not null)
                {
                    location = await unitOfWork.Locations.GetByIdAsync(
                        department.LocationId,
                        cancellationToken: cancellationToken);
                }
            }

            responses.Add(new OrgChatMemberResponse(
                member.Id,
                member.FirstName,
                member.LastName,
                role,
                isOrgAdmin,
                isOrgAdmin ? null : department?.Name,
                isOrgAdmin ? null : location?.Name));
        }

        return ApiResponse<IReadOnlyList<OrgChatMemberResponse>>.SuccessResponse(
            responses,
            AppMessages.Chat.OrgMembersListed);
    }

    public async Task<ApiResponse<ChannelResponse>> CreateAsync(
        CreateChannelRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Type == ChannelType.Group)
            return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.GroupNotAllowed);

        if (request.Type == ChannelType.Organization)
            return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.Forbidden);

        if (request.MemberEmployeeIds.Count == 0)
            return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.MembersRequired);

        var creator = await unitOfWork.Employees.GetByUserIdAsync(createdByUserId, cancellationToken);
        if (creator is null)
            return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.NoEmployeeProfile);

        var memberIds = request.MemberEmployeeIds.Distinct().ToList();
        if (!memberIds.Contains(creator.Id))
            memberIds.Add(creator.Id);

        foreach (var memberId in memberIds)
        {
            var member = await unitOfWork.Employees.GetByIdAsync(memberId, cancellationToken: cancellationToken);
            if (member is null || member.TerminatedAt is not null
                || !organizationScope.IsSameOrganization(member.OrganizationId))
                return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.MemberNotFound);
        }

        if (request.Type == ChannelType.Direct)
        {
            if (request.MemberEmployeeIds.Any(id => id == creator.Id)
                && request.MemberEmployeeIds.Distinct().Count() <= 1)
                return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.CannotMessageSelf);

            if (memberIds.Count != 2)
                return ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Chat.DirectRequiresTwoMembers);

            var existing = await unitOfWork.Channels.FindDirectChannelAsync(
                memberIds[0],
                memberIds[1],
                cancellationToken);
            if (existing is not null)
                return ApiResponse<ChannelResponse>.SuccessResponse(
                    await MapChannelAsync(existing, cancellationToken: cancellationToken),
                    AppMessages.Chat.Found);
        }

        var channel = new ChannelEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = creator.OrganizationId,
            Name = null,
            Type = ChannelType.Direct,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Channels.AddAsync(channel, cancellationToken);

        var members = memberIds.Select(id => new ChannelMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = creator.OrganizationId,
            ChannelId = channel.Id,
            EmployeeId = id,
            JoinedAt = DateTime.UtcNow
        }).ToList();

        await unitOfWork.Channels.AddMembersAsync(members, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ChannelResponse>.SuccessResponse(
            await MapChannelAsync(channel, cancellationToken: cancellationToken),
            AppMessages.Chat.Created);
    }

    public async Task<ApiResponse<MessageListResponse>> ListMessagesAsync(
        Guid channelId,
        Guid userId,
        string? role,
        DateTime? before,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveMemberAccessAsync(channelId, userId, role, cancellationToken);
        if (!access.Allowed)
            return ApiResponse<MessageListResponse>.FailureResponse(access.Error!);

        var channel = await unitOfWork.Channels.GetByIdAsync(channelId, cancellationToken);
        if (channel is null)
            return ApiResponse<MessageListResponse>.FailureResponse(AppMessages.Chat.ChannelNotFound);

        var (items, hasMore) = await unitOfWork.Messages.ListByChannelAsync(channelId, before, limit, cancellationToken);
        var responses = new List<MessageResponse>(items.Count);
        foreach (var message in items.OrderBy(m => m.CreatedAt))
            responses.Add(await MapMessageAsync(message, cancellationToken));

        DateTime? nextCursor = hasMore && items.Count > 0 ? items.Min(m => m.CreatedAt) : null;
        return ApiResponse<MessageListResponse>.SuccessResponse(
            new MessageListResponse(responses, nextCursor, hasMore),
            AppMessages.Chat.MessagesListed);
    }

    public async Task<ApiResponse<MessageResponse>> SendMessageAsync(
        Guid channelId,
        SendMessageRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var body = SanitizeBody(request.Body);
        if (string.IsNullOrWhiteSpace(body))
            return ApiResponse<MessageResponse>.FailureResponse(AppMessages.Chat.BodyRequired);

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<MessageResponse>.FailureResponse(AppMessages.Chat.NoEmployeeProfile);

        if (!await unitOfWork.Channels.IsMemberAsync(channelId, employee.Id, cancellationToken))
            return ApiResponse<MessageResponse>.FailureResponse(AppMessages.Chat.Forbidden);

        var channel = await unitOfWork.Channels.GetByIdAsync(channelId, cancellationToken);
        if (channel is null || !organizationScope.IsSameOrganization(channel.OrganizationId))
            return ApiResponse<MessageResponse>.FailureResponse(AppMessages.Chat.ChannelNotFound);

        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = channel.OrganizationId,
            ChannelId = channelId,
            SenderId = employee.Id,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Messages.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await MapMessageAsync(message, cancellationToken);

        try
        {
            await realtime.NotifyMessageAsync(channelId, response, cancellationToken);
        }
        catch
        {
            // Real-time delivery must not roll back persisted message.
        }

        return ApiResponse<MessageResponse>.SuccessResponse(response, AppMessages.Chat.MessageSent);
    }

    public async Task<ApiResponse<object>> DeleteMessageAsync(
        Guid channelId,
        Guid messageId,
        Guid userId,
        string? role,
        CancellationToken cancellationToken = default)
    {
        var message = await unitOfWork.Messages.GetByIdAsync(messageId, track: true, cancellationToken: cancellationToken);
        if (message is null || message.ChannelId != channelId)
            return ApiResponse<object>.FailureResponse(AppMessages.Chat.MessageNotFound);

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        var isAdmin = role == Wokki.Domain.Constants.RoleConstants.Admin;
        if (employee is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Chat.NoEmployeeProfile);

        if (!isAdmin && message.SenderId != employee.Id)
            return ApiResponse<object>.FailureResponse(AppMessages.Chat.Forbidden);

        if (!await unitOfWork.Channels.IsMemberAsync(channelId, employee.Id, cancellationToken) && !isAdmin)
            return ApiResponse<object>.FailureResponse(AppMessages.Chat.Forbidden);

        message.DeletedAt = DateTime.UtcNow;
        message.Body = DeletedPlaceholder;
        unitOfWork.Messages.Update(message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Chat.MessageDeleted);
    }

    private async Task<ChannelResponse> MapChannelAsync(
        ChannelEntity channel,
        IReadOnlyDictionary<Guid, DateTime>? latestByChannel = null,
        CancellationToken cancellationToken = default)
    {
        var members = await unitOfWork.Channels.ListMembersAsync(channel.Id, cancellationToken);
        var memberResponses = new List<ChannelMemberResponse>(members.Count);
        foreach (var member in members)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(member.EmployeeId, cancellationToken: cancellationToken);
            if (employee is null)
                continue;

            memberResponses.Add(new ChannelMemberResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                member.JoinedAt));
        }

        DateTime? lastMessageAt = null;
        if (latestByChannel is not null && latestByChannel.TryGetValue(channel.Id, out var latest))
            lastMessageAt = latest;
        else
            lastMessageAt = await GetLatestMessageAtAsync(channel.Id, cancellationToken);

        return new ChannelResponse(
            channel.Id,
            channel.Name,
            channel.Type,
            channel.CreatedBy,
            channel.CreatedAt,
            lastMessageAt,
            memberResponses);
    }

    private async Task<DateTime?> GetLatestMessageAtAsync(Guid channelId, CancellationToken cancellationToken)
    {
        var map = await unitOfWork.Messages.GetLatestCreatedAtByChannelsAsync([channelId], cancellationToken);
        return map.TryGetValue(channelId, out var latest) ? latest : null;
    }

    private async Task<MessageResponse> MapMessageAsync(MessageEntity message, CancellationToken cancellationToken)
    {
        var sender = await unitOfWork.Employees.GetByIdAsync(message.SenderId, cancellationToken: cancellationToken);
        var senderName = sender is null ? "Unknown" : $"{sender.FirstName} {sender.LastName}".Trim();
        var isDeleted = message.DeletedAt is not null;
        return new MessageResponse(
            message.Id,
            message.ChannelId,
            message.SenderId,
            senderName,
            isDeleted ? DeletedPlaceholder : message.Body,
            isDeleted,
            message.CreatedAt);
    }

    private async Task<(bool Allowed, AppMessage? Error)> ResolveMemberAccessAsync(
        Guid channelId,
        Guid userId,
        string? role,
        CancellationToken cancellationToken)
    {
        if (role is Wokki.Domain.Constants.RoleConstants.Admin)
            return (true, null);

        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return (false, AppMessages.Chat.NoEmployeeProfile);

        if (!await unitOfWork.Channels.IsMemberAsync(channelId, employee.Id, cancellationToken))
            return (false, AppMessages.Chat.Forbidden);

        return (true, null);
    }

    private static string SanitizeBody(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length > MaxBodyLength)
            trimmed = trimmed[..MaxBodyLength];

        return HtmlTagRegex().Replace(trimmed, string.Empty);
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
}
