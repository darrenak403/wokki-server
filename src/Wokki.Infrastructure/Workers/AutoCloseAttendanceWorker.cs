using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wokki.Application.Services.Attendance.Interfaces;

namespace Wokki.Infrastructure.Workers;

public sealed class AutoCloseAttendanceWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IAutoCloseAttendanceService>();
            await svc.BulkAutoCloseExpiredAsync(stoppingToken);
            await svc.BulkAutoCloseOTSessionsAsync(stoppingToken);
        }
    }
}
