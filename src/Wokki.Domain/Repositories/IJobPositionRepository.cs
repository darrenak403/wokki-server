using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IJobPositionRepository
{
    Task<IReadOnlyList<JobPosition>> ListByDepartmentAsync(
        Guid departmentId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<JobPosition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(JobPosition entity, CancellationToken cancellationToken = default);
    void Update(JobPosition entity);
    void Remove(JobPosition entity);
}
