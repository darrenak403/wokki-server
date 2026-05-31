using Wokki.Application.Common;
using Wokki.Application.Dtos.SwapPost;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;

namespace Wokki.Application.Services.SwapPost.Interfaces;

public interface ISwapPostService
{
    Task<ApiResponse<SwapPostResponse>> CreateAsync(
        CreateSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListFeedAsync(
        Guid scheduleId,
        Guid userId,
        string role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListMineAsync(
        Guid userId,
        Guid? scheduleId,
        SwapPostStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SwapPostResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SwapPostResponse>> AcceptAsync(
        Guid id,
        AcceptSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SwapPostAcceptPreviewResponse>> PreviewAcceptAsync(
        Guid id,
        AcceptSwapPostRequest request,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SwapPostResponse>> CancelAsync(
        Guid id,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<SwapPostResponse>>> ListAdminFeedAsync(
        Guid? locationId,
        Guid? departmentId,
        DateOnly? weekStartDate,
        Guid userId,
        string role,
        IReadOnlySet<Guid>? managedLocationIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<SwapPostAuditResponse>>> ListAuditAsync(
        Guid? scheduleId,
        Guid? locationId,
        Guid? departmentId,
        DateOnly? weekStartDate,
        Guid userId,
        string role,
        IReadOnlySet<Guid>? managedLocationIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
