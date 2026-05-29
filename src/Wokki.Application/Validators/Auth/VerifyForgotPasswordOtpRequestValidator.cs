using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class VerifyForgotPasswordOtpRequestValidator : AbstractValidator<VerifyForgotPasswordOtpRequest>
{
    public VerifyForgotPasswordOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.OtpCode).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
