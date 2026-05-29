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

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(publicId))
            return;

        var cloudinary = CreateClient(settings);
        var result = await cloudinary.DestroyAsync(new DeletionParams(publicId));
        if (result.Error is not null)
            logger.LogWarning("Cloudinary delete failed for {PublicId}: {Message}", publicId, result.Error.Message);
    }

    internal static string BuildPaymentQrPublicId(Guid organizationId, Guid employeeId) =>
        $"wokki/orgs/{organizationId}/employees/{employeeId}/payment-qr";

    private static Cloudinary CreateClient(CloudinarySettings settings) =>
        new(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
}
