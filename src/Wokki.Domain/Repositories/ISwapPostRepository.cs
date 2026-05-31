using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface ISwapPostRepository
{
    Task<SwapPost?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<SwapPost?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListFeedAsync(
        Guid scheduleId,
        Guid departmentId,
        Guid locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListByEmployeeAsync(
        Guid employeeId,
        Guid? scheduleId = null,
        SwapPostStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingForAuthorAssignmentAsync(
        Guid authorAssignmentId,
        Guid? excludePostId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingForAcceptorAssignmentAsync(
        Guid acceptorAssignmentId,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListAuditAsync(
        Guid organizationId,
        IReadOnlySet<Guid>? locationIds,
        Guid? scheduleId,
        DateOnly? weekStartDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(SwapPost swapPost, CancellationToken cancellationToken = default);
    void Update(SwapPost swapPost);
    Task<int> HidePendingByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}
