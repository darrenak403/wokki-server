using FluentValidation;
using Wokki.Application.Dtos.Workspace;

namespace Wokki.Application.Validators.Workspace;

public sealed class TransferLocationValidator : AbstractValidator<TransferLocationRequest>
{
    public TransferLocationValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ToLocationId).NotEmpty();
    }
}
