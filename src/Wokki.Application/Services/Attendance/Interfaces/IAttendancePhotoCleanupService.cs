namespace Wokki.Application.Services.Attendance.Interfaces;

/// <summary>Purges old check-in selfie photos per the configured retention window. Never touches the face-enrollment reference photo.</summary>
public interface IAttendancePhotoCleanupService
{
    Task PurgeExpiredPhotosAsync(CancellationToken cancellationToken = default);
}
