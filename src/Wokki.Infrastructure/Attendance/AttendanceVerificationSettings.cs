using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Attendance;

public sealed class AttendanceVerificationSettings : IAttendanceVerificationSettings
{
    public const string SectionName = "AttendanceVerification";

    public double DefaultGeofenceRadiusMeters { get; set; } = 150;
    public int PhotoRetentionDays { get; set; } = 30;
}
