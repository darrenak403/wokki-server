using Wokki.Application.Dtos.Location;
using Wokki.Application.Mappings.Locations;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Location.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
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

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Locations.ListAsync(activeOnly: true, cancellationToken);
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

    public async Task<ApiResponse<LocationSchedulingPolicyResponse>> GetSchedulingPolicyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (location is null)
            return ApiResponse<LocationSchedulingPolicyResponse>.FailureResponse(AppMessages.Location.NotFound);

        var policy = await unitOfWork.LocationSchedulingPolicies.GetByLocationIdAsync(id, cancellationToken: cancellationToken);

        return ApiResponse<LocationSchedulingPolicyResponse>.SuccessResponse(
            ToPolicyResponse(policy ?? new LocationSchedulingPolicy
            {
                LocationId = id,
                RulesJson = LocationSchedulingPolicyRules.Serialize(LocationSchedulingPolicyRules.GetDefaultRules())
            }),
            AppMessages.Location.SchedulingPolicyFound);
    }

    public async Task<ApiResponse<LocationSchedulingPolicyResponse>> UpsertSchedulingPolicyAsync(
        Guid id,
        UpsertLocationSchedulingPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (location is null)
            return ApiResponse<LocationSchedulingPolicyResponse>.FailureResponse(AppMessages.Location.NotFound);

        if (!LocationSchedulingPolicyRules.TryValidate(request.Rules))
            return ApiResponse<LocationSchedulingPolicyResponse>.FailureResponse(AppMessages.Location.SchedulingPolicyInvalid);

        var policy = await unitOfWork.LocationSchedulingPolicies.GetByLocationIdAsync(
            id,
            track: true,
            cancellationToken);
        if (policy is null)
        {
            policy = new LocationSchedulingPolicy { LocationId = id };
            ApplyPolicy(policy, request);
            await unitOfWork.LocationSchedulingPolicies.AddAsync(policy, cancellationToken);
        }
        else
        {
            ApplyPolicy(policy, request);
            unitOfWork.LocationSchedulingPolicies.Update(policy);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<LocationSchedulingPolicyResponse>.SuccessResponse(
            ToPolicyResponse(policy),
            AppMessages.Location.SchedulingPolicyUpdated);
    }

    private static void ApplyPolicy(LocationSchedulingPolicy policy, UpsertLocationSchedulingPolicyRequest request)
    {
        policy.RulesJson = LocationSchedulingPolicyRules.SerializeUpsert(request.Rules);
        policy.UpdatedAt = DateTime.UtcNow;
    }

    private static LocationSchedulingPolicyResponse ToPolicyResponse(LocationSchedulingPolicy p) =>
        new(
            p.LocationId,
            LocationSchedulingPolicyRules.SchemaVersion,
            LocationSchedulingPolicyRules.GetEffectiveRules(p),
            p.UpdatedAt);
}
