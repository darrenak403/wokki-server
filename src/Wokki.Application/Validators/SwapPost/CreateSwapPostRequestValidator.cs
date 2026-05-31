using FluentValidation;
using Wokki.Application.Dtos.SwapPost;

namespace Wokki.Application.Validators.SwapPost;

public sealed class CreateSwapPostRequestValidator : AbstractValidator<CreateSwapPostRequest>
{
    public CreateSwapPostRequestValidator()
    {
        RuleFor(x => x.AuthorAssignmentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}
