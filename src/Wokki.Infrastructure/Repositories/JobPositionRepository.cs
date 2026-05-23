using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class JobPositionRepository(AppDbContext context) : IJobPositionRepository
{
    public async Task<IReadOnlyList<JobPosition>> ListByDepartmentAsync(
        Guid departmentId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = context.JobPositions.AsNoTracking().Where(p => p.DepartmentId == departmentId);
        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<JobPosition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.JobPositions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task AddAsync(JobPosition entity, CancellationToken cancellationToken = default) =>
        context.JobPositions.AddAsync(entity, cancellationToken).AsTask();

    public void Update(JobPosition entity) => context.JobPositions.Update(entity);

    public void Remove(JobPosition entity) => context.JobPositions.Remove(entity);
}
