using Wokki.Application.Dtos.Location;
using Wokki.Application.Dtos.LocationManager;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.LocationManager.Interfaces;

public interface ILocationManagerService
{
    Task<ApiResponse<LocationManagerResponse>> AssignAsync(Guid locationId, AssignManagerDto dto, Guid assignedById, CancellationToken ct = default);
    Task<ApiResponse<object>> RemoveAsync(Guid locationId, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<LocationManagerResponse>>> ListByLocationAsync(Guid locationId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetMyLocationsAsync(Guid userId, CancellationToken ct = default);
}
