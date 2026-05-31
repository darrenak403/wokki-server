using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence;

public static class SeedData
{
    public const string PlatformAdminEmail = "admin@gmail.com";
    public const string PlatformSeedPassword = "12345@Abc";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!await context.Database.CanConnectAsync())
        {
            logger.LogWarning("Database not reachable. Skip platform seed.");
            return;
        }

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning(
                "Pending migrations ({Count}). Skip platform seed — set Database:AutoMigrate=true or apply migrations.",
                pendingMigrations.Count());
            return;
        }

        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist. Skip platform seed.");
            return;
        }

        logger.LogInformation("Seeding platform operator {Email}.", PlatformAdminEmail);
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = PlatformAdminEmail,
            PasswordHash = passwordHasher.HashPassword(PlatformSeedPassword),
            Role = RoleConstants.PlatformOperator,
            OrganizationId = null,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
