namespace Wokki.Domain.Repositories;

public sealed record PlatformStatsSnapshot(int OrganizationCount, int UserCount, int LocationCount, int EmployeeCount);

public sealed record OrgStatsSnapshot(
    int UserCount,
    int LocationCount,
    int DepartmentCount,
    int EmployeeCount,
    int ActiveLocationMembershipCount);
