using Microsoft.AspNetCore.Http;
using Wokki.Common.Utils;

namespace Wokki.Common.Extensions;

public static class ApiResponseExtensions
{
    public static IResult ToHttpResult<T>(this ApiResponse<T> response) =>
        Results.Json(response, statusCode: response.Message.StatusCode);
}
