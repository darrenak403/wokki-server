using FluentValidation;
using Wokki.Application.Common;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class ScheduleRosterRequestValidator : AbstractValidator<ScheduleRosterRequest>
{
    public ScheduleRosterRequestValidator()
    {
        RuleFor(x => x.WeekStartDate)
            .Must(ScheduleRules.IsMonday)
            .WithMessage("Week start date must be a Monday.");

        RuleFor(x => x)
            .Must(x =>
            {
                var end = x.WeekEndDate ?? x.WeekStartDate.AddDays(6);
                return end >= x.WeekStartDate && end.DayNumber - x.WeekStartDate.DayNumber <= 28;
            })
            .WithMessage("Date range must not exceed 28 days.");
    }
}
