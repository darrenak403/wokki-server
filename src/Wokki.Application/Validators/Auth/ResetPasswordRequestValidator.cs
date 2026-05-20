using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
