using FluentValidation;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class CopyScheduleRequestValidator : AbstractValidator<CopyScheduleRequest>
{
    public CopyScheduleRequestValidator()
    {
        RuleFor(x => x.TargetWeekStartDate).NotEmpty();
    }
}
