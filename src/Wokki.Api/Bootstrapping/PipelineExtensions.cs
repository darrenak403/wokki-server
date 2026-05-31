using Serilog;
using Scalar.AspNetCore;
using Wokki.Api.Apis.Attendance;
using Wokki.Api.Apis.LocationMembership;
using Wokki.Api.Apis.LocationManager;
using Wokki.Api.Apis.OvertimeRequest;
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
using Wokki.Api.Apis.ScheduleLeaveRequest;
using Wokki.Api.Apis.Shifts;
using Wokki.Api.Apis.SwapPosts;
using Wokki.Api.Apis.Users;
using Wokki.Api.Apis.Organization;
using Wokki.Api.Apis.Platform;
using Wokki.Api.Apis.Scheduling;
using Wokki.Api.Apis.Workspace;

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
        app.UseMiddleware<Middleware.OrganizationContextMiddleware>();
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
        app.MapScheduleLeaveRequestApi();
        app.MapEmployeeSelfApi();
        app.MapSwapPostApi();
        app.MapAttendanceApi();
        app.MapPayrollApi();
        app.MapChannelApi();
        app.MapOvertimeRequestApi();
        app.MapLocationMembershipApi();
        app.MapLocationManagerApi();
        app.MapWorkspaceApi();
        app.MapPlatformApi();
        app.MapOrgStatsApi();
        app.MapSchedulingCatalogApi();

        return app;
    }
}
