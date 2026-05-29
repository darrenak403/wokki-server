using Wokki.Application.Dtos.Department;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Departments;

public static class DepartmentMapper
{
    public static DepartmentResponse ToResponse(this Department department) =>
        new(department.Id, department.LocationId, department.Name, department.IsActive, department.CreatedAt);

    public static Department ToEntity(this CreateDepartmentRequest request, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LocationId = request.LocationId,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this Department department, UpdateDepartmentRequest request)
    {
        department.Name = request.Name.Trim();
        department.IsActive = request.IsActive;
    }
}
