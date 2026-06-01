using FluentValidation;
using Wokki.Application.Dtos.OrgJoinRequest;

namespace Wokki.Application.Validators.OrgJoinRequest;

public sealed class RejectOrgJoinRequestValidator : AbstractValidator<RejectOrgJoinRequest>
{
    public RejectOrgJoinRequestValidator() =>
        RuleFor(x => x.Note).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Note));
}
