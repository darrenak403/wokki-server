using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public bool IsEnabled { get; private set; }

    public void SetTenant(Guid? tenantId, bool enabled = false)
    {
        TenantId = tenantId;
        IsEnabled = enabled;
    }
}
