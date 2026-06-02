using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IOrganizationSubscriptionLedgerRepository
{
    Task AddAsync(
        OrganizationSubscriptionLedgerEntry entry,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationSubscriptionLedgerEntry> Items, int TotalCount)> ListAsync(
        Guid? organizationId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
