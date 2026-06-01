using FluentValidation;
using Wokki.Application.Dtos.OrgJoinRequest;

namespace Wokki.Application.Validators.OrgJoinRequest;

public sealed class ApproveOrgJoinRequestValidator : AbstractValidator<ApproveOrgJoinRequest>
{
    public ApproveOrgJoinRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Phone).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
