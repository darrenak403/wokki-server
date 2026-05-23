using Serilog;
using Scalar.AspNetCore;
using Wokki.Api.Apis.Attendance;
using Wokki.Api.Apis.Chat;
using Wokki.Api.Apis.Bedrock;
using Wokki.Api.Apis.Auth;
using Wokki.Api.Hubs;
using Wokki.Api.Apis.Departments;
using Wokki.Api.Apis.Employees;
using Wokki.Api.Apis.Health;
using Wokki.Api.Apis.Locations;
using Wokki.Api.Apis.EmployeeSelf;
using Wokki.Api.Apis.Payroll;
using Wokki.Api.Apis.Schedules;
using Wokki.Api.Apis.Shifts;
using Wokki.Api.Apis.SwapRequests;
using Wokki.Api.Apis.Users;

namespace Wokki.Api.Bootstrapping;

public static class PipelineExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
                if (httpContext.Request.Query.ContainsKey("access_token"))
                    diagnosticContext.Set("RequestQuery", "[redacted]");
                else
                    diagnosticContext.Set("RequestQuery", httpContext.Request.QueryString.Value ?? string.Empty);
            };
        });
        app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
        app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

        app.UseHttpsRedirection();
        app.UseCors(CorsSettings.FrontendPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<ChatHub>("/ws/chat");

        return app;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var enableDocs = app.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetValue<bool?>("ApiDocs:Enabled")
            ?? app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (enableDocs)
        {
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
        app.MapBedrockApi();
        app.MapAuthApi();
        app.MapUserApi();
        app.MapEmployeeApi();
        app.MapLocationApi();
        app.MapDepartmentApi();
        app.MapShiftApi();
        app.MapScheduleApi();
        app.MapEmployeeSelfApi();
        app.MapSwapRequestApi();
        app.MapAttendanceApi();
        app.MapPayrollApi();
        app.MapChannelApi();

        return app;
    }
}
