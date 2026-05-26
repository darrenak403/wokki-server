using Wokki.Application.Dtos.LocationMembership;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.LocationMembership.Interfaces;

public interface ILocationMembershipService
{
    Task<ApiResponse<LocationMembershipResponse>> RequestAsync(Guid userId, LocationMembershipRequestDto dto, CancellationToken ct = default);
    Task<ApiResponse<LocationMembershipResponse>> ReviewAsync(Guid membershipId, Guid callerId, bool isAdmin, LocationMembershipReviewDto dto, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<LocationMembershipResponse>>> ListByLocationAsync(Guid locationId, string? status, Guid callerId, bool isAdmin, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<LocationMembershipResponse>>> ListAllPendingAsync(Guid callerId, bool isAdmin, CancellationToken ct = default);
    Task<ApiResponse<LocationMembershipResponse?>> GetMyStatusAsync(Guid userId, CancellationToken ct = default);
}
