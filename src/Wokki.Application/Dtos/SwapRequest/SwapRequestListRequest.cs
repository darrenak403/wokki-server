using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.SwapRequest;

public sealed record SwapRequestListRequest(
    int Page = 1,
    int PageSize = 20,
    SwapStatus? Status = null,
    Guid? DepartmentId = null,
    DateOnly? WeekStartDate = null);
