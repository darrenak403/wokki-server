using FluentValidation;
using Wokki.Application.Dtos.Auth;

namespace Wokki.Application.Validators.Auth;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
