using FluentValidation;
using Wokki.Application.Dtos.Attendance;

namespace Wokki.Application.Validators.Attendance;

public sealed class ClockInRequestValidator : AbstractValidator<ClockInRequest>
{
    private const int MaxPhotoBase64Length = 7_000_000;

    public ClockInRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.PhotoBase64).MaximumLength(MaxPhotoBase64Length);
    }
}
