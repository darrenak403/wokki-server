using FluentValidation;
using Wokki.Application.Dtos.LocationManager;

namespace Wokki.Application.Validators.LocationManager;

public sealed class AssignManagerValidator : AbstractValidator<AssignManagerDto>
{
    public AssignManagerValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
