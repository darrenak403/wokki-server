using FluentValidation;
using Wokki.Application.Dtos.Department;

namespace Wokki.Application.Validators.Department;

public sealed class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
