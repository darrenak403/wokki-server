using FluentValidation;
using Wokki.Application.Dtos.Workspace;

namespace Wokki.Application.Validators.Workspace;

public sealed class TransferDepartmentValidator : AbstractValidator<TransferDepartmentRequest>
{
    public TransferDepartmentValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ToDepartmentId).NotEmpty();
    }
}
