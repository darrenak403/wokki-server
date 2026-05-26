namespace Wokki.Application.Services.LocationScope.Interfaces;

public interface ILocationScopeService
{
    // Admin → null (bypass sentinel, do not load all IDs into memory).
    // Manager → set of LocationIds they manage.
    // Other → empty set.
    Task<IReadOnlySet<Guid>?> GetManagedLocationIdsAsync(Guid userId, string role, CancellationToken ct = default);

    Task<bool> CanManageLocationAsync(Guid userId, string role, Guid locationId, CancellationToken ct = default);

    // Resolves: Schedule → DepartmentId → Department → LocationId
    Task<bool> CanManageScheduleAsync(Guid userId, string role, Guid scheduleId, CancellationToken ct = default);

    // Resolves: Department → LocationId
    Task<bool> CanManageDepartmentAsync(Guid userId, string role, Guid departmentId, CancellationToken ct = default);
}
