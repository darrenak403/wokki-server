using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class DepartmentRepository(AppDbContext context) : IDepartmentRepository
{
    public async Task<Department?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Departments : context.Departments.AsNoTracking();
        return await query.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Department>> ListAsync(
        Guid? organizationId = null,
        Guid? locationId = null,
        bool activeOnly = true,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Departments.AsNoTracking().AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(d => d.OrganizationId == organizationId.Value);

        if (locationId.HasValue)
            query = query.Where(d => d.LocationId == locationId.Value);

        if (locationIds is not null)
        {
            var allowedLocationIds = locationIds.ToArray();
            query = allowedLocationIds.Length == 0
                ? query.Where(_ => false)
                : query.Where(d => allowedLocationIds.Contains(d.LocationId));
        }

        if (activeOnly)
            query = query.Where(d => d.IsActive);

        return await query.OrderBy(d => d.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default) =>
        await context.Departments.AddAsync(department, cancellationToken);

    public void Update(Department department) => context.Departments.Update(department);
}
