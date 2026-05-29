using System.Globalization;
using System.Text;
using Wokki.Application.Dtos.Payroll;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Payroll.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using PayPeriodEntity = Wokki.Domain.Entities.PayPeriod;

namespace Wokki.Application.Services.Payroll.Implementations;

public sealed class PayrollService(IUnitOfWork unitOfWork, IOrganizationScopeService organizationScope) : IPayrollService
{
    private const int MaxExportRows = 500;

    public async Task<ApiResponse<PayrollSummaryResponse>> GetSummaryAsync(
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        var (period, lines, error) = await BuildSummaryCoreAsync(request, cancellationToken);
        if (error is not null)
            return ApiResponse<PayrollSummaryResponse>.FailureResponse(error);

        var response = new PayrollSummaryResponse(
            period!.Id,
            period.DepartmentId,
            period.StartDate,
            period.EndDate,
            period.Status,
            lines!,
            lines!.Sum(l => l.GrossPay));

        return ApiResponse<PayrollSummaryResponse>.SuccessResponse(response, AppMessages.Payroll.Summary);
    }

    public async Task<ApiResponse<PayrollEmployeeDetailResponse>> GetEmployeeDetailAsync(
        Guid employeeId,
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: cancellationToken);
        if (employee is null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<PayrollEmployeeDetailResponse>.FailureResponse(AppMessages.Payroll.EmployeeNotFound);

        var (period, lines, error) = await BuildSummaryCoreAsync(request, cancellationToken);
        if (error is not null)
            return ApiResponse<PayrollEmployeeDetailResponse>.FailureResponse(error);

        var line = lines!.FirstOrDefault(l => l.EmployeeId == employeeId);
        if (line is null)
        {
            return ApiResponse<PayrollEmployeeDetailResponse>.SuccessResponse(
                new PayrollEmployeeDetailResponse(
                    employee.Id,
                    employee.FirstName,
                    employee.LastName,
                    period!.Id,
                    period.StartDate,
                    period.EndDate,
                    0,
                    employee.HourlyRate,
                    0,
                    0,
                    0m,
                    []),
                AppMessages.Payroll.EmployeeSummary);
        }

        // For locked periods return no live attendance — snapshot totals are authoritative
        IReadOnlyList<PayrollAttendanceItemResponse> items = [];
        if (period!.Status != PayPeriodStatus.Locked)
        {
            var attendance = await unitOfWork.Attendance.ListByEmployeeAsync(
                employeeId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            items = attendance
                .Where(a => a.ClockOut is not null && a.AssignmentId is not null)
                .Select(a => new PayrollAttendanceItemResponse(a.Id, a.ClockIn, a.ClockOut, a.WorkedMinutes))
                .ToList();
        }

        return ApiResponse<PayrollEmployeeDetailResponse>.SuccessResponse(
            new PayrollEmployeeDetailResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                period!.Id,
                period.StartDate,
                period.EndDate,
                line.TotalWorkedMinutes,
                line.HourlyRate,
                line.GrossPay,
                line.ApprovedOvertimeMinutes,
                line.OvertimePay,
                items),
            AppMessages.Payroll.EmployeeSummary);
    }

    public async Task<ApiResponse<PayrollExportResult>> ExportCsvAsync(
        PayrollPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAsync(request, cancellationToken);
        if (!summary.Success || summary.Data is null)
            return ApiResponse<PayrollExportResult>.FailureResponse(summary.Message);

        if (summary.Data.Lines.Count > MaxExportRows)
            return ApiResponse<PayrollExportResult>.FailureResponse(AppMessages.Payroll.ExportTooLarge);

        var csv = BuildCsv(summary.Data.Lines);
        var fileName = $"payroll-{request.DepartmentId:N}-{request.StartDate:yyyyMMdd}.csv";
        var result = new PayrollExportResult(
            Encoding.UTF8.GetBytes(csv),
            fileName,
            "text/csv");

        return ApiResponse<PayrollExportResult>.SuccessResponse(result, AppMessages.Payroll.Exported);
    }

    private async Task<(PayPeriodEntity? Period, IReadOnlyList<PayrollEmployeeLineResponse>? Lines, AppMessage? Error)> BuildSummaryCoreAsync(
        PayrollPeriodRequest request,
        CancellationToken cancellationToken,
        PayPeriodEntity? existingPeriod = null)
    {
        if (request.EndDate < request.StartDate)
            return (null, null, AppMessages.Payroll.InvalidDateRange);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !organizationScope.IsSameOrganization(department.OrganizationId))
            return (null, null, AppMessages.Payroll.DepartmentNotFound);

        var period = existingPeriod
                     ?? await unitOfWork.PayPeriods.GetByDepartmentAndStartAsync(
                         request.DepartmentId,
                         request.StartDate,
                         cancellationToken);

        if (period is null)
        {
            if (await unitOfWork.PayPeriods.HasOverlappingAsync(
                    request.DepartmentId,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken: cancellationToken))
                return (null, null, AppMessages.Payroll.PeriodOverlap);

            period = new PayPeriodEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = department.OrganizationId,
                DepartmentId = request.DepartmentId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = PayPeriodStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.PayPeriods.AddAsync(period, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (period.Status == PayPeriodStatus.Locked)
        {
            var lockedLines = await unitOfWork.PayrollLines.ListByPayPeriodAsync(period.Id, cancellationToken);
            if (lockedLines.Count > 0)
            {
                var employees = await LoadEmployeeMapAsync(lockedLines.Select(l => l.EmployeeId), cancellationToken);
                var snapshotLines = lockedLines.Select(l =>
                {
                    var emp = employees[l.EmployeeId];
                    return new PayrollEmployeeLineResponse(
                        l.EmployeeId,
                        emp.FirstName,
                        emp.LastName,
                        l.TotalWorkedMinutes,
                        l.HourlyRate,
                        l.GrossPay,
                        l.ApprovedOvertimeMinutes,
                        l.OvertimePay);
                }).ToList();
                return (period, snapshotLines, null);
            }
        }

        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            1000,
            department.OrganizationId,
            request.DepartmentId,
            locationIds: new HashSet<Guid> { department.LocationId },
            cancellationToken: cancellationToken);
        var employeeIds = employeePage.Items.Select(e => e.Id).ToList();
        var minutesByEmployee = await unitOfWork.Attendance.SumWorkedMinutesByEmployeeAsync(
            employeeIds,
            request.StartDate,
            request.EndDate,
            cancellationToken);
        var otByEmployee = await unitOfWork.Attendance.SumApprovedOvertimeByEmployeeAsync(
            employeeIds,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var lines = new List<PayrollEmployeeLineResponse>();
        foreach (var employee in employeePage.Items)
        {
            minutesByEmployee.TryGetValue(employee.Id, out var minutes);
            otByEmployee.TryGetValue(employee.Id, out var otMinutes);
            var hourlyRate = employee.HourlyRate;
            var otPay = Math.Round((otMinutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
            // otMinutes is already contained in minutes (same clock-in/out window); do not add twice
            var gross = Math.Round((minutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
            lines.Add(new PayrollEmployeeLineResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                minutes,
                hourlyRate,
                gross,
                otMinutes,
                otPay));
        }

        return (period, lines, null);
    }

    private async Task<Dictionary<Guid, EmployeeEntity>> LoadEmployeeMapAsync(
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, EmployeeEntity>();
        foreach (var id in employeeIds.Distinct())
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (employee is not null)
                map[id] = employee;
        }

        return map;
    }

    private static string BuildCsv(IReadOnlyList<PayrollEmployeeLineResponse> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EmployeeId,FirstName,LastName,TotalWorkedMinutes,TotalHours,HourlyRate,ApprovedOvertimeMinutes,OvertimePay,GrossPay");
        foreach (var line in lines)
        {
            var hours = Math.Round(line.TotalWorkedMinutes / 60m, 2);
            sb.AppendLine(string.Join(',',
                line.EmployeeId,
                Escape(line.FirstName),
                Escape(line.LastName),
                line.TotalWorkedMinutes.ToString(CultureInfo.InvariantCulture),
                hours.ToString(CultureInfo.InvariantCulture),
                line.HourlyRate.ToString(CultureInfo.InvariantCulture),
                line.ApprovedOvertimeMinutes.ToString(CultureInfo.InvariantCulture),
                line.OvertimePay.ToString(CultureInfo.InvariantCulture),
                line.GrossPay.ToString(CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
