using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Wokki.Api.Services;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Api.Bootstrapping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
                   ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsSettings.FrontendPolicy, policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();

                if (cors.AllowAnyOrigin)
                    policy.SetIsOriginAllowed(_ => true);
                else if (cors.AllowedOrigins.Length > 0)
                    policy.WithOrigins(cors.AllowedOrigins);
            });
        });

        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        services.AddScoped<IChatRealtimeNotifier, SignalRChatNotifier>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Fixed, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.AddPolicy(RateLimitPolicies.Clock, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
