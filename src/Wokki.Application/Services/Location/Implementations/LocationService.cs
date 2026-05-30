using Wokki.Application.Dtos.Location;
using Wokki.Application.Mappings.Locations;
using Wokki.Application.Services.Location.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Location.Implementations;

public sealed class LocationService(IUnitOfWork unitOfWork, IOrganizationScopeService organizationScope) : ILocationService
{
    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> ListAsync(
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Locations.ListAsync(
            organizationId: organizationScope.GetCurrentOrganizationId(),
            activeOnly: false,
            locationIds: locationIds,
            cancellationToken: cancellationToken);
        var responses = items.Select(l => l.ToResponse()).ToList();
        return ApiResponse<IReadOnlyList<LocationResponse>>.SuccessResponse(responses, AppMessages.Location.Listed);
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Locations.ListAsync(
            organizationId: organizationScope.GetCurrentOrganizationId(),
            activeOnly: true,
            cancellationToken: cancellationToken);
        var responses = items.Select(l => l.ToResponse()).ToList();
        return ApiResponse<IReadOnlyList<LocationResponse>>.SuccessResponse(responses, AppMessages.Location.Listed);
    }

    public async Task<ApiResponse<LocationResponse>> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = organizationScope.RequireOrganizationId();
        var location = request.ToEntity(organizationId);
        await unitOfWork.Locations.AddAsync(location, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<LocationResponse>.SuccessResponse(location.ToResponse(), AppMessages.Location.Created);
    }

    public async Task<ApiResponse<LocationResponse>> UpdateAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (location is null || !organizationScope.IsSameOrganization(location.OrganizationId))
            return ApiResponse<LocationResponse>.FailureResponse(AppMessages.Location.NotFound);

        location.ApplyUpdate(request);
        unitOfWork.Locations.Update(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<LocationResponse>.SuccessResponse(location.ToResponse(), AppMessages.Location.Updated);
    }
}
