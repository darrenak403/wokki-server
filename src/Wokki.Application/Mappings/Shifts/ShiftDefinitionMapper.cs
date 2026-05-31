using Wokki.Application.Dtos.Shift;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Shifts;

public static class ShiftDefinitionMapper
{
    public static ShiftDefinitionResponse ToResponse(this ShiftDefinition shift) =>
        new(
            shift.Id,
            shift.LocationId,
            shift.DepartmentId,
            shift.Name,
            shift.StartTime,
            shift.EndTime,
            shift.RequiredRole,
            shift.Color,
            shift.IsActive,
            shift.CreatedAt);

    public static ShiftDefinition ToEntity(this CreateShiftDefinitionRequest request, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LocationId = request.LocationId,
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequiredRole = request.RequiredRole.Trim(),
            Color = request.Color.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this ShiftDefinition shift, UpdateShiftDefinitionRequest request)
    {
        shift.Name = request.Name.Trim();
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.RequiredRole = request.RequiredRole.Trim();
        shift.Color = request.Color.Trim();
        shift.IsActive = request.IsActive;
    }

    public static string DedupeKey(ShiftDefinition shift) =>
        $"{shift.Name.Trim()}|{shift.StartTime}|{shift.EndTime}";

    public static ShiftDefinition CloneToDepartment(ShiftDefinition source, Guid targetDepartmentId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = source.OrganizationId,
            LocationId = source.LocationId,
            DepartmentId = targetDepartmentId,
            Name = source.Name,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            RequiredRole = source.RequiredRole,
            Color = source.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
}
