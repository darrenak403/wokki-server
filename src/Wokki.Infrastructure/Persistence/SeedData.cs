using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Entities;
using Wokki.Domain.Constants;

namespace Wokki.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist. Skip seeding default users.");
            return;
        }

        logger.LogInformation("Seeding default users...");

        context.Users.AddRange(
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@gmail.com",
                PasswordHash = passwordHasher.HashPassword("12345@Abc"),
                Role = RoleConstants.Admin
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "manager@gmail.com",
                PasswordHash = passwordHasher.HashPassword("12345@Abc"),
                Role = RoleConstants.Manager
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "user@gmail.com",
                PasswordHash = passwordHasher.HashPassword("12345@Abc"),
                Role = RoleConstants.User
            });

        await context.SaveChangesAsync();
    }
}
