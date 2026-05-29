using FluentValidation;
using Wokki.Application.Dtos.Employee;

namespace Wokki.Application.Validators.Employee;

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.BankAccountNumber).MaximumLength(32);
        RuleFor(x => x.BankAccountHolderName).MaximumLength(200);
        RuleFor(x => x.BankName).MaximumLength(200);
    }
}
