using FluentValidation;
using Wokki.Application.Dtos.Employee;
using Wokki.Domain.Constants;

namespace Wokki.Application.Validators.Employee;

public sealed class EmployeeRoleTransitionRequestValidator : AbstractValidator<EmployeeRoleTransitionRequest>
{
    public EmployeeRoleTransitionRequestValidator()
    {
        RuleFor(x => x.TargetRole)
            .Must(r => r == RoleConstants.User || r == RoleConstants.Manager)
            .WithMessage($"TargetRole must be '{RoleConstants.User}' or '{RoleConstants.Manager}'.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .When(x => x.TargetRole == RoleConstants.User);

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.HourlyRate.HasValue);
    }
}
