using Wokki.Application.Dtos.LocationMembership;
using Wokki.Application.Services.LocationMembership.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationMembership.Implementations;

public sealed class LocationMembershipService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope) : ILocationMembershipService
{
    public async Task<ApiResponse<IReadOnlyList<LocationMembershipResponse>>> ListByLocationAsync(
        Guid locationId,
        string? status,
        Guid callerId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: ct);
        if (location is null || !organizationScope.IsSameOrganization(location.OrganizationId))
            return ApiResponse<IReadOnlyList<LocationMembershipResponse>>.FailureResponse(AppMessages.LocationMembership.LocationNotFound);

        if (!isAdmin)
        {
            var isManager = await unitOfWork.LocationManagers.IsManagerOfLocationAsync(callerId, locationId, ct);
            if (!isManager)
                return ApiResponse<IReadOnlyList<LocationMembershipResponse>>.FailureResponse(AppMessages.LocationMembership.Forbidden);
        }

        LocationMembershipStatus? statusFilter = null;
        if (status is not null && Enum.TryParse<LocationMembershipStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var memberships = await unitOfWork.LocationMemberships.ListByLocationAsync(locationId, statusFilter, ct);
        var responses = memberships.Select(MapResponse).ToList();
        return ApiResponse<IReadOnlyList<LocationMembershipResponse>>.SuccessResponse(responses, AppMessages.LocationMembership.Listed);
    }

    public async Task<ApiResponse<LocationMembershipResponse?>> GetMyStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<LocationMembershipResponse?>.FailureResponse(AppMessages.LocationMembership.NoEmployeeProfile);

        var membership = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(employee.Id, cancellationToken: ct);
        return ApiResponse<LocationMembershipResponse?>.SuccessResponse(
            membership is null ? null : MapResponse(membership),
            AppMessages.LocationMembership.Found);
    }

    private static LocationMembershipResponse MapResponse(Domain.Entities.LocationMembership m) => new(
        m.Id,
        m.LocationId,
        m.Location?.Name ?? string.Empty,
        m.EmployeeId,
        m.Employee?.FirstName ?? string.Empty,
        m.Employee?.LastName ?? string.Empty,
        m.Status,
        m.RequestedAt,
        m.ReviewedById,
        m.ReviewedAt,
        m.Note);
}
