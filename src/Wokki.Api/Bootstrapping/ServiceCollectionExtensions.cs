using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Wokki.Api.Bootstrapping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
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
