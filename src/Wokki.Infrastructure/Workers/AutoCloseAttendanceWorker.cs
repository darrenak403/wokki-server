using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wokki.Application.Services.Attendance.Interfaces;

namespace Wokki.Infrastructure.Workers;

public sealed class AutoCloseAttendanceWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoCloseAttendanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAutoCloseAttendanceService>();
                await svc.BulkAutoCloseExpiredAsync(stoppingToken);
                await svc.BulkAutoCloseOTSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AutoCloseAttendanceWorker tick failed");
            }
        }
    }
}
