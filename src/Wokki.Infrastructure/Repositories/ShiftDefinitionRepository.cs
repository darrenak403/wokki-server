using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ShiftDefinitionRepository(AppDbContext context) : IShiftDefinitionRepository
{
    public async Task<ShiftDefinition?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.ShiftDefinitions : context.ShiftDefinitions.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftDefinition>> ListAsync(
        Guid locationId,
        Guid? departmentId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = context.ShiftDefinitions.AsNoTracking()
            .Where(s => s.LocationId == locationId);

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == null || s.DepartmentId == departmentId.Value);

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftDefinition>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await context.ShiftDefinitions.AsNoTracking()
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShiftDefinition shift, CancellationToken cancellationToken = default) =>
        await context.ShiftDefinitions.AddAsync(shift, cancellationToken);

    public void Update(ShiftDefinition shift) => context.ShiftDefinitions.Update(shift);
}
