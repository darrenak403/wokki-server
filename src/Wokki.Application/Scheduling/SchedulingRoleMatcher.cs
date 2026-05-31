using Wokki.Domain.Constants;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class SchedulingRoleMatcher
{
    public static bool Matches(Employee employee, ShiftDefinition shift)
    {
        if (string.IsNullOrWhiteSpace(shift.RequiredRole))
            return true;

        var required = shift.RequiredRole.Trim();
        if (string.Equals(required, RoleConstants.User, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(required, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            return true;

        var position = employee.Position?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(position))
            return false;

        return string.Equals(position, required, StringComparison.OrdinalIgnoreCase);
    }
}
