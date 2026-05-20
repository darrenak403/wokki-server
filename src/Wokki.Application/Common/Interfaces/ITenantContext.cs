namespace Wokki.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsEnabled { get; }
}
