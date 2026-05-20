using FluentValidation;
using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Validators.Schedule;

public sealed class CreateShiftAssignmentRequestValidator : AbstractValidator<CreateShiftAssignmentRequest>
{
    public CreateShiftAssignmentRequestValidator()
    {
        RuleFor(x => x.ShiftDefinitionId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
