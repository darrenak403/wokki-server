using FluentValidation;
using Wokki.Application.Dtos.Chat;
using Wokki.Domain.Enums;

namespace Wokki.Application.Validators.Chat;

public sealed class CreateChannelRequestValidator : AbstractValidator<CreateChannelRequest>
{
    public CreateChannelRequestValidator()
    {
        RuleFor(x => x.MemberEmployeeIds).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .When(x => x.Type == ChannelType.Group);
    }
}
