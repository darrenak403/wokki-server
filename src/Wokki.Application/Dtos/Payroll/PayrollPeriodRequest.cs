namespace Wokki.Application.Dtos.Payroll;

public sealed record PayrollPeriodRequest(
    Guid DepartmentId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool? UnpaidOnly = null);
