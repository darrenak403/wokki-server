using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface IOrgJoinRequestRepository
{
    Task<OrgJoinRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<OrgJoinRequest?> GetPendingByUserIdAsync(Guid userId, bool track = false, CancellationToken cancellationToken = default);
    Task<OrgJoinRequest?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrgJoinRequest>> ListPendingByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrgJoinRequest>> ListPendingForExpiryCheckAsync(CancellationToken cancellationToken = default);
    Task AddAsync(OrgJoinRequest request, CancellationToken cancellationToken = default);
    void Update(OrgJoinRequest request);
}
