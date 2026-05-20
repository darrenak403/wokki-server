using FluentValidation;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;

namespace Wokki.Api.Extensions;

public static class ValidationExtensions
{
    public static bool ValidateRequest<T>(
        this T request,
        IValidator<T> validator,
        out IResult? validationResult)
    {
        var result = validator.Validate(request);
        if (result.IsValid)
        {
            validationResult = null;
            return true;
        }

        var errors = result.Errors
            .Select(e => new ErrorDetail(e.PropertyName, e.ErrorMessage))
            .ToList();

        validationResult = ApiResponse<object>.FailureResponse(AppMessages.Validation.Failed, errors).ToHttpResult();
        return false;
    }
}
