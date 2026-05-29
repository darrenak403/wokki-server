using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<Schedule?> GetByDepartmentAndWeekAsync(
        Guid departmentId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Schedule> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? departmentId = null,
        DateOnly? weekStartDate = null,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default);
    void Update(Schedule schedule);
    void Remove(Schedule schedule);
}
