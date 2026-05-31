using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Channel>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Channel?> FindOrganizationChannelAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Channel?> FindDirectChannelAsync(
        Guid employeeIdA,
        Guid employeeIdB,
        CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid channelId, Guid employeeId, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid channelId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChannelMember>> ListMembersAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task AddAsync(Channel channel, CancellationToken cancellationToken = default);
    Task AddMemberAsync(ChannelMember member, CancellationToken cancellationToken = default);
    Task AddMembersAsync(IEnumerable<ChannelMember> members, CancellationToken cancellationToken = default);
}
