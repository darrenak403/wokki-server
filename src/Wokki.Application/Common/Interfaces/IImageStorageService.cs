namespace Wokki.Application.Common.Interfaces;

public sealed record StoredImageResult(string Url, string PublicId);

public interface IImageStorageService
{
    bool IsConfigured { get; }

    Task<StoredImageResult> UploadPaymentQrAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Authenticated/signed delivery — not a publicly-fetchable URL, unlike UploadPaymentQrAsync.</summary>
    Task<StoredImageResult> UploadCheckInPhotoAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>Authenticated/signed delivery; overwrites any prior enrollment photo for this employee.</summary>
    Task<StoredImageResult> UploadFaceEnrollmentPhotoAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}
