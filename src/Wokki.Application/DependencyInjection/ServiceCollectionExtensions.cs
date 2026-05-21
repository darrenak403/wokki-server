using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wokki.Application.Services.Attendance.Implementations;
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
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IShiftDefinitionService, ShiftDefinitionService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ISwapRequestService, SwapRequestService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IChannelService, ChannelService>();

        return services;
    }
}
