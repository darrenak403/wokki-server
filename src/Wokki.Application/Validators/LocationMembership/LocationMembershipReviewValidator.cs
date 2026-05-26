using FluentValidation;
using Wokki.Application.Dtos.LocationMembership;
using Wokki.Domain.Enums;

namespace Wokki.Application.Validators.LocationMembership;

public sealed class LocationMembershipReviewValidator : AbstractValidator<LocationMembershipReviewDto>
{
    public LocationMembershipReviewValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s == LocationMembershipStatus.Active || s == LocationMembershipStatus.Rejected)
            .WithMessage("Status must be Active or Rejected.");

        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}
