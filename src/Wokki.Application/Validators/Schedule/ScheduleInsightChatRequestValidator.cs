using FluentValidation;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class ScheduleInsightChatRequestValidator : AbstractValidator<ScheduleInsightChatRequest>
{
    public ScheduleInsightChatRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
