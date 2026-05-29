namespace Wokki.Application.Dtos.Platform;

public sealed record PlatformStatsResponse(
    int OrganizationCount,
    int UserCount,
    int LocationCount,
    int EmployeeCount);
