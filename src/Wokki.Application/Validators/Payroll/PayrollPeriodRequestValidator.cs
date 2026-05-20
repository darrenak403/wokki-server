using FluentValidation;
using Wokki.Application.Dtos.Payroll;

namespace Wokki.Application.Validators.Payroll;

public sealed class PayrollPeriodRequestValidator : AbstractValidator<PayrollPeriodRequest>
{
    public PayrollPeriodRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}
