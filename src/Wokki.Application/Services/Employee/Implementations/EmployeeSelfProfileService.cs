using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Mappings.Employees;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeSelfProfileService(
    IUnitOfWork unitOfWork,
    IImageStorageService imageStorage,
    IOrgAdminEmployeeProvisioner orgAdminEmployeeProvisioner) : IEmployeeSelfProfileService
{
    private const long MaxPaymentQrBytes = 5 * 1024 * 1024;

    public async Task<ApiResponse<EmployeeResponse>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken)
                       ?? await orgAdminEmployeeProvisioner.EnsureByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Self.ProfileFound);
    }

    public async Task<ApiResponse<FaceDescriptorResponse>> GetMyFaceDescriptorAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<FaceDescriptorResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        return ApiResponse<FaceDescriptorResponse>.SuccessResponse(
            new FaceDescriptorResponse(employee.FaceEmbedding),
            AppMessages.Self.ProfileFound);
    }

    public async Task<ApiResponse<EmployeeResponse>> UpdateMineAsync(
        Guid userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetTrackedEmployeeAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        if (employee.TerminatedAt is not null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        if (request.RemovePaymentQr && !string.IsNullOrWhiteSpace(employee.PaymentQrPublicId))
        {
            await imageStorage.DeleteAsync(employee.PaymentQrPublicId, cancellationToken);
            employee.ClearPaymentQr();
        }

        employee.ApplyMyProfileUpdate(request);
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Self.ProfileUpdated);
    }

    public async Task<ApiResponse<PaymentQrUploadResponse>> UploadPaymentQrAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        if (!imageStorage.IsConfigured)
            return ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Self.CloudinaryNotConfigured);

        if (contentLength <= 0 || contentLength > MaxPaymentQrBytes)
            return ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Self.PaymentQrInvalid);

        var employee = await GetTrackedEmployeeAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Self.NoEmployeeProfile);

        if (employee.TerminatedAt is not null)
            return ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        StoredImageResult upload;
        try
        {
            upload = await imageStorage.UploadPaymentQrAsync(
                content,
                fileName,
                contentType,
                employee.OrganizationId,
                employee.Id,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Self.PaymentQrInvalid);
        }

        if (!string.IsNullOrWhiteSpace(employee.PaymentQrPublicId)
            && !string.Equals(employee.PaymentQrPublicId, upload.PublicId, StringComparison.Ordinal))
        {
            await imageStorage.DeleteAsync(employee.PaymentQrPublicId, cancellationToken);
        }

        employee.PaymentQrImageUrl = upload.Url;
        employee.PaymentQrPublicId = upload.PublicId;
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PaymentQrUploadResponse>.SuccessResponse(
            new PaymentQrUploadResponse(upload.Url, upload.PublicId),
            AppMessages.Self.PaymentQrUploaded);
    }

    private async Task<EmployeeEntity?> GetTrackedEmployeeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var found = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (found is null)
            return null;

        return await unitOfWork.Employees.GetByIdAsync(found.Id, track: true, cancellationToken: cancellationToken);
    }

    private async Task<EmployeeResponse> BuildResponseAsync(
        EmployeeEntity employee,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken: cancellationToken)
                   ?? throw new InvalidOperationException($"User {employee.UserId} not found for employee {employee.Id}.");

        DepartmentEntity? department = null;
        if (!string.Equals(user.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
            && employee.DepartmentId.HasValue)
            department = await unitOfWork.Departments.GetByIdAsync(employee.DepartmentId.Value, cancellationToken: cancellationToken);
        LocationEntity? location = null;
        if (department is not null)
            location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);

        return employee.ToResponse(user, department, location);
    }
}
