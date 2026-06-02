using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OrganizationSubscriptionLedgerRepository(AppDbContext context)
    : IOrganizationSubscriptionLedgerRepository
{
    public async Task AddAsync(
        OrganizationSubscriptionLedgerEntry entry,
        CancellationToken cancellationToken = default) =>
        await context.OrganizationSubscriptionLedgerEntries.AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<OrganizationSubscriptionLedgerEntry> Items, int TotalCount)> ListAsync(
        Guid? organizationId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = context.OrganizationSubscriptionLedgerEntries.AsNoTracking().AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim();
            query = query.Where(x => x.Action == normalizedAction);
        }

        if (from.HasValue)
            query = query.Where(x => x.ChangedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.ChangedAt <= to.Value);

        query = query.OrderByDescending(x => x.ChangedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
