using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class SchedulePreferenceRepository(AppDbContext context) : ISchedulePreferenceRepository
{
    public async Task<SchedulePreferenceSubmission?> GetByScheduleAndEmployeeAsync(
        Guid scheduleId,
        Guid employeeId,
        bool includeLines = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SchedulePreferenceSubmission> query = context.SchedulePreferenceSubmissions;
        if (includeLines)
            query = query.Include(s => s.Lines);

        return await query.FirstOrDefaultAsync(
            s => s.ScheduleId == scheduleId && s.EmployeeId == employeeId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SchedulePreferenceSubmission>> ListByScheduleAsync(
        Guid scheduleId,
        bool includeLines = false,
        SchedulePreferenceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SchedulePreferenceSubmission> query = context.SchedulePreferenceSubmissions
            .Where(s => s.ScheduleId == scheduleId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (includeLines)
            query = query.Include(s => s.Lines);

        return await query.OrderBy(s => s.EmployeeId).ToListAsync(cancellationToken);
    }

    public Task<int> CountSubmittedByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        context.SchedulePreferenceSubmissions.CountAsync(
            s => s.ScheduleId == scheduleId && s.Status == SchedulePreferenceStatus.Submitted,
            cancellationToken);

    public Task AddAsync(SchedulePreferenceSubmission entity, CancellationToken cancellationToken = default) =>
        context.SchedulePreferenceSubmissions.AddAsync(entity, cancellationToken).AsTask();

    public void RemoveLines(SchedulePreferenceSubmission submission) =>
        context.SchedulePreferenceLines.RemoveRange(submission.Lines);
}
