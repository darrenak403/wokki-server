using Wokki.Application.Dtos.OvertimeRequest;
using Wokki.Domain.Enums;
using OvertimeRequestEntity = Wokki.Domain.Entities.OvertimeRequest;

namespace Wokki.Application.Mappings.OvertimeRequest;

public static class OvertimeRequestMapper
{
    public static OvertimeRequestResponse ToResponse(this OvertimeRequestEntity request)
    {
        int? elapsed = null;
        if (request.Status == OvertimeStatus.Pending && request.EndedAt is null)
            elapsed = (int)(DateTimeOffset.UtcNow - request.StartedAt).TotalMinutes;

        return new OvertimeRequestResponse(
            request.Id,
            request.ShiftAssignmentId,
            request.EmployeeId,
            request.Reason,
            request.StartedAt,
            request.EndedAt,
            request.OvertimeMinutes,
            elapsed,
            request.Status,
            request.ReviewedById,
            request.ReviewedAt,
            request.ReviewNote,
            request.CreatedAt);
    }
}
