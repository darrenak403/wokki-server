using Wokki.Application.Dtos.Employee;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Employee.Interfaces;

public interface IEmployeeSelfProfileService
{
    Task<ApiResponse<EmployeeResponse>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeResponse>> UpdateMineAsync(
        Guid userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PaymentQrUploadResponse>> UploadPaymentQrAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default);
}
