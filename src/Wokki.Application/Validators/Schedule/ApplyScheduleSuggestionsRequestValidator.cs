using FluentValidation;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class ApplyScheduleSuggestionsRequestValidator : AbstractValidator<ApplyScheduleSuggestionsRequest>
{
    public ApplyScheduleSuggestionsRequestValidator()
    {
        RuleFor(x => x.Suggestions).NotEmpty();
        RuleForEach(x => x.Suggestions).ChildRules(item =>
        {
            item.RuleFor(s => s.ShiftDefinitionId).NotEmpty();
            item.RuleFor(s => s.EmployeeId).NotEmpty();
        });
    }
}
