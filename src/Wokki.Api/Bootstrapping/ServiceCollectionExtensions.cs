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
                if (cors.AllowedOrigins.Length > 0)
                    policy.WithOrigins(cors.AllowedOrigins);

                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddSignalR();
        services.AddScoped<IChatRealtimeNotifier, SignalRChatNotifier>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(RateLimitPolicies.Fixed, limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter(RateLimitPolicies.Clock, limiter =>
            {
                limiter.PermitLimit = 300;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });

        return services;
    }
}
