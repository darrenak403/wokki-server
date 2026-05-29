using Wokki.Application.Dtos.Schedule;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Schedules;

public static class ScheduleMapper
{
    public static ScheduleResponse ToResponse(this Schedule schedule) =>
        new(
            schedule.Id,
            schedule.DepartmentId,
            schedule.WeekStartDate,
            schedule.Status,
            schedule.CreatedBy,
            schedule.PublishedAt,
            schedule.CreatedAt);

    public static Schedule ToEntity(this CreateScheduleRequest request, Guid createdBy, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DepartmentId = request.DepartmentId,
            WeekStartDate = request.WeekStartDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this Schedule schedule, UpdateScheduleRequest request)
    {
        schedule.DepartmentId = request.DepartmentId;
        schedule.WeekStartDate = request.WeekStartDate;
    }

    public static ShiftAssignment ToAssignmentEntity(this CreateShiftAssignmentRequest request, Guid scheduleId, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ScheduleId = scheduleId,
            ShiftDefinitionId = request.ShiftDefinitionId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };
}
