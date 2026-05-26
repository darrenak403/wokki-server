using Wokki.Application.Dtos.Location;
using Wokki.Application.Dtos.LocationManager;
using Wokki.Application.Services.LocationManager.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationManager.Implementations;

public sealed class LocationManagerService(IUnitOfWork unitOfWork) : ILocationManagerService
{
    public async Task<ApiResponse<LocationManagerResponse>> AssignAsync(
        Guid locationId,
        AssignManagerDto dto,
        Guid assignedById,
        CancellationToken ct = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: ct);
        if (location is null)
            return ApiResponse<LocationManagerResponse>.FailureResponse(AppMessages.LocationManager.LocationNotFound);

        var user = await unitOfWork.Users.GetByIdAsync(dto.UserId, ct);
        if (user is null)
            return ApiResponse<LocationManagerResponse>.FailureResponse(AppMessages.LocationManager.UserNotFound);

        var existing = await unitOfWork.LocationManagers.GetAsync(locationId, dto.UserId, ct);
        if (existing is not null)
            return ApiResponse<LocationManagerResponse>.FailureResponse(AppMessages.LocationManager.AlreadyAssigned);

        var manager = new Domain.Entities.LocationManager
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            UserId = dto.UserId,
            AssignedById = assignedById,
            AssignedAt = DateTime.UtcNow
        };

        await unitOfWork.LocationManagers.AddAsync(manager, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await unitOfWork.LocationManagers.GetAsync(locationId, dto.UserId, ct);
        return ApiResponse<LocationManagerResponse>.SuccessResponse(MapResponse(created!), AppMessages.LocationManager.Assigned);
    }

    public async Task<ApiResponse<object>> RemoveAsync(Guid locationId, Guid userId, CancellationToken ct = default)
    {
        var manager = await unitOfWork.LocationManagers.GetAsync(locationId, userId, ct);
        if (manager is null)
            return ApiResponse<object>.FailureResponse(AppMessages.LocationManager.NotFound);

        unitOfWork.LocationManagers.Remove(manager);
        await unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<object>.SuccessResponse(null!, AppMessages.LocationManager.Removed);
    }

    public async Task<ApiResponse<IReadOnlyList<LocationManagerResponse>>> ListByLocationAsync(Guid locationId, CancellationToken ct = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: ct);
        if (location is null)
            return ApiResponse<IReadOnlyList<LocationManagerResponse>>.FailureResponse(AppMessages.LocationManager.LocationNotFound);

        var managers = await unitOfWork.LocationManagers.GetByLocationAsync(locationId, ct);
        return ApiResponse<IReadOnlyList<LocationManagerResponse>>.SuccessResponse(
            managers.Select(MapResponse).ToList(),
            AppMessages.LocationManager.Listed);
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetMyLocationsAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await unitOfWork.LocationManagers.GetByUserAsync(userId, ct);
        var locations = rows
            .Where(r => r.Location is not null)
            .Select(r => new LocationResponse(
                r.Location.Id,
                r.Location.Name,
                r.Location.Address,
                r.Location.TimeZone,
                r.Location.IsActive,
                r.Location.CreatedAt))
            .ToList();

        return ApiResponse<IReadOnlyList<LocationResponse>>.SuccessResponse(locations, AppMessages.LocationManager.MyLocationsListed);
    }

    private static LocationManagerResponse MapResponse(Domain.Entities.LocationManager m) => new(
        m.Id,
        m.LocationId,
        m.Location?.Name ?? string.Empty,
        m.UserId,
        m.User?.Email ?? string.Empty,
        m.AssignedById,
        m.AssignedAt);
}
