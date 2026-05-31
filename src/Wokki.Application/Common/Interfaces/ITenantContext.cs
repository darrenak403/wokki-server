namespace Wokki.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid? OrganizationId { get; }
    bool IsPlatformOperator { get; }
    bool IsEnabled { get; }
}
