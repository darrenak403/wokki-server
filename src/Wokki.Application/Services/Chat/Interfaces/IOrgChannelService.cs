namespace Wokki.Application.Services.Chat.Interfaces;

public interface IOrgChannelService
{
    Task<Guid> EnsureOrgChannelAsync(Guid organizationId, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task EnsureMemberAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<Guid?> GetOrgChannelIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
