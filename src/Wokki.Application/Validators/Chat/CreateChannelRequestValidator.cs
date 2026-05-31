using FluentValidation;
using Wokki.Application.Dtos.Chat;
using Wokki.Domain.Enums;

namespace Wokki.Application.Validators.Chat;

public sealed class CreateChannelRequestValidator : AbstractValidator<CreateChannelRequest>
{
    public CreateChannelRequestValidator()
    {
        RuleFor(x => x.Type).Equal(ChannelType.Direct);
        RuleFor(x => x.MemberEmployeeIds).NotEmpty();
    }
}
