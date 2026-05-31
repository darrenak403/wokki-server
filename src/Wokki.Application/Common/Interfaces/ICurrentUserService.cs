namespace Wokki.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? OrganizationId { get; }
    bool IsPlatformOperator { get; }
    bool IsAuthenticated { get; }
}
