using FluentValidation;
using Wokki.Application.Dtos.Workspace;
using Wokki.Domain.Constants;

namespace Wokki.Application.Validators.Workspace;

public sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => r == RoleConstants.User || r == RoleConstants.Manager)
            .WithMessage($"Role must be '{RoleConstants.User}' or '{RoleConstants.Manager}'.");
    }
}
