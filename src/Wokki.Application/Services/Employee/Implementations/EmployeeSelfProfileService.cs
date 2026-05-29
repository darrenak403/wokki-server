using Wokki.Application.Common;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Mappings.Employees;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using LocationEntity = Wokki.Domain.Entities.Location;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeSelfProfileService(IUnitOfWork unitOfWork) : IEmployeeSelfProfileService
{
    public async Task<ApiResponse<EmployeeResponse>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Self.ProfileFound);
    }

    public async Task<ApiResponse<EmployeeResponse>> UpdateMineAsync(
        Guid userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var found = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (found is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        var employee = await unitOfWork.Employees.GetByIdAsync(found.Id, track: true, cancellationToken: cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        if (employee.TerminatedAt is not null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        employee.ApplyMyProfileUpdate(request);
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Self.ProfileUpdated);
    }

    private async Task<EmployeeResponse> BuildResponseAsync(
        EmployeeEntity employee,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken: cancellationToken)
                   ?? throw new InvalidOperationException($"User {employee.UserId} not found for employee {employee.Id}.");

        var department = await unitOfWork.Departments.GetByIdAsync(employee.DepartmentId, cancellationToken: cancellationToken);
        LocationEntity? location = null;
        if (department is not null)
            location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);

        return employee.ToResponse(user, department, location);
    }
}
