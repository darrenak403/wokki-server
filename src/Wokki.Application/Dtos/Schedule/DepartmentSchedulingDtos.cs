namespace Wokki.Application.Dtos.Schedule;

public sealed record DepartmentSchedulingPolicyResponse(
    Guid DepartmentId,
    int MaxShiftsPerEmployeePerWeek);

public sealed record UpsertDepartmentSchedulingPolicyRequest(
    int MaxShiftsPerEmployeePerWeek);

public sealed record JobPositionResponse(
    Guid Id,
    Guid DepartmentId,
    string Name,
    string Code,
    int TargetHeadcount,
    bool IsActive);

public sealed record CreateJobPositionRequest(
    string Name,
    string Code,
    int TargetHeadcount);

public sealed record UpdateJobPositionRequest(
    string Name,
    string Code,
    int TargetHeadcount,
    bool IsActive);
