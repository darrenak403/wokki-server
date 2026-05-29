using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IEmployeeDepartmentMembershipRepository
{
    Task<IReadOnlyList<EmployeeDepartmentMembership>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid employeeId,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<EmployeeDepartmentMembership?> GetByEmployeeAndDepartmentAsync(
        Guid employeeId,
        Guid departmentId,
        bool track = false,
        CancellationToken cancellationToken = default);

    Task<EmployeeDepartmentMembership?> GetActivePrimaryByEmployeeAsync(
        Guid employeeId,
        bool track = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmployeeDepartmentMembership membership, CancellationToken cancellationToken = default);
    void Update(EmployeeDepartmentMembership membership);

    Task ReplaceForEmployeeAsync(
        Guid employeeId,
        Guid organizationId,
        IReadOnlyList<Guid> departmentIds,
        Guid primaryDepartmentId,
        CancellationToken cancellationToken = default);
}
