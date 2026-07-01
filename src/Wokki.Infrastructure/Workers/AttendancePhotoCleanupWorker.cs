using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wokki.Application.Services.Attendance.Interfaces;

namespace Wokki.Infrastructure.Workers;

public sealed class AttendancePhotoCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AttendancePhotoCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAttendancePhotoCleanupService>();
                await svc.PurgeExpiredPhotosAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AttendancePhotoCleanupWorker tick failed");
            }
        }
    }
}
