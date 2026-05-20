using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wokki.Application.Features.Auth;
using Wokki.Application.Features.Users;

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
