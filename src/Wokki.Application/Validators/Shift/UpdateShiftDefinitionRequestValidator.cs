using FluentValidation;
using Wokki.Application.Dtos.Shift;

namespace Wokki.Application.Validators.Shift;

public sealed class UpdateShiftDefinitionRequestValidator : AbstractValidator<UpdateShiftDefinitionRequest>
{
    public UpdateShiftDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RequiredRole).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(16);
        RuleFor(x => x).Must(x => x.EndTime > x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}
