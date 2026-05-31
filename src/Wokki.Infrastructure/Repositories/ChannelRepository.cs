using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ChannelRepository(AppDbContext context) : IChannelRepository
{
    public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Channels.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Channel>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await (
            from c in context.Channels.AsNoTracking()
            join m in context.ChannelMembers.AsNoTracking() on c.Id equals m.ChannelId
            where m.EmployeeId == employeeId
            orderby c.CreatedAt descending
            select c
        ).ToListAsync(cancellationToken);

    public async Task<Channel?> FindOrganizationChannelAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.Channels.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId && c.Type == ChannelType.Organization,
                cancellationToken);

    public async Task<Channel?> FindDirectChannelAsync(
        Guid employeeIdA,
        Guid employeeIdB,
        CancellationToken cancellationToken = default)
    {
        var channelIdsA = context.ChannelMembers.AsNoTracking()
            .Where(m => m.EmployeeId == employeeIdA)
            .Select(m => m.ChannelId);

        return await (
            from c in context.Channels.AsNoTracking()
            join m in context.ChannelMembers.AsNoTracking() on c.Id equals m.ChannelId
            where c.Type == ChannelType.Direct
                  && channelIdsA.Contains(c.Id)
                  && m.EmployeeId == employeeIdB
            select c
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsMemberAsync(
        Guid channelId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await context.ChannelMembers.AsNoTracking()
            .AnyAsync(m => m.ChannelId == channelId && m.EmployeeId == employeeId, cancellationToken);

    public async Task RemoveMemberAsync(
        Guid channelId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var member = await context.ChannelMembers
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.EmployeeId == employeeId, cancellationToken);
        if (member is not null)
            context.ChannelMembers.Remove(member);
    }

    public async Task<IReadOnlyList<ChannelMember>> ListMembersAsync(
        Guid channelId,
        CancellationToken cancellationToken = default) =>
        await context.ChannelMembers.AsNoTracking()
            .Where(m => m.ChannelId == channelId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Channel channel, CancellationToken cancellationToken = default) =>
        await context.Channels.AddAsync(channel, cancellationToken);

    public async Task AddMemberAsync(ChannelMember member, CancellationToken cancellationToken = default) =>
        await context.ChannelMembers.AddAsync(member, cancellationToken);

    public async Task AddMembersAsync(IEnumerable<ChannelMember> members, CancellationToken cancellationToken = default) =>
        await context.ChannelMembers.AddRangeAsync(members, cancellationToken);
}
