using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Mappings.Attendance;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using AttendanceEntity = Wokki.Domain.Entities.AttendanceRecord;

namespace Wokki.Application.Services.Attendance.Implementations;

public sealed class AttendanceService(IUnitOfWork unitOfWork) : IAttendanceService
{
    public async Task<ApiResponse<AttendanceResponse>> ClockInAsync(
        Guid userId,
        ClockInRequest? request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoEmployeeProfile);

        var open = await unitOfWork.Attendance.GetOpenByEmployeeAsync(employee.Id, cancellationToken);
        if (open is not null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.OpenRecordExists);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assignments = await unitOfWork.ShiftAssignments.ListByEmployeeInDateRangeAsync(
            employee.Id,
            today,
            today,
            cancellationToken);

        if (assignments.Count == 0)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoAssignmentToday);

        Guid assignmentId;
        if (request?.AssignmentId is Guid requestedId)
        {
            if (assignments.All(a => a.Id != requestedId))
                return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoAssignmentToday);
            assignmentId = requestedId;
        }
        else
        {
            assignmentId = assignments[0].Id;
        }

        var record = new AttendanceEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            AssignmentId = assignmentId,
            ClockIn = DateTimeOffset.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Attendance.AddAsync(record, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AttendanceResponse>.SuccessResponse(record.ToResponse(), AppMessages.Attendance.ClockedIn);
    }

    public async Task<ApiResponse<AttendanceResponse>> ClockOutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoEmployeeProfile);

        var record = await unitOfWork.Attendance.GetOpenByEmployeeAsync(employee.Id, cancellationToken);
        if (record is null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoOpenRecord);

        record.ClockOut = DateTimeOffset.UtcNow;
        record.WorkedMinutes = ComputeWorkedMinutes(record.ClockIn, record.ClockOut.Value);
        unitOfWork.Attendance.Update(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AttendanceResponse>.SuccessResponse(record.ToResponse(), AppMessages.Attendance.ClockedOut);
    }

    public async Task<ApiResponse<PagedResponse<AttendanceResponse>>> ListAsync(
        AttendanceListRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.Attendance.ListAsync(
            page,
            pageSize,
            request.EmployeeId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return ApiResponse<PagedResponse<AttendanceResponse>>.SuccessPagedResponse(
            items.Select(i => i.ToResponse()),
            page,
            pageSize,
            total,
            AppMessages.Attendance.Listed);
    }

    public async Task<ApiResponse<IReadOnlyList<AttendanceResponse>>> ListMineAsync(
        Guid userId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId, cancellationToken);
        if (employee is null)
            return ApiResponse<IReadOnlyList<AttendanceResponse>>.FailureResponse(AppMessages.Attendance.NoEmployeeProfile);

        var items = await unitOfWork.Attendance.ListByEmployeeAsync(employee.Id, fromDate, toDate, cancellationToken);
        return ApiResponse<IReadOnlyList<AttendanceResponse>>.SuccessResponse(
            items.Select(i => i.ToResponse()).ToList(),
            AppMessages.Attendance.Listed);
    }

    public async Task<ApiResponse<AttendanceResponse>> AdjustAsync(
        Guid id,
        AdjustAttendanceRequest request,
        Guid adjustedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AdjustmentNote))
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.AdjustmentNoteRequired);

        if (request.ClockOut <= request.ClockIn)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Validation.Failed);

        var record = await unitOfWork.Attendance.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (record is null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NotFound);

        if (await IsPayPeriodLockedForRecordAsync(record, cancellationToken))
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.PeriodLocked);

        record.ClockIn = request.ClockIn;
        record.ClockOut = request.ClockOut;
        record.WorkedMinutes = ComputeWorkedMinutes(record.ClockIn, record.ClockOut.Value);
        record.AdjustedBy = adjustedByUserId;
        record.AdjustmentNote = request.AdjustmentNote.Trim();

        unitOfWork.Attendance.Update(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AttendanceResponse>.SuccessResponse(record.ToResponse(), AppMessages.Attendance.Adjusted);
    }

    private async Task<bool> IsPayPeriodLockedForRecordAsync(
        AttendanceEntity record,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(record.EmployeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return false;

        var clockDate = DateOnly.FromDateTime(record.ClockIn.UtcDateTime);
        var period = await unitOfWork.PayPeriods.GetContainingDateAsync(employee.DepartmentId, clockDate, cancellationToken);
        return period?.Status == PayPeriodStatus.Locked;
    }

    private static int ComputeWorkedMinutes(DateTimeOffset clockIn, DateTimeOffset clockOut)
    {
        var minutes = (int)Math.Round((clockOut - clockIn).TotalMinutes, MidpointRounding.AwayFromZero);
        return Math.Max(0, minutes);
    }
}
