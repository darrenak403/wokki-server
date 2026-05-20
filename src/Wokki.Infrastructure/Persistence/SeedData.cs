using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        if (await context.Users.AnyAsync())
            return;

        logger.LogInformation("Seeding default users...");

        context.Users.AddRange(
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@wokki.local",
                PasswordHash = "admin123",
                Role = RoleConstants.Admin
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "user@wokki.local",
                PasswordHash = "user123",
                Role = RoleConstants.User
            });

        await context.SaveChangesAsync();
    }
}
