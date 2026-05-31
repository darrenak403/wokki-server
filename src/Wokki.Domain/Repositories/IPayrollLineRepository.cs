using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IPayrollLineRepository
{
    Task<IReadOnlyList<PayrollLine>> ListByPayPeriodAsync(Guid payPeriodId, CancellationToken cancellationToken = default);
    Task<PayrollLine?> GetByPayPeriodAndEmployeeAsync(
        Guid payPeriodId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
    Task<PayrollLine?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task AddAsync(PayrollLine line, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PayrollLine> lines, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<PayrollLine> lines);
    void Update(PayrollLine line);
}
