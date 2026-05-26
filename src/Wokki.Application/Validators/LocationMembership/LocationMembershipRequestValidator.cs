using FluentValidation;
using Wokki.Application.Dtos.LocationMembership;

namespace Wokki.Application.Validators.LocationMembership;

public sealed class LocationMembershipRequestValidator : AbstractValidator<LocationMembershipRequestDto>
{
    public LocationMembershipRequestValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty();
    }
}
