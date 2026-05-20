using FluentValidation;
using Wokki.Application.Dtos.Location;

namespace Wokki.Application.Validators.Location;

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
    }
}
