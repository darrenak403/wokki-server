using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IShiftDefinitionRepository
{
    Task<ShiftDefinition?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftDefinition>> ListAsync(
        Guid locationId,
        Guid? departmentId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftDefinition>> ListByDepartmentAsync(
        Guid locationId,
        Guid departmentId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftDefinition>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(ShiftDefinition shift, CancellationToken cancellationToken = default);
    void Update(ShiftDefinition shift);
}
