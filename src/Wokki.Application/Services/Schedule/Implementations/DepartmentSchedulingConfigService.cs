using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class DepartmentSchedulingConfigService(IUnitOfWork unitOfWork) : IDepartmentSchedulingConfigService
{
    public async Task<ApiResponse<IReadOnlyList<JobPositionResponse>>> ListJobPositionsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!await DepartmentExistsAsync(departmentId, cancellationToken))
            return ApiResponse<IReadOnlyList<JobPositionResponse>>.FailureResponse(AppMessages.Department.NotFound);

        var positions = await unitOfWork.JobPositions.ListByDepartmentAsync(departmentId, activeOnly: false, cancellationToken);
        return ApiResponse<IReadOnlyList<JobPositionResponse>>.SuccessResponse(
            positions.Select(ToResponse).ToList(),
            AppMessages.SchedulingConfig.JobPositionsListed);
    }

    public async Task<ApiResponse<JobPositionResponse>> CreateJobPositionAsync(
        Guid departmentId,
        CreateJobPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await DepartmentExistsAsync(departmentId, cancellationToken))
            return ApiResponse<JobPositionResponse>.FailureResponse(AppMessages.Department.NotFound);

        if (request.TargetHeadcount < 1)
            return ApiResponse<JobPositionResponse>.FailureResponse(AppMessages.SchedulingConfig.InvalidHeadcount);

        var entity = new JobPosition
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            TargetHeadcount = request.TargetHeadcount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.JobPositions.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<JobPositionResponse>.SuccessResponse(
            ToResponse(entity),
            AppMessages.SchedulingConfig.JobPositionCreated);
    }

    public async Task<ApiResponse<JobPositionResponse>> UpdateJobPositionAsync(
        Guid departmentId,
        Guid jobPositionId,
        UpdateJobPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.JobPositions.GetByIdAsync(jobPositionId, cancellationToken);
        if (entity is null || entity.DepartmentId != departmentId)
            return ApiResponse<JobPositionResponse>.FailureResponse(AppMessages.SchedulingConfig.JobPositionNotFound);

        if (request.TargetHeadcount < 1)
            return ApiResponse<JobPositionResponse>.FailureResponse(AppMessages.SchedulingConfig.InvalidHeadcount);

        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim();
        entity.TargetHeadcount = request.TargetHeadcount;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        unitOfWork.JobPositions.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<JobPositionResponse>.SuccessResponse(
            ToResponse(entity),
            AppMessages.SchedulingConfig.JobPositionUpdated);
    }

    public async Task<ApiResponse<object>> DeleteJobPositionAsync(
        Guid departmentId,
        Guid jobPositionId,
        CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.JobPositions.GetByIdAsync(jobPositionId, cancellationToken);
        if (entity is null || entity.DepartmentId != departmentId)
            return ApiResponse<object>.FailureResponse(AppMessages.SchedulingConfig.JobPositionNotFound);

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        unitOfWork.JobPositions.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.SchedulingConfig.JobPositionDeleted);
    }

    private async Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var department = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: cancellationToken);
        return department is not null;
    }

    private static JobPositionResponse ToResponse(JobPosition entity) =>
        new(entity.Id, entity.DepartmentId, entity.Name, entity.Code, entity.TargetHeadcount, entity.IsActive);
}
