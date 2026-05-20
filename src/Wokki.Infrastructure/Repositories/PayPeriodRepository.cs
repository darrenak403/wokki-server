using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class PayPeriodRepository(AppDbContext context) : IPayPeriodRepository
{
    public async Task<PayPeriod?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.PayPeriods : context.PayPeriods.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PayPeriod?> GetByDepartmentAndStartAsync(
        Guid departmentId,
        DateOnly startDate,
        CancellationToken cancellationToken = default) =>
        await context.PayPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.DepartmentId == departmentId && p.StartDate == startDate, cancellationToken);

    public async Task<PayPeriod?> GetContainingDateAsync(
        Guid departmentId,
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        await context.PayPeriods.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.DepartmentId == departmentId && p.StartDate <= date && p.EndDate >= date,
                cancellationToken);

    public async Task<bool> HasOverlappingAsync(
        Guid departmentId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await context.PayPeriods.AnyAsync(
            p => p.DepartmentId == departmentId
                 && (excludeId == null || p.Id != excludeId.Value)
                 && p.StartDate <= endDate
                 && p.EndDate >= startDate,
            cancellationToken);

    public async Task AddAsync(PayPeriod payPeriod, CancellationToken cancellationToken = default) =>
        await context.PayPeriods.AddAsync(payPeriod, cancellationToken);

    public void Update(PayPeriod payPeriod) => context.PayPeriods.Update(payPeriod);
}
