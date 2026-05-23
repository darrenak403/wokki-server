using FluentValidation;
using Wokki.Application.Dtos.Shift;

namespace Wokki.Application.Validators.Shift;

public sealed class CreateShiftDefinitionRequestValidator : AbstractValidator<CreateShiftDefinitionRequest>
{
    public CreateShiftDefinitionRequestValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RequiredRole).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MaxStaffPerSlot).GreaterThan(0).LessThanOrEqualTo(50);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(16);
        RuleFor(x => x).Must(x => x.EndTime > x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}
