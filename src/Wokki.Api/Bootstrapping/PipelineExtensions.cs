using Serilog;
using Scalar.AspNetCore;
using Wokki.Api.Apis.Auth;
using Wokki.Api.Apis.Health;
using Wokki.Api.Apis.Users;

namespace Wokki.Api.Bootstrapping;

public static class PipelineExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
        app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var enableDocs = app.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetValue<bool?>("ApiDocs:Enabled")
            ?? app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (enableDocs)
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Wokki API");
                options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
            });
        }

        app.MapGet("/", () => Results.Redirect("/scalar"))
            .WithName("GetRoot")
            .ExcludeFromDescription();

        app.MapHealthApi();
        app.MapAuthApi();
        app.MapUserApi();

        return app;
    }
}
