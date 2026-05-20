using FluentValidation;
using Wokki.Application.Features.Auth.Dtos;

namespace Wokki.Application.Features.Auth;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
