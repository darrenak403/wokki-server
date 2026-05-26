using Wokki.Application.Dtos.LocationMembership;
using Wokki.Application.Services.LocationMembership.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationMembership.Implementations;

public sealed class LocationMembershipService(IUnitOfWork unitOfWork) : ILocationMembershipService
{
    public async Task<ApiResponse<LocationMembershipResponse>> RequestAsync(
        Guid userId,
        LocationMembershipRequestDto dto,
        CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.NoEmployeeProfile);

        var employeeId = employee.Id;

        var location = await unitOfWork.Locations.GetByIdAsync(dto.LocationId, cancellationToken: ct);
        if (location is null)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.LocationNotFound);

        var hasPendingOrActive = await unitOfWork.LocationMemberships.HasPendingOrActiveAsync(employeeId, dto.LocationId, ct);
        if (hasPendingOrActive)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.DuplicateRequest);

        var membership = new Domain.Entities.LocationMembership
        {
            Id = Guid.NewGuid(),
            LocationId = dto.LocationId,
            EmployeeId = employeeId,
            Status = LocationMembershipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        await unitOfWork.LocationMemberships.AddAsync(membership, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await unitOfWork.LocationMemberships.GetByIdAsync(membership.Id, cancellationToken: ct);
        return ApiResponse<LocationMembershipResponse>.SuccessResponse(MapResponse(created!), AppMessages.LocationMembership.Requested);
    }

    public async Task<ApiResponse<LocationMembershipResponse>> ReviewAsync(
        Guid membershipId,
        Guid callerId,
        bool isAdmin,
        LocationMembershipReviewDto dto,
        CancellationToken ct = default)
    {
        var membership = await unitOfWork.LocationMemberships.GetByIdAsync(membershipId, track: true, cancellationToken: ct);
        if (membership is null)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.NotFound);

        if (!isAdmin)
        {
            var isManager = await unitOfWork.LocationManagers.IsManagerOfLocationAsync(callerId, membership.LocationId, ct);
            if (!isManager)
                return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.Forbidden);
        }

        if (membership.Status != LocationMembershipStatus.Pending)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.InvalidReviewStatus);

        // FR-03: when approving, verify employee doesn't already have an active membership elsewhere
        if (dto.Status == LocationMembershipStatus.Active)
        {
            var existingActive = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(membership.EmployeeId, cancellationToken: ct);
            if (existingActive is not null && existingActive.Id != membershipId)
                return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.ActiveMembershipConflict);
        }

        membership.Status = dto.Status;
        membership.ReviewedById = callerId;
        membership.ReviewedAt = DateTime.UtcNow;
        membership.Note = dto.Note?.Trim();

        unitOfWork.LocationMemberships.Update(membership);
        await unitOfWork.SaveChangesAsync(ct);

        var updated = await unitOfWork.LocationMemberships.GetByIdAsync(membershipId, cancellationToken: ct);
        return ApiResponse<LocationMembershipResponse>.SuccessResponse(MapResponse(updated!), AppMessages.LocationMembership.Reviewed);
    }

    public async Task<ApiResponse<IReadOnlyList<LocationMembershipResponse>>> ListByLocationAsync(
        Guid locationId,
        string? status,
        Guid callerId,
        bool isAdmin,
        CancellationToken ct = default)
    {
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

    public async Task<ApiResponse<IReadOnlyList<LocationMembershipResponse>>> ListAllPendingAsync(
        Guid callerId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        IReadOnlySet<Guid>? locationIds = null;
        if (!isAdmin)
        {
            var managedLocations = await unitOfWork.LocationManagers.GetByUserAsync(callerId, ct);
            locationIds = managedLocations.Select(m => m.LocationId).ToHashSet();
        }

        var memberships = await unitOfWork.LocationMemberships.ListPendingAsync(locationIds, ct);
        var responses = memberships.Select(MapResponse).ToList();
        return ApiResponse<IReadOnlyList<LocationMembershipResponse>>.SuccessResponse(responses, AppMessages.LocationMembership.Listed);
    }

    public async Task<ApiResponse<LocationMembershipResponse?>> GetMyStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<LocationMembershipResponse?>.FailureResponse(AppMessages.LocationMembership.NoEmployeeProfile);

        var membership = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(employee.Id, cancellationToken: ct)
            ?? await unitOfWork.LocationMemberships.GetLatestPendingByEmployeeAsync(employee.Id, ct);
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
