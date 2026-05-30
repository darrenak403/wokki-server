using FluentValidation;
using Wokki.Application.Dtos.Shift;

namespace Wokki.Application.Validators.Shift;

public sealed class CopyShiftDefinitionsRequestValidator : AbstractValidator<CopyShiftDefinitionsRequest>
{
    public CopyShiftDefinitionsRequestValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.SourceDepartmentId).NotEmpty();
        RuleFor(x => x.TargetDepartmentIds).NotEmpty();
        RuleForEach(x => x.TargetDepartmentIds).NotEmpty();
        RuleFor(x => x.TargetDepartmentIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Target department ids must be distinct.");
        RuleFor(x => x)
            .Must(x => !x.TargetDepartmentIds.Contains(x.SourceDepartmentId))
            .WithMessage("Source department cannot be a copy target.");
        When(x => x.ShiftIds is not null, () =>
        {
            RuleForEach(x => x.ShiftIds!).NotEmpty();
        });
    }
}
