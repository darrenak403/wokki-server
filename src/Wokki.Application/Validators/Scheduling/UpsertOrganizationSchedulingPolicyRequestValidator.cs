using FluentValidation;
using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Validators.Scheduling;

public sealed class UpsertOrganizationSchedulingPolicyRequestValidator
    : AbstractValidator<UpsertOrganizationSchedulingPolicyRequest>
{
    public UpsertOrganizationSchedulingPolicyRequestValidator()
    {
        RuleFor(x => x.Rules)
            .NotNull()
            .WithMessage("rules is required.");
    }
}
