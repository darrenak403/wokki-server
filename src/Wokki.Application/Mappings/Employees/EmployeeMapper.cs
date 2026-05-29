using Wokki.Application.Dtos.Employee;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Employees;

public static class EmployeeMapper
{
    public static EmployeeResponse ToResponse(
        this Employee employee,
        User user,
        Department? department = null,
        Location? location = null) =>
        new(
            employee.Id,
            employee.UserId,
            user.Email,
            user.Role,
            employee.FirstName,
            employee.LastName,
            employee.Phone,
            employee.Position,
            employee.HourlyRate,
            employee.DepartmentId,
            department?.Name,
            location?.Id,
            location?.Name,
            employee.EmployedAt,
            employee.TerminatedAt,
            employee.CreatedAt);

    public static Employee ToEntity(this CreateEmployeeRequest request, Guid userId, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            Position = request.Position.Trim(),
            HourlyRate = request.HourlyRate,
            DepartmentId = request.DepartmentId,
            EmployedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this Employee employee, UpdateEmployeeRequest request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone?.Trim() ?? string.Empty;
        employee.Position = request.Position.Trim();
        employee.HourlyRate = request.HourlyRate;
        employee.DepartmentId = request.DepartmentId;
    }
}
