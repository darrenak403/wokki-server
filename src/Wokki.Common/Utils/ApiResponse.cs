namespace Wokki.Common.Utils;

public sealed class ApiResponse<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public AppMessage Message { get; }
    public IReadOnlyList<ErrorDetail>? Errors { get; }

    private ApiResponse(bool success, T? data, AppMessage message, IReadOnlyList<ErrorDetail>? errors)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse<T> SuccessResponse(T data, AppMessage message) =>
        new(true, data, message, null);

    public static ApiResponse<T> FailureResponse(AppMessage message, IReadOnlyList<ErrorDetail>? errors = null) =>
        new(false, default, message, errors);

    public static ApiResponse<T> FailureResponse(
        T data,
        AppMessage message,
        IReadOnlyList<ErrorDetail>? errors = null) =>
        new(false, data, message, errors);

    public static ApiResponse<PagedResponse<TItem>> SuccessPagedResponse<TItem>(
        PagedResponse<TItem> pagedData,
        AppMessage message) =>
        ApiResponse<PagedResponse<TItem>>.SuccessResponse(pagedData, message);

    public static ApiResponse<PagedResponse<TItem>> SuccessPagedResponse<TItem>(
        IEnumerable<TItem> items,
        int page,
        int pageSize,
        int totalCount,
        AppMessage message) =>
        SuccessPagedResponse(new PagedResponse<TItem>(items, page, pageSize, totalCount), message);
}
