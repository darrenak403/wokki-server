namespace Wokki.Application.Dtos.Attendance;

/// <summary>
/// Lat/lng, photo, and face-match fields are optional advisory verification inputs — never required,
/// regardless of whether the Location has coordinates/IP configured (camera/GPS permission may be denied).
/// </summary>
public sealed record ClockInRequest(
    Guid? AssignmentId = null,
    double? Latitude = null,
    double? Longitude = null,
    string? PhotoBase64 = null,
    string? PhotoContentType = null,
    string? FaceEmbeddingJson = null,
    bool? FaceMatch = null);
