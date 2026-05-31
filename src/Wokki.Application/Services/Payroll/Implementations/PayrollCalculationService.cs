using Wokki.Application.Services.Payroll.Interfaces;

namespace Wokki.Application.Services.Payroll.Implementations;

public sealed class PayrollCalculationService : IPayrollCalculationService
{
    public PayrollEmployeeCalculation Calculate(int totalWorkedMinutes, int approvedOvertimeMinutes, decimal hourlyRate)
    {
        var otMinutes = Math.Max(0, Math.Min(approvedOvertimeMinutes, totalWorkedMinutes));
        var regularMinutes = Math.Max(0, totalWorkedMinutes - otMinutes);

        var regularPay = Math.Round((regularMinutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
        var overtimePay = Math.Round((otMinutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
        var grossPay = regularPay + overtimePay;

        return new PayrollEmployeeCalculation(
            totalWorkedMinutes,
            regularMinutes,
            otMinutes,
            regularPay,
            overtimePay,
            grossPay);
    }
}
