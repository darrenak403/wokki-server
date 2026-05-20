using Wokki.Application.Dtos.Location;
using Wokki.Application.Mappings.Locations;
using Wokki.Application.Services.Location.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Location.Implementations;

public sealed class LocationService(IUnitOfWork unitOfWork) : ILocationService
{
    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Locations.ListAsync(activeOnly: false, cancellationToken);
        var responses = items.Select(l => l.ToResponse()).ToList();
        return ApiResponse<IReadOnlyList<LocationResponse>>.SuccessResponse(responses, AppMessages.Location.Listed);
    }

    public async Task<ApiResponse<LocationResponse>> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var existing = await unitOfWork.Locations.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            return ApiResponse<LocationResponse>.FailureResponse(AppMessages.Location.Exists);

        var entity = request.ToEntity();
        await unitOfWork.Locations.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<LocationResponse>.SuccessResponse(entity.ToResponse(), AppMessages.Location.Created);
    }

    public async Task<ApiResponse<LocationResponse>> UpdateAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (location is null)
            return ApiResponse<LocationResponse>.FailureResponse(AppMessages.Location.NotFound);

        var name = request.Name.Trim();
        var duplicate = await unitOfWork.Locations.GetByNameAsync(name, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
            return ApiResponse<LocationResponse>.FailureResponse(AppMessages.Location.Exists);

        location.ApplyUpdate(request);
        unitOfWork.Locations.Update(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<LocationResponse>.SuccessResponse(location.ToResponse(), AppMessages.Location.Updated);
    }
}
