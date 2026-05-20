using Wokki.Application.Dtos.Payroll;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Payroll.Interfaces;

public interface IPayrollService
{
    Task<ApiResponse<PayrollSummaryResponse>> GetSummaryAsync(
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<PayrollEmployeeDetailResponse>> GetEmployeeDetailAsync(
        Guid employeeId,
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<PayrollExportResult>> ExportCsvAsync(
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PayrollExportResult(byte[] Content, string FileName, string ContentType);
