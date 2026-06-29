using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Attendance.Implementations;

public sealed class AttendancePhotoCleanupService(
    IUnitOfWork unitOfWork,
    IImageStorageService imageStorage,
    IAttendanceVerificationSettings verificationSettings,
    ILogger<AttendancePhotoCleanupService> logger) : IAttendancePhotoCleanupService
{
    private const int BatchSize = 200;

    public async Task PurgeExpiredPhotosAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-verificationSettings.PhotoRetentionDays);
        var stalePhotos = await unitOfWork.Attendance.ListWithStalePhotosAsync(cutoff, BatchSize, cancellationToken);

        foreach (var record in stalePhotos)
        {
            if (string.IsNullOrWhiteSpace(record.ClockInPhotoPublicId))
                continue;

            await imageStorage.DeleteAsync(record.ClockInPhotoPublicId, cancellationToken);
            record.ClockInPhotoUrl = null;
            record.ClockInPhotoPublicId = null;
            unitOfWork.Attendance.Update(record);

            // Save per-record: the Cloudinary asset above is already irreversibly deleted, so one record's
            // concurrent-update conflict must not roll back the DB cleanup of every other record in this batch.
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clear photo columns for AttendanceRecord {RecordId} after Cloudinary delete", record.Id);
            }
        }
    }
}
