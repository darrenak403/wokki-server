namespace Wokki.Application.Common.Interfaces;

public interface IAttendanceVerificationSettings
{
    double DefaultGeofenceRadiusMeters { get; }
    int PhotoRetentionDays { get; }
}
