using FluentValidation;
using Wokki.Application.Dtos.Employee;

namespace Wokki.Application.Validators.Employee;

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}
