using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? departmentId = null,
        Guid? locationId = null,
        bool includeTerminated = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfDepartmentAsync(Guid employeeId, Guid departmentId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);
}
