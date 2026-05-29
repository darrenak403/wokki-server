using Wokki.Application.Dtos.SwapRequest;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.SwapRequest.Interfaces;

public interface ISwapRequestService
{
    Task<ApiResponse<SwapRequestResponse>> CreateAsync(
        CreateSwapRequestRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<SwapRequestResponse>>> ListAsync(
        SwapRequestListRequest request,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<SwapRequestResponse>>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        string? role,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> AcceptAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> DeclineAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> CancelAsync(
        Guid id,
        Guid userId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> OverrideApproveAsync(
        Guid id,
        Guid managerUserId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<SwapRequestResponse>> OverrideRejectAsync(
        Guid id,
        Guid managerUserId,
        SwapActionRequest? request,
        CancellationToken cancellationToken = default);
}
