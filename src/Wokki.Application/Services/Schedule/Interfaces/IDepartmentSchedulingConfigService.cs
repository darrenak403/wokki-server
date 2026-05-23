using Wokki.Application.Dtos.Schedule;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface IDepartmentSchedulingConfigService
{
    Task<ApiResponse<DepartmentSchedulingPolicyResponse>> GetPolicyAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentSchedulingPolicyResponse>> UpsertPolicyAsync(
        Guid departmentId,
        UpsertDepartmentSchedulingPolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<JobPositionResponse>>> ListJobPositionsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobPositionResponse>> CreateJobPositionAsync(
        Guid departmentId,
        CreateJobPositionRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobPositionResponse>> UpdateJobPositionAsync(
        Guid departmentId,
        Guid jobPositionId,
        UpdateJobPositionRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> DeleteJobPositionAsync(
        Guid departmentId,
        Guid jobPositionId,
        CancellationToken cancellationToken = default);
}
