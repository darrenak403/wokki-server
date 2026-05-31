namespace Wokki.Application.Dtos.Payroll;

public sealed record PayrollEmployeeDetailResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    Guid PayPeriodId,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalWorkedMinutes,
    decimal HourlyRate,
    decimal GrossPay,
    int ApprovedOvertimeMinutes,
    decimal OvertimePay,
    IReadOnlyList<PayrollAttendanceItemResponse> AttendanceItems,
    string? BankAccountNumber = null,
    string? BankAccountHolderName = null,
    string? BankName = null,
    string? PaymentQrImageUrl = null);

public sealed record PayrollAttendanceItemResponse(
    Guid AttendanceId,
    DateTimeOffset ClockIn,
    DateTimeOffset? ClockOut,
    int WorkedMinutes);
