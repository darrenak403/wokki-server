using Wokki.Application.Dtos.Shift;
using Wokki.Application.Mappings.Shifts;
using Wokki.Application.Services.Shift.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Shift.Implementations;

public sealed class ShiftDefinitionService(IUnitOfWork unitOfWork) : IShiftDefinitionService
{
    public async Task<ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>> ListAsync(
        Guid locationId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: cancellationToken);
        if (location is null)
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

        var location = await unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken: cancellationToken);
        if (location is null)
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.LocationNotFound);

        if (request.DepartmentId.HasValue)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken: cancellationToken);
            if (department is null)
                return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Department.NotFound);
        }

        var entity = request.ToEntity();
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
        if (shift is null)
            return ApiResponse<ShiftDefinitionResponse>.FailureResponse(AppMessages.Shift.NotFound);

        shift.ApplyUpdate(request);
        unitOfWork.ShiftDefinitions.Update(shift);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShiftDefinitionResponse>.SuccessResponse(shift.ToResponse(), AppMessages.Shift.Updated);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (shift is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Shift.NotFound);

        shift.IsActive = false;
        unitOfWork.ShiftDefinitions.Update(shift);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Shift.Deleted);
    }
}
