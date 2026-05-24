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

    Task ReplaceForEmployeeAsync(
        Guid employeeId,
        IReadOnlyList<Guid> departmentIds,
        Guid primaryDepartmentId,
        CancellationToken cancellationToken = default);
}
