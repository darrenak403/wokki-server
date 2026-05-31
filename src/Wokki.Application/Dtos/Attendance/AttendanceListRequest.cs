using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Attendance;

public sealed record AttendanceListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    AttendanceMode? Mode = null,
    bool? PayrollEligible = null);
