using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Chat;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Chat;

public static class ChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/channels")
            .MapChannelRoutes()
            .WithTags("Chat")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapChannelRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListChannels")
            .WithDescription("Danh sách kênh chat của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<ChannelResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateChannel")
            .WithDescription("Tạo kênh Direct hoặc Group (Admin/Manager).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ChannelResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}/messages", ListMessagesAsync)
            .WithName("ListChannelMessages")
            .WithDescription("Lịch sử tin nhắn (cursor-based).")
            .RequireAuthorization()
            .Produces<ApiResponse<MessageListResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/messages", SendMessageAsync)
            .WithName("SendChannelMessage")
            .WithDescription("Gửi tin nhắn và broadcast qua SignalR.")
            .RequireAuthorization()
            .Produces<ApiResponse<MessageResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapDelete("/{id:guid}/messages/{messageId:guid}", DeleteMessageAsync)
            .WithName("DeleteChannelMessage")
            .WithDescription("Xóa mềm tin nhắn (người gửi hoặc Admin).")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IChannelService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<ChannelResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMineAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateChannelRequest request,
        [FromServices] IChannelService service,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CreateChannelRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<ChannelResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        // Known scope gap: CreateChannelRequest has no locationId — cannot scope by location.
        var response = await service.CreateAsync(request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListMessagesAsync(
        [FromRoute] Guid id,
        [FromServices] IChannelService service,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] DateTime? before,
        [FromQuery] int? limit,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<MessageListResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMessagesAsync(
            id,
            currentUser.UserId.Value,
            currentUser.Role,
            before,
            limit ?? 50,
            cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> SendMessageAsync(
        [FromRoute] Guid id,
        [FromBody] SendMessageRequest request,
        [FromServices] IChannelService service,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<SendMessageRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<MessageResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.SendMessageAsync(id, request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteMessageAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid messageId,
        [FromServices] IChannelService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.DeleteMessageAsync(
            id,
            messageId,
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);
        return response.ToHttpResult();
    }
}
