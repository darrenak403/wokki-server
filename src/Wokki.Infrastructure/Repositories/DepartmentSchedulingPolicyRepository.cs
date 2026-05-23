using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class DepartmentSchedulingPolicyRepository(AppDbContext context) : IDepartmentSchedulingPolicyRepository
{
    public Task<DepartmentSchedulingPolicy?> GetByDepartmentIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        context.DepartmentSchedulingPolicies.FirstOrDefaultAsync(p => p.DepartmentId == departmentId, cancellationToken);

    public Task AddAsync(DepartmentSchedulingPolicy entity, CancellationToken cancellationToken = default) =>
        context.DepartmentSchedulingPolicies.AddAsync(entity, cancellationToken).AsTask();

    public void Update(DepartmentSchedulingPolicy entity) =>
        context.DepartmentSchedulingPolicies.Update(entity);
}
