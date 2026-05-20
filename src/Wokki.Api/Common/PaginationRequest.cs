using Microsoft.AspNetCore.Mvc;

namespace Wokki.Api.Common;

public sealed class PaginationRequest
{
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 10;
}
