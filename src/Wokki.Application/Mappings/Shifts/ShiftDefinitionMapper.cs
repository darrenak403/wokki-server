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
            shift.MaxStaffPerSlot,
            shift.Color,
            shift.IsActive,
            shift.CreatedAt);

    public static ShiftDefinition ToEntity(this CreateShiftDefinitionRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            LocationId = request.LocationId,
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequiredRole = request.RequiredRole.Trim(),
            MaxStaffPerSlot = request.MaxStaffPerSlot < 1 ? 1 : request.MaxStaffPerSlot,
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
        shift.MaxStaffPerSlot = request.MaxStaffPerSlot < 1 ? 1 : request.MaxStaffPerSlot;
        shift.Color = request.Color.Trim();
        shift.IsActive = request.IsActive;
    }
}
