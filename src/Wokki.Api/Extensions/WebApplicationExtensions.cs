using Microsoft.EntityFrameworkCore;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        var autoMigrate = app.Configuration.GetValue<bool?>("Database:AutoMigrate")
                          ?? !app.Environment.IsProduction();

        if (!autoMigrate)
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied.");
    }
}
