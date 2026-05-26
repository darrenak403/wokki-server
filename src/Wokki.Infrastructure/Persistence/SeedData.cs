using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Guid DefaultLocationId = CoffeeShopSeedIds.LocationId;
    public static readonly Guid DefaultDepartmentId = CoffeeShopSeedIds.DepartmentBarId;
    /// <summary>Transient holder for atomic swap of two assignments (never scheduled on a shift).</summary>
    public static readonly Guid SwapHoldEmployeeId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist. Skip seeding Wokki Coffê demo data.");
        }
        else
        {
            logger.LogInformation("Seeding Wokki Coffê demo data...");
            await CoffeeShopSeedBuilder.SeedAsync(context, passwordHasher, logger);
        }

        // After seed (or on existing DB): SwapHold is required for assignment swaps (FK-safe three-step update).
        await EnsureSwapHoldEmployeeAsync(context, logger);

        // Seed Active memberships for all existing employees (idempotent migration).
        await EnsureLocationMembershipsAsync(context, logger);
    }

    private static async Task EnsureSwapHoldEmployeeAsync(
        AppDbContext context,
        ILogger logger)
    {
        if (await context.Employees.AnyAsync(e => e.Id == SwapHoldEmployeeId))
            return;

        var departmentId = await context.Departments
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        if (departmentId == Guid.Empty)
        {
            logger.LogWarning("Swap hold employee not created: no department in database.");
            return;
        }

        var adminUserId = await context.Users
            .Where(u => u.Role == RoleConstants.Admin)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (adminUserId == Guid.Empty)
        {
            logger.LogWarning("Swap hold employee not created: no admin user.");
            return;
        }

        context.Employees.Add(new Employee
        {
            Id = SwapHoldEmployeeId,
            UserId = adminUserId,
            FirstName = "Swap",
            LastName = "Hold",
            Phone = "",
            Position = "System",
            HourlyRate = 0m,
            DepartmentId = departmentId
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Ensured swap hold employee {EmployeeId}.", SwapHoldEmployeeId);
    }

    private static async Task EnsureLocationMembershipsAsync(AppDbContext context, ILogger logger)
    {
        var coveredEmployeeIds = await context.LocationMemberships
            .Where(m => m.Status == LocationMembershipStatus.Active || m.Status == LocationMembershipStatus.Pending)
            .Select(m => m.EmployeeId)
            .Distinct()
            .ToListAsync();

        var uncovered = await context.Employees
            .Where(e => e.Id != SwapHoldEmployeeId && !coveredEmployeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.DepartmentId })
            .ToListAsync();

        if (uncovered.Count == 0)
            return;

        var deptIds = uncovered.Select(e => e.DepartmentId).Distinct().ToList();
        var deptLocationMap = await context.Departments
            .Where(d => deptIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.LocationId);

        var now = DateTime.UtcNow;
        var inserted = 0;

        foreach (var emp in uncovered)
        {
            if (!deptLocationMap.TryGetValue(emp.DepartmentId, out var locationId))
            {
                logger.LogWarning("Membership seed: employee {EmployeeId} has no resolvable location — skipped.", emp.Id);
                continue;
            }

            context.LocationMemberships.Add(new LocationMembership
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                LocationId = locationId,
                Status = LocationMembershipStatus.Active,
                RequestedAt = now,
                ReviewedAt = now,
                ReviewedById = null
            });
            inserted++;
        }

        if (inserted == 0)
            return;

        await context.SaveChangesAsync();
        logger.LogInformation("Location membership seed: {Count} membership(s) seeded.", inserted);
    }
}
