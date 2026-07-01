namespace Wokki.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? OrganizationId { get; }
    bool IsPlatformOperator { get; }
    bool IsAuthenticated { get; }

    /// <summary>Caller's resolved IP, trustworthy only when UseForwardedHeaders is correctly configured.</summary>
    string? IpAddress { get; }
}
