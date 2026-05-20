using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
