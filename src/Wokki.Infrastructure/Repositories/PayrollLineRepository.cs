using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class PayrollLineRepository(AppDbContext context) : IPayrollLineRepository
{
    public async Task<IReadOnlyList<PayrollLine>> ListByPayPeriodAsync(
        Guid payPeriodId,
        CancellationToken cancellationToken = default) =>
        await context.PayrollLines.AsNoTracking()
            .Where(l => l.PayPeriodId == payPeriodId)
            .ToListAsync(cancellationToken);

    public async Task<PayrollLine?> GetByPayPeriodAndEmployeeAsync(
        Guid payPeriodId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await context.PayrollLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.PayPeriodId == payPeriodId && l.EmployeeId == employeeId, cancellationToken);

    public async Task<PayrollLine?> GetByIdAsync(
        Guid id,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track ? context.PayrollLines : context.PayrollLines.AsNoTracking();
        return await query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task AddAsync(PayrollLine line, CancellationToken cancellationToken = default) =>
        await context.PayrollLines.AddAsync(line, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<PayrollLine> lines, CancellationToken cancellationToken = default) =>
        await context.PayrollLines.AddRangeAsync(lines, cancellationToken);

    public void RemoveRange(IEnumerable<PayrollLine> lines) => context.PayrollLines.RemoveRange(lines);

    public void Update(PayrollLine line) => context.PayrollLines.Update(line);
}
