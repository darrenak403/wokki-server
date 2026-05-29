using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmNewPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword)
            .WithMessage("Confirm password must match new password.");
    }
}
