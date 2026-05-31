using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsPlatformOperator { get; private set; }
    public bool IsEnabled { get; private set; }

    public void SetTenant(Guid? tenantId, bool enabled = false)
    {
        TenantId = tenantId;
        OrganizationId = tenantId;
        IsEnabled = enabled;
    }

    public void SetOrganization(Guid? organizationId, bool isPlatformOperator, bool enabled = true)
    {
        OrganizationId = organizationId;
        TenantId = organizationId;
        IsPlatformOperator = isPlatformOperator;
        IsEnabled = enabled;
    }
}
