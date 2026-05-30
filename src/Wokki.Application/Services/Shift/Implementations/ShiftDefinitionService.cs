using Wokki.Application.Dtos.Shift;
using Wokki.Application.Mappings.Shifts;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Shift.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Shift.Implementations;

public sealed class ShiftDefinitionService(IUnitOfWork unitOfWork, IOrganizationScopeService organizationScope) : IShiftDefinitionService
{
    public async Task<ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>> ListAsync(
        Guid locationId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: cancellationToken);
        if (location is null || !organizationScope.IsSameOrganization(location.OrganizationId))
            return ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>.FailureResponse(AppMessages.Shift.LocationNotFound);

        var items = await unitOfWork.ShiftDefinitions.ListAsync(locationId, departmentId, activeOnly: false, cancellationToken);
        return ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>.SuccessResponse(
            items.Select(s => s.ToResponse()).ToList(),
            AppMessages.Shift.Listed);
    }

    public async Task<ApiResponse<ShiftDefinitionResponse>> CreateAsync(
        CreateShiftDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.InvalidTimeRange);

        var organizationId = organizationScope.RequireOrganizationId();
        var location = await unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken: cancellationToken);
        if (location is null || !organizationScope.IsSameOrganization(location.OrganizationId))
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.LocationNotFound);

        if (request.DepartmentId.HasValue)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken: cancellationToken);
            if (department is null || department.OrganizationId != organizationId)
                return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Department.NotFound);
        }

        var entity = request.ToEntity(organizationId);
        await unitOfWork.ShiftDefinitions.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShiftDefinitionResponse>.SuccessResponse(entity.ToResponse(), AppMessages.Shift.Created);
    }

    public async Task<ApiResponse<ShiftDefinitionResponse>> UpdateAsync(
        Guid id,
        UpdateShiftDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.InvalidTimeRange);

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (shift is null || !organizationScope.IsSameOrganization(shift.OrganizationId))
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.NotFound);

        shift.ApplyUpdate(request);
        unitOfWork.ShiftDefinitions.Update(shift);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShiftDefinitionResponse>.SuccessResponse(shift.ToResponse(), AppMessages.Shift.Updated);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (shift is null || !organizationScope.IsSameOrganization(shift.OrganizationId))
            return ApiResponse<object>.FailureResponse(AppMessages.Shift.NotFound);

        shift.IsActive = false;
        unitOfWork.ShiftDefinitions.Update(shift);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Shift.Deleted);
    }

    public async Task<ApiResponse<CopyShiftDefinitionsResponse>> CopyAsync(
        CopyShiftDefinitionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = organizationScope.RequireOrganizationId();

        var location = await unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken: cancellationToken);
        if (location is null || !organizationScope.IsSameOrganization(location.OrganizationId))
            return ApiResponse<CopyShiftDefinitionsResponse>.FailureResponse(AppMessages.Shift.LocationNotFound);

        var sourceDepartment = await unitOfWork.Departments.GetByIdAsync(
            request.SourceDepartmentId,
            cancellationToken: cancellationToken);
        if (sourceDepartment is null
            || sourceDepartment.OrganizationId != organizationId
            || sourceDepartment.LocationId != request.LocationId)
        {
            return ApiResponse<CopyShiftDefinitionsResponse>.FailureResponse(AppMessages.Shift.CopySourceNotFound);
        }

        var targetDepartmentIds = request.TargetDepartmentIds.Distinct().ToList();
        if (targetDepartmentIds.Contains(request.SourceDepartmentId))
            return ApiResponse<CopyShiftDefinitionsResponse>.FailureResponse(AppMessages.Shift.CopyTargetInvalid);

        foreach (var targetDepartmentId in targetDepartmentIds)
        {
            var targetDepartment = await unitOfWork.Departments.GetByIdAsync(
                targetDepartmentId,
                cancellationToken: cancellationToken);
            if (targetDepartment is null
                || targetDepartment.OrganizationId != organizationId
                || targetDepartment.LocationId != request.LocationId)
            {
                return ApiResponse<CopyShiftDefinitionsResponse>.FailureResponse(AppMessages.Shift.CopyTargetInvalid);
            }
        }

        var sourceShifts = await unitOfWork.ShiftDefinitions.ListByDepartmentAsync(
            request.LocationId,
            request.SourceDepartmentId,
            activeOnly: true,
            cancellationToken);

        if (request.ShiftIds is { Count: > 0 })
        {
            var selectedIds = request.ShiftIds.ToHashSet();
            sourceShifts = sourceShifts.Where(shift => selectedIds.Contains(shift.Id)).ToList();
        }

        if (sourceShifts.Count == 0)
            return ApiResponse<CopyShiftDefinitionsResponse>.FailureResponse(AppMessages.Shift.CopyNothingToCopy);

        var copiedCount = 0;
        var skippedCount = 0;
        var createdShiftIds = new List<Guid>();
        var skipped = new List<CopyShiftSkippedItem>();

        foreach (var targetDepartmentId in targetDepartmentIds)
        {
            var existingShifts = await unitOfWork.ShiftDefinitions.ListByDepartmentAsync(
                request.LocationId,
                targetDepartmentId,
                activeOnly: true,
                cancellationToken);
            var existingKeys = existingShifts
                .Select(ShiftDefinitionMapper.DedupeKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var sourceShift in sourceShifts)
            {
                var dedupeKey = ShiftDefinitionMapper.DedupeKey(sourceShift);
                if (existingKeys.Contains(dedupeKey))
                {
                    skippedCount++;
                    skipped.Add(new CopyShiftSkippedItem(targetDepartmentId, sourceShift.Name, "DUPLICATE"));
                    continue;
                }

                var copy = ShiftDefinitionMapper.CloneToDepartment(sourceShift, targetDepartmentId);
                await unitOfWork.ShiftDefinitions.AddAsync(copy, cancellationToken);
                existingKeys.Add(dedupeKey);
                createdShiftIds.Add(copy.Id);
                copiedCount++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CopyShiftDefinitionsResponse>.SuccessResponse(
            new CopyShiftDefinitionsResponse(copiedCount, skippedCount, createdShiftIds, skipped),
            AppMessages.Shift.Copied);
    }
}
