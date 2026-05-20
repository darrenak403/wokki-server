using FluentValidation;
using Wokki.Application.Dtos.Location;

namespace Wokki.Application.Validators.Location;

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
    }
}
