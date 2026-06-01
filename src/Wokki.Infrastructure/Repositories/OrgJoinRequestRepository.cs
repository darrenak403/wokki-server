using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OrgJoinRequestRepository(AppDbContext context) : IOrgJoinRequestRepository
{
    public Task<OrgJoinRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = context.OrgJoinRequests.AsQueryable();
        if (!track)
            query = query.AsNoTracking();

        return query
            .Include(x => x.User)
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<OrgJoinRequest?> GetPendingByUserIdAsync(Guid userId, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = context.OrgJoinRequests.AsQueryable();
        if (!track)
            query = query.AsNoTracking();

        return query
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.Status == OrgJoinRequestStatus.Pending,
                cancellationToken);
    }

    public Task<OrgJoinRequest?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.OrgJoinRequests
            .AsNoTracking()
            .Include(x => x.Organization)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrgJoinRequest>> ListPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await context.OrgJoinRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.OrganizationId == organizationId && x.Status == OrgJoinRequestStatus.Pending)
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrgJoinRequest>> ListPendingForExpiryCheckAsync(
        CancellationToken cancellationToken = default) =>
        await context.OrgJoinRequests
            .Where(x => x.Status == OrgJoinRequestStatus.Pending)
            .Include(x => x.Organization)
            .ToListAsync(cancellationToken);

    public Task AddAsync(OrgJoinRequest request, CancellationToken cancellationToken = default) =>
        context.OrgJoinRequests.AddAsync(request, cancellationToken).AsTask();

    public void Update(OrgJoinRequest request) => context.OrgJoinRequests.Update(request);
}
