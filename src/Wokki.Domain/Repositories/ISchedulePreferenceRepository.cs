using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface ISchedulePreferenceRepository
{
    Task<SchedulePreferenceSubmission?> GetByScheduleAndEmployeeAsync(
        Guid scheduleId,
        Guid employeeId,
        bool includeLines = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchedulePreferenceSubmission>> ListByScheduleAsync(
        Guid scheduleId,
        bool includeLines = false,
        SchedulePreferenceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<int> CountSubmittedByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task AddAsync(SchedulePreferenceSubmission entity, CancellationToken cancellationToken = default);

    Task AddLinesAsync(
        IReadOnlyList<SchedulePreferenceLine> lines,
        CancellationToken cancellationToken = default);

    void RemoveLines(IEnumerable<SchedulePreferenceLine> lines);

    void Remove(SchedulePreferenceSubmission submission);
}
