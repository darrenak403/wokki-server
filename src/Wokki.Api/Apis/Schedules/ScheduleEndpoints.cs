using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Schedules;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/schedules")
            .MapScheduleRoutes()
            .WithTags("Schedules")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapScheduleRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListSchedules")
            .WithDescription("Danh sách lịch tuần.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PagedResponse<ScheduleResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateAsync)
            .WithName("CreateSchedule")
            .WithDescription("Tạo lịch tuần (Draft).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetScheduleById")
            .WithDescription("Chi tiết lịch và phân công.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleDetailResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateSchedule")
            .WithDescription("Cập nhật lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteSchedule")
            .WithDescription("Xóa lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/publish", PublishAsync)
            .WithName("PublishSchedule")
            .WithDescription("Publish lịch.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/unpublish", UnpublishAsync)
            .WithName("UnpublishSchedule")
            .WithDescription("Revert lịch về Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/copy", CopyAsync)
            .WithName("CopySchedule")
            .WithDescription("Copy lịch sang tuần mới (Draft).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/assignments", ListAssignmentsAsync)
            .WithName("ListScheduleAssignments")
            .WithDescription("Danh sách phân công trong lịch.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/assignments", CreateAssignmentAsync)
            .WithName("CreateScheduleAssignment")
            .WithDescription("Phân công nhân viên vào ca.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ShiftAssignmentResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/assignments/{assignmentId:guid}", DeleteAssignmentAsync)
            .WithName("DeleteScheduleAssignment")
            .WithDescription("Xóa phân công.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/suggest", SuggestAsync)
            .WithName("SuggestScheduleAssignments")
            .WithDescription("Gợi ý phân công bằng heuristic (không ghi DB).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ScheduleSuggestionsResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/apply-suggestions", ApplySuggestionsAsync)
            .WithName("ApplyScheduleSuggestions")
            .WithDescription("Áp dụng gợi ý vào lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ScheduleListRequest request,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateScheduleRequest request,
        [FromServices] IScheduleService service,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CreateScheduleRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CreateAsync(request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateScheduleRequest request,
        [FromServices] IScheduleService service,
        [FromServices] IValidator<UpdateScheduleRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> PublishAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.PublishAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UnpublishAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.UnpublishAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CopyAsync(
        [FromRoute] Guid id,
        [FromBody] CopyScheduleRequest request,
        [FromServices] IScheduleService service,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CopyScheduleRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<ScheduleResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CopyAsync(id, request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAssignmentsAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAssignmentsAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAssignmentAsync(
        [FromRoute] Guid id,
        [FromBody] CreateShiftAssignmentRequest request,
        [FromServices] IScheduleService service,
        [FromServices] IValidator<CreateShiftAssignmentRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.CreateAssignmentAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteAssignmentAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid assignmentId,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteAssignmentAsync(id, assignmentId, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> SuggestAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.SuggestAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ApplySuggestionsAsync(
        [FromRoute] Guid id,
        [FromBody] ApplyScheduleSuggestionsRequest request,
        [FromServices] IScheduleService service,
        [FromServices] IValidator<ApplyScheduleSuggestionsRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.ApplySuggestionsAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }
}
