using Wokki.Application.Dtos.SwapPost;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using DepartmentEntity = Wokki.Domain.Entities.Department;

namespace Wokki.Application.Mappings.SwapPosts;

public static class SwapPostMapper
{
    public static SwapPost ToEntity(
        CreateSwapPostRequest request,
        Employee author,
        ShiftAssignment authorAssignment,
        Schedule schedule,
        DepartmentEntity department) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = author.OrganizationId,
            ScheduleId = schedule.Id,
            DepartmentId = schedule.DepartmentId,
            LocationId = department.LocationId,
            AuthorEmployeeId = author.Id,
            AuthorAssignmentId = authorAssignment.Id,
            Type = request.Type,
            Status = SwapPostStatus.Pending,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
