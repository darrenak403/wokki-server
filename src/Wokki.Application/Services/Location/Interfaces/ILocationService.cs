using Wokki.Application.Dtos.Location;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Location.Interfaces;

public interface ILocationService
{
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<LocationResponse>> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LocationResponse>> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default);
}
