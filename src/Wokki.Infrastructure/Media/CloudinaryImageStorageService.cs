using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Media;

public sealed class CloudinaryImageStorageService(
    IOptions<CloudinarySettings> options,
    ILogger<CloudinaryImageStorageService> logger) : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<StoredImageResult> UploadPaymentQrAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Cloudinary is not configured.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Unsupported image type. Use JPEG, PNG, or WebP.");

        var cloudinary = CreateClient(settings);
        var publicId = BuildPaymentQrPublicId(organizationId, employeeId);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true,
        };

        var result = await cloudinary.UploadAsync(uploadParams);
        if (result.Error is not null)
        {
            logger.LogWarning("Cloudinary upload failed: {Message}", result.Error.Message);
            throw new InvalidOperationException(result.Error.Message);
        }

        return new StoredImageResult(result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty, result.PublicId);
    }

    public Task<StoredImageResult> UploadCheckInPhotoAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default) =>
        UploadAuthenticatedAsync(
            content,
            fileName,
            contentType,
            BuildCheckInPhotoPublicId(organizationId, employeeId, attendanceRecordId));

    public Task<StoredImageResult> UploadFaceEnrollmentPhotoAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        UploadAuthenticatedAsync(
            content,
            fileName,
            contentType,
            BuildFaceEnrollmentPublicId(organizationId, employeeId));

    private async Task<StoredImageResult> UploadAuthenticatedAsync(
        Stream content,
        string fileName,
        string contentType,
        string publicId)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Cloudinary is not configured.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Unsupported image type. Use JPEG, PNG, or WebP.");

        var cloudinary = CreateClient(settings);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Type = "authenticated",
            Overwrite = true,
            Invalidate = true,
        };

        var result = await cloudinary.UploadAsync(uploadParams);
        if (result.Error is not null)
        {
            logger.LogWarning("Cloudinary upload failed: {Message}", result.Error.Message);
            throw new InvalidOperationException(result.Error.Message);
        }

        var signedUrl = cloudinary.Api.UrlImgUp
            .ResourceType("image")
            .Type("authenticated")
            .Secure(true)
            .Signed(true)
            .BuildUrl(result.PublicId);

        return new StoredImageResult(signedUrl, result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(publicId))
            return;

        var cloudinary = CreateClient(settings);
        var deletionParams = new DeletionParams(publicId)
        {
            // Check-in/face-enrollment photos upload with Type=authenticated; destroy must match or it silently no-ops.
            Type = IsAuthenticatedDeliveryPublicId(publicId) ? "authenticated" : "upload",
        };
        var result = await cloudinary.DestroyAsync(deletionParams);
        if (result.Error is not null)
            logger.LogWarning("Cloudinary delete failed for {PublicId}: {Message}", publicId, result.Error.Message);
    }

    private static bool IsAuthenticatedDeliveryPublicId(string publicId) =>
        publicId.Contains("/checkins/", StringComparison.Ordinal)
        || publicId.Contains("/face-enrollment", StringComparison.Ordinal);

    internal static string BuildPaymentQrPublicId(Guid organizationId, Guid employeeId) =>
        $"wokki/orgs/{organizationId}/employees/{employeeId}/payment-qr";

    internal static string BuildCheckInPhotoPublicId(Guid organizationId, Guid employeeId, Guid attendanceRecordId) =>
        $"wokki/orgs/{organizationId}/employees/{employeeId}/checkins/{attendanceRecordId}";

    internal static string BuildFaceEnrollmentPublicId(Guid organizationId, Guid employeeId) =>
        $"wokki/orgs/{organizationId}/employees/{employeeId}/face-enrollment";

    private static Cloudinary CreateClient(CloudinarySettings settings) =>
        new(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
}
