namespace Wokki.Application.Dtos.Organization;

public sealed record OrgStatsResponse(
    Guid OrganizationId,
    int UserCount,
    int LocationCount,
    int DepartmentCount,
    int EmployeeCount,
    int ActiveLocationMembershipCount);
