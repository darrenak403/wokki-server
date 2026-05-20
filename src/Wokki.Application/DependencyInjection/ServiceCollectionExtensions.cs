using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wokki.Application.Services.Auth.Implementations;
using Wokki.Application.Services.Auth.Interfaces;
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

        return services;
    }
}
