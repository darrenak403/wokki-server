using Wokki.Application.Dtos.OvertimeRequest;
using Wokki.Application.Mappings.OvertimeRequest;
using Wokki.Application.Services.OvertimeRequest.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.OvertimeRequest.Implementations;

public sealed class OvertimeRequestService(IUnitOfWork unitOfWork) : IOvertimeRequestService
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
        if (request is null)
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
        CancellationToken ct = default)
    {
        IReadOnlyList<Guid>? allowedEmployeeIds = null;
        if (!isAdmin)
        {
            var reviewer = await unitOfWork.Employees.GetByUserIdAsync(reviewerUserId, ct);
            if (reviewer is null)
                return ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

            var reviewerMemberships = await unitOfWork.EmployeeDepartmentMemberships.ListByEmployeeAsync(reviewer.Id, ct);
            var reviewerDeptIds = reviewerMemberships
                .Select(m => m.DepartmentId)
                .Append(reviewer.DepartmentId)
                .Distinct()
                .ToList();

            if (departmentId.HasValue && !reviewerDeptIds.Contains(departmentId.Value))
                return ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

            allowedEmployeeIds = await unitOfWork.Employees.GetIdsByDepartmentIdsAsync(reviewerDeptIds, ct);
        }

        var (items, total) = await unitOfWork.OvertimeRequests.ListPendingApprovalAsync(
            allowedEmployeeIds, departmentId, page, pageSize, ct);

        return ApiResponse<PagedResponse<OvertimeRequestResponse>>.SuccessPagedResponse(
            items.Select(r => r.ToResponse()).ToList(),
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
        if (otRequest is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NotFound);

        if (otRequest.Status is not OvertimeStatus.PendingApproval and not OvertimeStatus.AutoClosed)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (otRequest.Status == OvertimeStatus.Approved)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        var reviewer = await unitOfWork.Employees.GetByUserIdAsync(reviewerUserId, ct);
        if (reviewer is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

        if (!isAdmin && !await IsReviewerAuthorizedAsync(reviewer.Id, otRequest.EmployeeId, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        if (await IsPayPeriodLockedAsync(otRequest, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.PeriodLocked);

        var attendanceRecord = await unitOfWork.Attendance.GetOpenByEmployeeAsync(otRequest.EmployeeId, ct)
            ?? await GetAttendanceRecordForAssignmentAsync(otRequest.ShiftAssignmentId, otRequest.EmployeeId, ct);

        otRequest.Status = OvertimeStatus.Approved;
        otRequest.ReviewedById = reviewer.Id;
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
        if (otRequest is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NotFound);

        if (otRequest.Status is not OvertimeStatus.PendingApproval and not OvertimeStatus.AutoClosed)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        if (otRequest.Status == OvertimeStatus.Rejected)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.InvalidTransition);

        var reviewer = await unitOfWork.Employees.GetByUserIdAsync(reviewerUserId, ct);
        if (reviewer is null)
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.NoEmployeeProfile);

        if (!isAdmin && !await IsReviewerAuthorizedAsync(reviewer.Id, otRequest.EmployeeId, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.Forbidden);

        if (await IsPayPeriodLockedAsync(otRequest, ct))
            return ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.OvertimeRequest.PeriodLocked);

        otRequest.Status = OvertimeStatus.Rejected;
        otRequest.ReviewedById = reviewer.Id;
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

    private async Task<bool> IsReviewerAuthorizedAsync(Guid reviewerEmployeeId, Guid requestEmployeeId, CancellationToken ct)
    {
        var reviewer = await unitOfWork.Employees.GetByIdAsync(reviewerEmployeeId, cancellationToken: ct);
        if (reviewer is null)
            return false;

        var reviewerMemberships = await unitOfWork.EmployeeDepartmentMemberships.ListByEmployeeAsync(reviewerEmployeeId, ct);
        var reviewerDeptIds = reviewerMemberships
            .Select(m => m.DepartmentId)
            .Append(reviewer.DepartmentId)
            .Distinct()
            .ToList();

        var employeeIdsInReviewerDepts = await unitOfWork.Employees
            .GetIdsByDepartmentIdsAsync(reviewerDeptIds, cancellationToken: ct);
        return employeeIdsInReviewerDepts.Contains(requestEmployeeId);
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
