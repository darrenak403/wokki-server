using DepartmentEntity = Wokki.Domain.Entities.Department;

namespace Wokki.Application.Services.Employee.Interfaces;

public interface IStaffPlacementCoordinator
{
    Task AssignManagerLocationsAsync(
        Guid userId,
        Guid organizationId,
        IReadOnlyList<Guid> locationIds,
        CancellationToken cancellationToken = default);

    Task EnsureActiveLocationMembershipAsync(
        Guid employeeId,
        Guid organizationId,
        Guid locationId,
        CancellationToken cancellationToken = default);

    Task EndActiveLocationMembershipAsync(
        Guid employeeId,
        Guid? reviewedById,
        CancellationToken cancellationToken = default);

    Task ClearDepartmentMembershipsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task ApplyEmployeeDepartmentAsync(
        Guid employeeId,
        Guid organizationId,
        DepartmentEntity department,
        CancellationToken cancellationToken = default);

    Task RemoveAllLocationManagersForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
