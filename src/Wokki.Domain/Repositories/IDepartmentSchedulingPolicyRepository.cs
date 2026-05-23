using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IDepartmentSchedulingPolicyRepository
{
    Task<DepartmentSchedulingPolicy?> GetByDepartmentIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
    Task AddAsync(DepartmentSchedulingPolicy entity, CancellationToken cancellationToken = default);
    void Update(DepartmentSchedulingPolicy entity);
}
