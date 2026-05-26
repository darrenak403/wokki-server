using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface ILocationMembershipRepository
{
    Task<LocationMembership?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<LocationMembership?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingOrActiveAsync(Guid employeeId, Guid locationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationMembership>> ListByLocationAsync(Guid locationId, LocationMembershipStatus? status, CancellationToken cancellationToken = default);
    Task AddAsync(LocationMembership membership, CancellationToken cancellationToken = default);
    void Update(LocationMembership membership);
}
