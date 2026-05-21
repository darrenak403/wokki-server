using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class EmployeeAvailabilityRepository(AppDbContext context) : IEmployeeAvailabilityRepository
{
    public async Task<IReadOnlyList<EmployeeAvailability>> ListByEmployeeIdsAsync(
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.EmployeeAvailabilities.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);
    }
}
