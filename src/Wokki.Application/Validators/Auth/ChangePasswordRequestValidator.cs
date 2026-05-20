using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
