using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Users : context.Users.AsNoTracking();
        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().AsQueryable();
        if (organizationId.HasValue)
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        query = query.OrderByDescending(u => u.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListWithoutEmployeeAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Where(u => !context.Employees.Any(e => e.UserId == u.Id));
        if (organizationId.HasValue)
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        query = query.OrderByDescending(u => u.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<PlatformUserSnapshot> Items, int TotalCount)> ListPlatformAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in context.Users.AsNoTracking()
            join organization in context.Organizations.AsNoTracking()
                on user.OrganizationId equals organization.Id into organizationJoin
            from organization in organizationJoin.DefaultIfEmpty()
            select new
            {
                User = user,
                OrganizationName = organization == null ? null : organization.Name
            };

        if (organizationId.HasValue)
            query = query.Where(x => x.User.OrganizationId == organizationId.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(x => x.User.Role.ToLower() == normalizedRole);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.User.Email.ToLower().Contains(term) ||
                (x.OrganizationName != null && x.OrganizationName.ToLower().Contains(term)));
        }

        query = query.OrderByDescending(x => x.User.CreatedAt);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PlatformUserSnapshot(
                x.User.Id,
                x.User.Email,
                x.User.Role,
                x.User.OrganizationId,
                x.OrganizationName,
                x.User.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => context.Users.Update(user);

    public void Remove(User user) => context.Users.Remove(user);
}
