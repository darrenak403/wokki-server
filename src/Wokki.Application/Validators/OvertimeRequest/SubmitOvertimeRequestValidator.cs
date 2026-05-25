using FluentValidation;
using Wokki.Application.Dtos.OvertimeRequest;

namespace Wokki.Application.Validators.OvertimeRequest;

public sealed class SubmitOvertimeRequestValidator : AbstractValidator<SubmitOvertimeRequestDto>
{
    public SubmitOvertimeRequestValidator()
    {
        RuleFor(x => x.ShiftAssignmentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
