using FluentValidation;
using Wokki.Application.Dtos.SwapRequest;

namespace Wokki.Application.Validators.SwapRequest;

public sealed class CreateSwapRequestRequestValidator : AbstractValidator<CreateSwapRequestRequest>
{
    public CreateSwapRequestRequestValidator()
    {
        RuleFor(x => x.RequesterAssignmentId).NotEmpty();
        RuleFor(x => x.TargetAssignmentId).NotEmpty();
        RuleFor(x => x).Must(x => x.RequesterAssignmentId != x.TargetAssignmentId)
            .WithMessage("Requester and target assignments must differ.");
    }
}
