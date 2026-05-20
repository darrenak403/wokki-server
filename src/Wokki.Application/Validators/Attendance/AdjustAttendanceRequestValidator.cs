using FluentValidation;
using Wokki.Application.Dtos.Attendance;

namespace Wokki.Application.Validators.Attendance;

public sealed class AdjustAttendanceRequestValidator : AbstractValidator<AdjustAttendanceRequest>
{
    public AdjustAttendanceRequestValidator()
    {
        RuleFor(x => x.AdjustmentNote).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ClockOut).GreaterThan(x => x.ClockIn);
    }
}
