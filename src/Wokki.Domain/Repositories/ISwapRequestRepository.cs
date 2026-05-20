using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface ISwapRequestRepository
{
    Task<SwapRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SwapRequest> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        SwapStatus? status = null,
        Guid? departmentId = null,
        DateOnly? weekStartDate = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SwapRequest>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenSwapForAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);
    Task<bool> HasPeerAcceptedForAssignmentAsync(
        Guid assignmentId,
        Guid? excludeSwapId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(SwapRequest swapRequest, CancellationToken cancellationToken = default);
    void Update(SwapRequest swapRequest);
}
