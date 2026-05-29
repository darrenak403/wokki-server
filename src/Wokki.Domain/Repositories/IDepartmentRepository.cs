using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> ListAsync(Guid? locationId = null, bool activeOnly = true, IReadOnlySet<Guid>? locationIds = null, CancellationToken cancellationToken = default);
    Task AddAsync(Department department, CancellationToken cancellationToken = default);
    void Update(Department department);
}
