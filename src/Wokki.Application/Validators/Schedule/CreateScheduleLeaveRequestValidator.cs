using FluentValidation;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class CreateScheduleLeaveRequestValidator : AbstractValidator<CreateScheduleLeaveRequest>
{
    public CreateScheduleLeaveRequestValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.ShiftDefinitionId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
