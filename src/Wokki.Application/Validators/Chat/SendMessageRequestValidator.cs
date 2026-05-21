using FluentValidation;
using Wokki.Application.Dtos.Chat;

namespace Wokki.Application.Validators.Chat;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
