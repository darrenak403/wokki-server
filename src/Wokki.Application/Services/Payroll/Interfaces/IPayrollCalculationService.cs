namespace Wokki.Application.Services.Payroll.Interfaces;

public sealed record PayrollEmployeeCalculation(
    int TotalWorkedMinutes,
    int RegularMinutes,
    int ApprovedOvertimeMinutes,
    decimal RegularPay,
    decimal OvertimePay,
    decimal GrossPay);

public interface IPayrollCalculationService
{
    PayrollEmployeeCalculation Calculate(int totalWorkedMinutes, int approvedOvertimeMinutes, decimal hourlyRate);
}
