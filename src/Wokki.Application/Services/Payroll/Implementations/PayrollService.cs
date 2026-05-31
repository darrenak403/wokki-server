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
using PayrollLineEntity = Wokki.Domain.Entities.PayrollLine;

namespace Wokki.Application.Services.Payroll.Implementations;

public sealed class PayrollService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    IPayrollCalculationService payrollCalculation) : IPayrollService
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
                    [],
                    employee.BankAccountNumber,
                    employee.BankAccountHolderName,
                    employee.BankName,
                    employee.PaymentQrImageUrl),
                AppMessages.Payroll.EmployeeSummary);
        }

        IReadOnlyList<PayrollAttendanceItemResponse> items = [];
        if (period!.Status != PayPeriodStatus.Locked)
        {
            var attendance = await unitOfWork.Attendance.ListByEmployeeAsync(
                employeeId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            items = attendance
                .Where(a => a.ClockOut is not null && a.AssignmentId is not null && a.Mode == AttendanceMode.Assignment)
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
                items,
                employee.BankAccountNumber,
                employee.BankAccountHolderName,
                employee.BankName,
                employee.PaymentQrImageUrl),
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

    public async Task<ApiResponse<PayrollSummaryResponse>> LockPeriodAsync(
        Guid payPeriodId,
        CancellationToken cancellationToken = default)
    {
        var period = await unitOfWork.PayPeriods.GetByIdAsync(payPeriodId, track: true, cancellationToken: cancellationToken);
        if (period is null || !organizationScope.IsSameOrganization(period.OrganizationId))
            return ApiResponse<PayrollSummaryResponse>.FailureResponse(AppMessages.Payroll.PeriodNotFound);

        if (period.Status == PayPeriodStatus.Locked)
            return ApiResponse<PayrollSummaryResponse>.FailureResponse(AppMessages.Payroll.PeriodAlreadyLocked);

        var request = new PayrollPeriodRequest(period.DepartmentId, period.StartDate, period.EndDate);
        var (_, lines, error) = await BuildSummaryCoreAsync(request, cancellationToken, period);
        if (error is not null || lines is null)
            return ApiResponse<PayrollSummaryResponse>.FailureResponse(error ?? AppMessages.Validation.Failed);

        var snapshotLines = lines.Select(line =>
        {
            var employee = line.EmployeeId;
            return new PayrollLineEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = period.OrganizationId,
                PayPeriodId = period.Id,
                EmployeeId = employee,
                TotalWorkedMinutes = line.TotalWorkedMinutes,
                RegularMinutes = line.RegularMinutes,
                HourlyRate = line.HourlyRate,
                GrossPay = line.GrossPay,
                ApprovedOvertimeMinutes = line.ApprovedOvertimeMinutes,
                OvertimePay = line.OvertimePay,
                CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        await unitOfWork.PayrollLines.AddRangeAsync(snapshotLines, cancellationToken);
        period.Status = PayPeriodStatus.Locked;
        unitOfWork.PayPeriods.Update(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new PayrollSummaryResponse(
            period.Id,
            period.DepartmentId,
            period.StartDate,
            period.EndDate,
            period.Status,
            lines,
            lines.Sum(l => l.GrossPay));

        return ApiResponse<PayrollSummaryResponse>.SuccessResponse(response, AppMessages.Payroll.PeriodLocked);
    }

    public async Task<ApiResponse<MyPayrollSummaryResponse>> GetMySummaryAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<MyPayrollSummaryResponse>.FailureResponse(AppMessages.Payroll.EmployeeNotFound);

        if (!employee.DepartmentId.HasValue)
            return ApiResponse<MyPayrollSummaryResponse>.FailureResponse(AppMessages.Payroll.EmployeeNotFound);

        var period = await unitOfWork.PayPeriods.GetByDepartmentAndStartAsync(
            employee.DepartmentId.Value,
            startDate,
            cancellationToken);

        PayPeriodStatus? status = period?.Status;

        if (period?.Status == PayPeriodStatus.Locked)
        {
            var lockedLine = await unitOfWork.PayrollLines.GetByPayPeriodAndEmployeeAsync(
                period.Id,
                employee.Id,
                cancellationToken);
            if (lockedLine is not null)
            {
                var calc = payrollCalculation.Calculate(
                    lockedLine.TotalWorkedMinutes,
                    lockedLine.ApprovedOvertimeMinutes,
                    lockedLine.HourlyRate);

                return ApiResponse<MyPayrollSummaryResponse>.SuccessResponse(
                    new MyPayrollSummaryResponse(
                        period.StartDate,
                        period.EndDate,
                        status,
                        calc.TotalWorkedMinutes,
                        calc.RegularMinutes,
                        calc.ApprovedOvertimeMinutes,
                        lockedLine.HourlyRate,
                        calc.RegularPay,
                        calc.OvertimePay,
                        calc.GrossPay),
                    AppMessages.Payroll.MySummary);
            }
        }

        var attendance = await unitOfWork.Attendance.ListByEmployeeAsync(
            employee.Id,
            startDate,
            endDate,
            cancellationToken);

        var shiftAttendance = attendance
            .Where(a => a.ClockOut is not null && a.AssignmentId is not null && a.Mode == AttendanceMode.Assignment)
            .ToList();
        var totalMinutes = shiftAttendance.Sum(a => a.WorkedMinutes);
        var otMinutes = shiftAttendance.Sum(a => a.ApprovedOvertimeMinutes);
        var calcLive = payrollCalculation.Calculate(totalMinutes, otMinutes, employee.HourlyRate);

        return ApiResponse<MyPayrollSummaryResponse>.SuccessResponse(
            new MyPayrollSummaryResponse(
                startDate,
                endDate,
                status,
                calcLive.TotalWorkedMinutes,
                calcLive.RegularMinutes,
                calcLive.ApprovedOvertimeMinutes,
                employee.HourlyRate,
                calcLive.RegularPay,
                calcLive.OvertimePay,
                calcLive.GrossPay),
            AppMessages.Payroll.MySummary);
    }

    public async Task<ApiResponse<PayrollEmployeeLineResponse>> SetLinePaidAsync(
        Guid payPeriodId,
        Guid employeeId,
        bool paid,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var period = await unitOfWork.PayPeriods.GetByIdAsync(payPeriodId, track: false, cancellationToken: cancellationToken);
        if (period is null || !organizationScope.IsSameOrganization(period.OrganizationId))
            return ApiResponse<PayrollEmployeeLineResponse>.FailureResponse(AppMessages.Payroll.PeriodNotFound);

        if (period.Status != PayPeriodStatus.Locked)
            return ApiResponse<PayrollEmployeeLineResponse>.FailureResponse(AppMessages.Validation.Failed);

        var lineSnapshot = await unitOfWork.PayrollLines.GetByPayPeriodAndEmployeeAsync(payPeriodId, employeeId, cancellationToken);
        if (lineSnapshot is null)
            return ApiResponse<PayrollEmployeeLineResponse>.FailureResponse(AppMessages.Payroll.LineNotFound);

        var lineEntity = await unitOfWork.PayrollLines.GetByIdAsync(lineSnapshot.Id, track: true, cancellationToken);
        if (lineEntity is null)
            return ApiResponse<PayrollEmployeeLineResponse>.FailureResponse(AppMessages.Payroll.LineNotFound);

        lineEntity.PaidAt = paid ? DateTime.UtcNow : null;
        lineEntity.PaidMarkedBy = paid ? adminUserId : null;
        unitOfWork.PayrollLines.Update(lineEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return ApiResponse<PayrollEmployeeLineResponse>.FailureResponse(AppMessages.Payroll.EmployeeNotFound);

        var calc = payrollCalculation.Calculate(
            lineEntity.TotalWorkedMinutes,
            lineEntity.ApprovedOvertimeMinutes,
            lineEntity.HourlyRate);

        return ApiResponse<PayrollEmployeeLineResponse>.SuccessResponse(
            ToPayrollLine(
                employeeId,
                employee,
                calc.TotalWorkedMinutes,
                calc.RegularMinutes,
                lineEntity.HourlyRate,
                calc.GrossPay,
                calc.ApprovedOvertimeMinutes,
                calc.OvertimePay,
                lineEntity.PaidAt is not null),
            AppMessages.Payroll.PaidUpdated);
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
                var snapshotLines = lockedLines
                    .Select(l =>
                    {
                        var emp = employees[l.EmployeeId];
                        var calc = payrollCalculation.Calculate(l.TotalWorkedMinutes, l.ApprovedOvertimeMinutes, l.HourlyRate);
                        return ToPayrollLine(
                            l.EmployeeId,
                            emp,
                            calc.TotalWorkedMinutes,
                            l.RegularMinutes > 0 ? l.RegularMinutes : calc.RegularMinutes,
                            l.HourlyRate,
                            l.GrossPay,
                            calc.ApprovedOvertimeMinutes,
                            l.OvertimePay,
                            l.PaidAt is not null);
                    })
                    .Where(l => request.UnpaidOnly != true || !l.IsPaid)
                    .ToList();
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
            var calc = payrollCalculation.Calculate(minutes, otMinutes, employee.HourlyRate);
            lines.Add(ToPayrollLine(
                employee.Id,
                employee,
                calc.TotalWorkedMinutes,
                calc.RegularMinutes,
                employee.HourlyRate,
                calc.GrossPay,
                calc.ApprovedOvertimeMinutes,
                calc.OvertimePay,
                false));
        }

        return (period, lines, null);
    }

    private static PayrollEmployeeLineResponse ToPayrollLine(
        Guid employeeId,
        EmployeeEntity employee,
        int totalWorkedMinutes,
        int regularMinutes,
        decimal hourlyRate,
        decimal grossPay,
        int approvedOvertimeMinutes,
        decimal overtimePay,
        bool isPaid) =>
        new(
            employeeId,
            employee.FirstName,
            employee.LastName,
            totalWorkedMinutes,
            regularMinutes,
            hourlyRate,
            grossPay,
            approvedOvertimeMinutes,
            overtimePay,
            isPaid,
            employee.BankAccountNumber,
            employee.BankAccountHolderName,
            employee.BankName,
            employee.PaymentQrImageUrl);

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
        sb.AppendLine("EmployeeId,FirstName,LastName,TotalWorkedMinutes,RegularMinutes,TotalHours,HourlyRate,ApprovedOvertimeMinutes,OvertimePay,GrossPay,IsPaid,BankName,BankAccountHolderName,BankAccountNumber,PaymentQrImageUrl");
        foreach (var line in lines)
        {
            var hours = Math.Round(line.TotalWorkedMinutes / 60m, 2);
            sb.AppendLine(string.Join(',',
                line.EmployeeId,
                Escape(line.FirstName),
                Escape(line.LastName),
                line.TotalWorkedMinutes.ToString(CultureInfo.InvariantCulture),
                line.RegularMinutes.ToString(CultureInfo.InvariantCulture),
                hours.ToString(CultureInfo.InvariantCulture),
                line.HourlyRate.ToString(CultureInfo.InvariantCulture),
                line.ApprovedOvertimeMinutes.ToString(CultureInfo.InvariantCulture),
                line.OvertimePay.ToString(CultureInfo.InvariantCulture),
                line.GrossPay.ToString(CultureInfo.InvariantCulture),
                line.IsPaid.ToString(CultureInfo.InvariantCulture),
                Escape(line.BankName ?? string.Empty),
                Escape(line.BankAccountHolderName ?? string.Empty),
                Escape(line.BankAccountNumber ?? string.Empty),
                Escape(line.PaymentQrImageUrl ?? string.Empty)));
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
