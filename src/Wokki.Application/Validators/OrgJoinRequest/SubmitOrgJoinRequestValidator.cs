using FluentValidation;
using Wokki.Application.Dtos.OrgJoinRequest;

namespace Wokki.Application.Validators.OrgJoinRequest;

public sealed class SubmitOrgJoinRequestValidator : AbstractValidator<SubmitOrgJoinRequest>
{
    public SubmitOrgJoinRequestValidator() =>
        RuleFor(x => x.OrganizationId).NotEmpty();
}
