using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Payroll;

public sealed record PayrollSummaryResponse(
    Guid PayPeriodId,
    Guid DepartmentId,
    DateOnly StartDate,
    DateOnly EndDate,
    PayPeriodStatus Status,
    IReadOnlyList<PayrollEmployeeLineResponse> Lines,
    decimal TotalGrossPay);

public sealed record PayrollEmployeeLineResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    int TotalWorkedMinutes,
    decimal HourlyRate,
    decimal GrossPay);
