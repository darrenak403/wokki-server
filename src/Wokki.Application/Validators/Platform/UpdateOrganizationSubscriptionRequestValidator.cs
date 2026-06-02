using FluentValidation;
using Wokki.Application.Dtos.Platform;

namespace Wokki.Application.Validators.Platform;

public sealed class UpdateOrganizationSubscriptionRequestValidator : AbstractValidator<UpdateOrganizationSubscriptionRequest>
{
    public UpdateOrganizationSubscriptionRequestValidator()
    {
        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 3650)
            .When(x => x.DurationDays.HasValue)
            .WithMessage("durationDays must be between 1 and 3650.");
    }
}
