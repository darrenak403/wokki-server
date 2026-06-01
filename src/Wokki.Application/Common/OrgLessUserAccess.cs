using Wokki.Domain.Constants;

namespace Wokki.Application.Common;

public static class OrgLessUserAccess
{
    public static bool IsOrgLessUser(string? role, Guid? organizationId) =>
        string.Equals(role, RoleConstants.User, StringComparison.OrdinalIgnoreCase)
        && !organizationId.HasValue;

    public static bool IsAllowedPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/v1/organizations/directory", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/v1/org-join-requests", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
