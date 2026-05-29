using Wokki.Application.Dtos.OvertimeRequest;
using Wokki.Application.Mappings.OvertimeRequest;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.OvertimeRequest.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.OvertimeRequest.Implementations;

public sealed class OvertimeRequestService(IUnitOfWork unitOfWork, IOrganizationScopeService organizationScope) : IOvertimeRequestService
{
    public async Task<ApiResponse<OvertimeRequestResponse>> SubmitAsync(
        Guid userId,
        SubmitOvertimeRequestDto dto,
        CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

        if (employee.TerminatedAt is not null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(dto.ShiftAssignmentId, cancellationToken: ct);
        if (assignment is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.AssignmentNotFound);

        if (assignment.EmployeeId != employee.Id)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        if (assignment.OrganizationId != employee.OrganizationId)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.AssignmentNotFound);

        var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: ct);
        if (schedule?.Status != ScheduleStatus.Published)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.ScheduleNotPublished);

        var shiftEndUtc = await ResolveShiftEndUtcAsync(assignment, ct);
        if (shiftEndUtc is null || DateTimeOffset.UtcNow <= shiftEndUtc.Value)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.ShiftNotEnded);

        var existing = await unitOfWork.OvertimeRequests.GetActiveByShiftAndEmployeeAsync(
            dto.ShiftAssignmentId, employee.Id, ct);
        if (existing is not null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.ActiveOTExists);

        var request = new Domain.Entities.OvertimeRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = employee.OrganizationId,
            ShiftAssignmentId = dto.ShiftAssignmentId,
            EmployeeId = employee.Id,
            Reason = dto.Reason.Trim(),
            StartedAt = DateTimeOffset.UtcNow,
            Status = OvertimeStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await unitOfWork.OvertimeRequests.AddAsync(request, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<OvertimeRequestResponse>.SuccessResponse(request.ToResponse(), AppMessages.OvertimeRequest.Submitted);
    }

    public async Task<ApiResponse<OvertimeRequestResponse>> ClockOutOTAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

        var request = await unitOfWork.OvertimeRequests.GetByIdAsync(id, track: true, cancellationToken: ct);
        if (request is null || !organizationScope.IsSameOrganization(request.OrganizationId))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NotFound);

        if (request.EmployeeId != employee.Id)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        if (request.Status != OvertimeStatus.Pending)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.AlreadyClosed);

        request.EndedAt = DateTimeOffset.UtcNow;
        request.OvertimeMinutes = (int)Math.Max(0, (request.EndedAt.Value - request.StartedAt).TotalMinutes);
        request.Status = OvertimeStatus.PendingApproval;
        unitOfWork.OvertimeRequests.Update(request);
        await unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<OvertimeRequestResponse>.SuccessResponse(request.ToResponse(), AppMessages.OvertimeRequest.ClockedOut);
    }

    public async Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListMyAsync(
        Guid userId,
        Guid? shiftAssignmentId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
            return ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

        var (items, total) = await unitOfWork.OvertimeRequests.ListByEmployeeAsync(
            employee.Id, shiftAssignmentId, page, pageSize, ct);

        return ApiResponse<PagedResponse<OvertimeRequestResponse>>.SuccessPagedResponse(
            items.Select(r => r.ToResponse()).ToList(),
            page, pageSize, total,
            AppMessages.OvertimeRequest.Listed);
    }

    public async Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListPendingAsync(
        Guid reviewerUserId,
        bool isAdmin,
        Guid? departmentId,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken ct = default)
    {
        var scopedLocationIds = isAdmin ? null : locationIds ?? new HashSet<Guid>();
        if (departmentId.HasValue && !await IsDepartmentInScopeAsync(departmentId.Value, scopedLocationIds, ct))
            return ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        var (items, total) = await unitOfWork.OvertimeRequests.ListPendingApprovalAsync(
            null, departmentId, page, pageSize, scopedLocationIds, ct);

        return ApiResponse<PagedResponse<OvertimeRequestResponse>>.SuccessPagedResponse(
            items.Select(r => r.ToResponse()).ToList(),
            page, pageSize, total,
            AppMessages.OvertimeRequest.Listed);
    }

    public async Task<ApiResponse<PagedResponse<OvertimeRequestResponse>>> ListAllAsync(
        Guid reviewerUserId,
        bool isAdmin,
        Guid? departmentId,
        int month,
        int year,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken ct = default)
    {
        var scopedLocationIds = isAdmin ? null : locationIds ?? new HashSet<Guid>();
        if (departmentId.HasValue && !await IsDepartmentInScopeAsync(departmentId.Value, scopedLocationIds, ct))
            return ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        var (items, total) = await unitOfWork.OvertimeRequests.ListAllByDepartmentAsync(
            null, departmentId, month, year, page, pageSize, scopedLocationIds, ct);

        return ApiResponse<PagedResponse<OvertimeRequestResponse>>.SuccessPagedResponse(
            items.Select(x => x.Request.ToResponse(x.EmployeeFirstName, x.EmployeeLastName, x.ShiftName, x.ScheduledDate)).ToList(),
            page, pageSize, total,
            AppMessages.OvertimeRequest.Listed);
    }

    public async Task<ApiResponse<OvertimeRequestResponse>> ApproveAsync(
        Guid id,
        Guid reviewerUserId,
        bool isAdmin,
        string? note,
        CancellationToken ct = default)
    {
        var otRequest = await unitOfWork.OvertimeRequests.GetByIdAsync(id, track: true, cancellationToken: ct);
        if (otRequest is null || !organizationScope.IsSameOrganization(otRequest.OrganizationId))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NotFound);

        if (otRequest.Status is not OvertimeStatus.PendingApproval and not OvertimeStatus.AutoClosed)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (otRequest.Status == OvertimeStatus.Approved)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (await IsPayPeriodLockedAsync(otRequest, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.PeriodLocked);

        var attendanceRecord = await unitOfWork.Attendance.GetOpenByEmployeeAsync(otRequest.EmployeeId, ct)
            ?? await GetAttendanceRecordForAssignmentAsync(otRequest.ShiftAssignmentId, otRequest.EmployeeId, ct);

        otRequest.Status = OvertimeStatus.Approved;
        otRequest.ReviewedById = reviewerUserId;
        otRequest.ReviewedAt = DateTimeOffset.UtcNow;
        otRequest.ReviewNote = note;
        unitOfWork.OvertimeRequests.Update(otRequest);

        if (attendanceRecord is not null)
        {
            attendanceRecord.ApprovedOvertimeMinutes += otRequest.OvertimeMinutes ?? 0;
            unitOfWork.Attendance.Update(attendanceRecord);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<OvertimeRequestResponse>.SuccessResponse(otRequest.ToResponse(), AppMessages.OvertimeRequest.Approved);
    }

    public async Task<ApiResponse<OvertimeRequestResponse>> RejectAsync(
        Guid id,
        Guid reviewerUserId,
        bool isAdmin,
        string? note,
        CancellationToken ct = default)
    {
        var otRequest = await unitOfWork.OvertimeRequests.GetByIdAsync(id, track: true, cancellationToken: ct);
        if (otRequest is null || !organizationScope.IsSameOrganization(otRequest.OrganizationId))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NotFound);

        if (otRequest.Status is not OvertimeStatus.PendingApproval and not OvertimeStatus.AutoClosed)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (otRequest.Status == OvertimeStatus.Rejected)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (await IsPayPeriodLockedAsync(otRequest, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.PeriodLocked);

        otRequest.Status = OvertimeStatus.Rejected;
        otRequest.ReviewedById = reviewerUserId;
        otRequest.ReviewedAt = DateTimeOffset.UtcNow;
        otRequest.ReviewNote = note;
        unitOfWork.OvertimeRequests.Update(otRequest);

        await unitOfWork.SaveChangesAsync(ct);
        return ApiResponse<OvertimeRequestResponse>.SuccessResponse(otRequest.ToResponse(), AppMessages.OvertimeRequest.Rejected);
    }

    private async Task<DateTimeOffset?> ResolveShiftEndUtcAsync(ShiftAssignment assignment, CancellationToken ct)
    {
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: ct);
        if (shift is null)
            return null;

        var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: ct);
        var department = schedule is null ? null : await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
        var location = department is null ? null : await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: ct);

        var tz = ResolveTimeZone(location?.TimeZone ?? "UTC");
        var shiftEndLocal = assignment.Date.ToDateTime(shift.EndTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(shiftEndLocal, tz));
    }

    private async Task<bool> IsDepartmentInScopeAsync(
        Guid departmentId,
        IReadOnlySet<Guid>? locationIds,
        CancellationToken ct)
    {
        if (locationIds is null)
            return true;

        var department = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: ct);
        return department is null || locationIds.Contains(department.LocationId);
    }

    private async Task<bool> IsPayPeriodLockedAsync(Domain.Entities.OvertimeRequest request, CancellationToken ct)
    {
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(request.ShiftAssignmentId, cancellationToken: ct);
        if (assignment is null)
            return false;

        var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken: ct);
        if (employee is null)
            return false;

        var period = await unitOfWork.PayPeriods.GetContainingDateAsync(employee.DepartmentId, assignment.Date, ct);
        return period?.Status == PayPeriodStatus.Locked;
    }

    private Task<AttendanceRecord?> GetAttendanceRecordForAssignmentAsync(Guid assignmentId, Guid employeeId, CancellationToken ct) =>
        unitOfWork.Attendance.GetByAssignmentIdAsync(assignmentId, ct);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
