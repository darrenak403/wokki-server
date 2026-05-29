using Wokki.Application.Dtos.Department;
using Wokki.Application.Mappings.Departments;
using Wokki.Application.Services.Department.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Department.Implementations;

public sealed class DepartmentService(IUnitOfWork unitOfWork) : IDepartmentService
{
    public async Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> ListAsync(
        Guid? locationId,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var items = await unitOfWork.Departments.ListAsync(
            locationId,
            activeOnly: false,
            locationIds: locationIds,
            cancellationToken: cancellationToken);
        var responses = items.Select(d => d.ToResponse()).ToList();
        return ApiResponse<IReadOnlyList<DepartmentResponse>>.SuccessResponse(responses, AppMessages.Department.Listed);
    }

    public async Task<ApiResponse<DepartmentResponse>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(request.LocationId, cancellationToken: cancellationToken);
        if (location is null)
            return ApiResponse<DepartmentResponse>.FailureResponse(AppMessages.Department.LocationNotFound);

        var entity = request.ToEntity();
        await unitOfWork.Departments.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<DepartmentResponse>.SuccessResponse(entity.ToResponse(), AppMessages.Department.Created);
    }

    public async Task<ApiResponse<DepartmentResponse>> UpdateAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = await unitOfWork.Departments.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<DepartmentResponse>.FailureResponse(AppMessages.Department.NotFound);

        department.ApplyUpdate(request);
        unitOfWork.Departments.Update(department);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<DepartmentResponse>.SuccessResponse(department.ToResponse(), AppMessages.Department.Updated);
    }
}
