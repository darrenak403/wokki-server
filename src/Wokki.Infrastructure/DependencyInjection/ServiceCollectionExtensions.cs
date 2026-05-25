using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Auth;
using Wokki.Infrastructure.Caching;
using Wokki.Infrastructure.Notifications;
using Wokki.Infrastructure.Persistence;
using Wokki.Infrastructure.Repositories;
using Wokki.Infrastructure.Bedrock;
using Wokki.Infrastructure.Scheduling;
using Wokki.Infrastructure.Tenancy;
using Wokki.Infrastructure.Workers;

namespace Wokki.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<AutoCloseAttendanceWorker>();
        services.AddBedrock(configuration);
        services.AddScheduleSuggestions(configuration);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, MicrosoftPasswordHasher>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        var smtp = configuration.GetSection(SmtpSettings.SectionName).Get<SmtpSettings>();
        if (smtp?.IsConfigured == true)
            services.AddScoped<INotificationService, EmailNotificationService>();
        else
            services.AddScoped<INotificationService, NoOpNotificationService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/ws/chat", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.Authenticated, p => p.RequireAuthenticatedUser())
            .AddPolicy(PolicyNames.Admin, p => p.RequireRole(RoleConstants.Admin));

        return services;
    }
}

public static class PolicyNames
{
    public const string Authenticated = "Authenticated";
    public const string Admin = "Admin";
}
