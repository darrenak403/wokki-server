using Wokki.Application.Dtos.OrgJoinRequest;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.OrgJoinRequest.Interfaces;

public interface IOrgJoinRequestService
{
    Task<ApiResponse<OrgJoinRequestResponse>> SubmitAsync(
        SubmitOrgJoinRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<OrgJoinRequestResponse>> GetMyAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> CancelMyAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<PendingOrgJoinRequestResponse>>> ListPendingAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<OrgJoinRequestResponse>> ApproveAsync(
        Guid id,
        ApproveOrgJoinRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<OrgJoinRequestResponse>> RejectAsync(
        Guid id,
        RejectOrgJoinRequest request,
        CancellationToken cancellationToken = default);
}
