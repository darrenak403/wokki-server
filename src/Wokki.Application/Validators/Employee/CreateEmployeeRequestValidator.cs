using FluentValidation;
using Wokki.Application.Dtos.Employee;
using Wokki.Domain.Constants;

namespace Wokki.Application.Validators.Employee;

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    private static readonly string[] AllowedRoles = [RoleConstants.User, RoleConstants.Manager, RoleConstants.Admin];

    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(6).When(x => !string.IsNullOrWhiteSpace(x.Password));
        RuleFor(x => x.Role).Must(r => AllowedRoles.Contains(r))
            .WithMessage($"Role must be {RoleConstants.User}, {RoleConstants.Manager}, or {RoleConstants.Admin}.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}
