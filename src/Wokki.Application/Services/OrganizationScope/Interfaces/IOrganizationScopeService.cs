namespace Wokki.Application.Services.OrganizationScope.Interfaces;

public interface IOrganizationScopeService
{
    Guid? GetCurrentOrganizationId();
    bool IsPlatformOperator { get; }
    bool IsSameOrganization(Guid organizationId);
    void EnsureOrganizationUser();
    void EnsureSameOrganization(Guid organizationId);
    Guid RequireOrganizationId();
}
