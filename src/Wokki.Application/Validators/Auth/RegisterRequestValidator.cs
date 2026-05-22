using FluentValidation;
using Wokki.Application.Dtos.Auth;
using Wokki.Domain.Constants;

namespace Wokki.Application.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrWhiteSpace(role) || role.Trim() == RoleConstants.User)
            .WithMessage("Self-registration only creates accounts with the User role.");
    }
}
