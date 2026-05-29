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

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}
