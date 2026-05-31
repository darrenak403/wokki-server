using Wokki.Application.Dtos.OvertimeRequest;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.OvertimeRequest.Interfaces;

public interface IOvertimeRequestService
{
    Task<ApiResponse<OvertimeRequestResponse>> SubmitAsync(Guid userId, SubmitOvertimeRequestDto dto, CancellationToken ct = default);
    Task<ApiResponse<OvertimeRequestResponse>> ClockOutOTAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListMyAsync(Guid userId, Guid? shiftAssignmentId, int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListPendingAsync(
        Guid reviewerUserId,
        bool isAdmin,
        Guid? departmentId,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken ct = default);
    Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListAllAsync(
        Guid reviewerUserId,
        bool isAdmin,
        Guid? departmentId,
        int month,
        int year,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken ct = default);
    Task<ApiResponse<OvertimeRequestResponse>> ApproveAsync(Guid id, Guid reviewerUserId, bool isAdmin, string? note, CancellationToken ct = default);
    Task<ApiResponse<OvertimeRequestResponse>> RejectAsync(Guid id, Guid reviewerUserId, bool isAdmin, string? note, CancellationToken ct = default);
}
