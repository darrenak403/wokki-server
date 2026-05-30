using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wokki.Application.Services.Attendance.Implementations;
using Wokki.Application.Services.LocationMembership.Implementations;
using Wokki.Application.Services.LocationMembership.Interfaces;
using Wokki.Application.Services.LocationManager.Implementations;
using Wokki.Application.Services.LocationManager.Interfaces;
using Wokki.Application.Services.LocationScope.Implementations;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.OrganizationScope.Implementations;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.OrganizationSchedulingPolicy.Implementations;
using Wokki.Application.Services.OrganizationSchedulingPolicy.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Implementations;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Application.Services.OvertimeRequest.Implementations;
using Wokki.Application.Services.OvertimeRequest.Interfaces;
using Wokki.Application.Services.Platform.Implementations;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.Chat.Implementations;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.Auth.Implementations;
using Wokki.Application.Services.Auth.Interfaces;
using Wokki.Application.Services.Department.Implementations;
using Wokki.Application.Services.Department.Interfaces;
using Wokki.Application.Services.Employee.Implementations;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.Location.Implementations;
using Wokki.Application.Services.Location.Interfaces;
using Wokki.Application.Services.Payroll.Implementations;
using Wokki.Application.Services.Payroll.Interfaces;
using Wokki.Application.Services.Schedule.Implementations;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Application.Services.SwapRequest.Implementations;
using Wokki.Application.Services.SwapRequest.Interfaces;
using Wokki.Application.Services.Shift.Implementations;
using Wokki.Application.Services.Shift.Interfaces;
using Wokki.Application.Services.User.Implementations;
using Wokki.Application.Services.User.Interfaces;
using Wokki.Application.Services.Stats.Implementations;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Application.Services.Workspace.Implementations;
using Wokki.Application.Services.Workspace.Interfaces;
using Wokki.Application.Validators.User;

namespace Wokki.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeSelfProfileService, EmployeeSelfProfileService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IShiftDefinitionService, ShiftDefinitionService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ISchedulePreferenceService, SchedulePreferenceService>();
        services.AddScoped<IScheduleInsightService, ScheduleInsightService>();
        services.AddScoped<ISwapRequestService, SwapRequestService>();
        services.AddScoped<IAutoCloseAttendanceService, AutoCloseAttendanceService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IOvertimeRequestService, OvertimeRequestService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<ILocationMembershipService, LocationMembershipService>();
        services.AddScoped<ILocationManagerService, LocationManagerService>();
        services.AddScoped<ILocationScopeService, LocationScopeService>();
        services.AddScoped<IOrganizationScopeService, OrganizationScopeService>();
        services.AddScoped<IOrganizationSubscriptionService, OrganizationSubscriptionService>();
        services.AddScoped<IOrganizationSchedulingPolicyService, OrganizationSchedulingPolicyService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();

        return services;
    }
}
