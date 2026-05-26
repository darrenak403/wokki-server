using Wokki.Application.Dtos.LocationMembership;
using Wokki.Application.Dtos.Workspace;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Workspace.Interfaces;

public interface IWorkspaceService
{
    Task<ApiResponse<object>> ChangeRoleAsync(Guid targetUserId, Guid callerId, ChangeRoleRequest request, CancellationToken ct = default);
    Task<ApiResponse<LocationMembershipResponse>> TransferLocationAsync(TransferLocationRequest request, Guid callerId, string callerRole, CancellationToken ct = default);
    Task<ApiResponse<object>> TransferDepartmentAsync(TransferDepartmentRequest request, Guid callerId, string callerRole, CancellationToken ct = default);
}
