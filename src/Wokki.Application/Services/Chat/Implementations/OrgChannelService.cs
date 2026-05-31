using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Chat.Implementations;

public sealed class OrgChannelService(IUnitOfWork unitOfWork) : IOrgChannelService
{
    private const int MemberSyncPageSize = 500;
    private const string DefaultOrgChannelName = "Toàn công ty";

    public async Task<Guid> EnsureOrgChannelAsync(
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await unitOfWork.Channels.FindOrganizationChannelAsync(organizationId, cancellationToken);
        if (existing is not null)
        {
            await SyncAllActiveMembersAsync(existing.Id, organizationId, cancellationToken);
            return existing.Id;
        }

        var org = await unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken: cancellationToken);
        var channelName = string.IsNullOrWhiteSpace(org?.Name) ? DefaultOrgChannelName : org!.Name.Trim();

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = channelName,
            Type = ChannelType.Organization,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Channels.AddAsync(channel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncAllActiveMembersAsync(channel.Id, organizationId, cancellationToken);
        return channel.Id;
    }

    public async Task EnsureMemberAsync(
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var channelId = await GetOrgChannelIdAsync(organizationId, cancellationToken);
        if (channelId is null)
            return;

        if (await unitOfWork.Channels.IsMemberAsync(channelId.Value, employeeId, cancellationToken))
            return;

        await unitOfWork.Channels.AddMemberAsync(new ChannelMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ChannelId = channelId.Value,
            EmployeeId = employeeId,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var channelId = await GetOrgChannelIdAsync(organizationId, cancellationToken);
        if (channelId is null)
            return;

        await unitOfWork.Channels.RemoveMemberAsync(channelId.Value, employeeId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetOrgChannelIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var channel = await unitOfWork.Channels.FindOrganizationChannelAsync(organizationId, cancellationToken);
        return channel?.Id;
    }

    private async Task SyncAllActiveMembersAsync(
        Guid channelId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var (employees, _) = await unitOfWork.Employees.ListAsync(
            page: 1,
            pageSize: MemberSyncPageSize,
            organizationId: organizationId,
            includeTerminated: false,
            cancellationToken: cancellationToken);

        var existingMembers = await unitOfWork.Channels.ListMembersAsync(channelId, cancellationToken);
        var existingIds = existingMembers.Select(m => m.EmployeeId).ToHashSet();
        var toAdd = employees
            .Where(e => !existingIds.Contains(e.Id))
            .Select(e => new ChannelMember
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ChannelId = channelId,
                EmployeeId = e.Id,
                JoinedAt = DateTime.UtcNow
            })
            .ToList();

        if (toAdd.Count == 0)
            return;

        await unitOfWork.Channels.AddMembersAsync(toAdd, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
