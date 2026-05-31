using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Payroll;

public sealed record MyPayrollSummaryResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    PayPeriodStatus? PeriodStatus,
    int TotalWorkedMinutes,
    int RegularMinutes,
    int ApprovedOvertimeMinutes,
    decimal HourlyRate,
    decimal RegularPay,
    decimal OvertimePay,
    decimal GrossPay);

public sealed record SetPayrollLinePaidRequest(bool Paid);
