using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IPayPeriodRepository
{
    Task<PayPeriod?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<PayPeriod?> GetByDepartmentAndStartAsync(
        Guid departmentId,
        DateOnly startDate,
        CancellationToken cancellationToken = default);
    Task<PayPeriod?> GetContainingDateAsync(
        Guid departmentId,
        DateOnly date,
        CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingAsync(
        Guid departmentId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PayPeriod payPeriod, CancellationToken cancellationToken = default);
    void Update(PayPeriod payPeriod);
}
