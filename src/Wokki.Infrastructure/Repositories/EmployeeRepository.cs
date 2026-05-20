using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Employees : context.Employees.AsNoTracking();
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? departmentId = null,
        Guid? locationId = null,
        bool includeTerminated = false,
        CancellationToken cancellationToken = default)
    {
        var query = context.Employees.AsNoTracking().AsQueryable();

        if (!includeTerminated)
            query = query.Where(e => e.TerminatedAt == null);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (locationId.HasValue)
        {
            query = query.Where(e =>
                context.Departments.Any(d => d.Id == e.DepartmentId && d.LocationId == locationId.Value));
        }

        query = query.OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await context.Employees.AddAsync(employee, cancellationToken);

    public void Update(Employee employee) => context.Employees.Update(employee);
}
