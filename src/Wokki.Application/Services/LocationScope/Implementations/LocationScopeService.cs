using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationScope.Implementations;

public sealed class LocationScopeService(IUnitOfWork unitOfWork) : ILocationScopeService
{
    public async Task<IReadOnlySet<Guid>?> GetManagedLocationIdsAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return null;
        if (role != RoleConstants.Manager) return new HashSet<Guid>();
        var rows = await unitOfWork.LocationManagers.GetByUserAsync(userId, ct);
        return rows.Select(r => r.LocationId).ToHashSet();
    }

    public async Task<bool> CanManageLocationAsync(Guid userId, string role, Guid locationId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var ids = await GetManagedLocationIdsAsync(userId, role, ct);
        return ids?.Contains(locationId) ?? false;
    }

    public async Task<bool> CanManageScheduleAsync(Guid userId, string role, Guid scheduleId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: ct);
        if (schedule is null) return true; // 404 handled by service
        var dept = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
        if (dept is null) return true; // 404 handled by service
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }

    public async Task<bool> CanManageDepartmentAsync(Guid userId, string role, Guid departmentId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var dept = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: ct);
        if (dept is null) return true; // 404 handled by service
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }
}
