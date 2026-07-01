using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Mappings.Attendance;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using AttendanceEntity = Wokki.Domain.Entities.AttendanceRecord;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;
using ShiftAssignmentEntity = Wokki.Domain.Entities.ShiftAssignment;
using ShiftDefinitionEntity = Wokki.Domain.Entities.ShiftDefinition;

namespace Wokki.Application.Services.Attendance.Implementations;

public sealed class AttendanceService(
    IUnitOfWork unitOfWork,
    IAutoCloseAttendanceService autoCloseService,
    IOrganizationScopeService organizationScope,
    IPlatformActivityRecorder platformActivityRecorder,
    ICurrentUserService currentUser,
    IImageStorageService imageStorage,
    IAttendanceVerificationSettings verificationSettings) : IAttendanceService
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
        {
            await autoCloseService.AutoCloseIfExpiredAsync(open, cancellationToken);
            if (open.ClockOut is null)
                return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.OpenRecordExists);
        }

        if (!employee.DepartmentId.HasValue)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoEmployeeProfile);

        return await ClockInAssignmentAsync(employee, request, cancellationToken);
    }

    private async Task<ApiResponse<AttendanceResponse>> ClockInAssignmentAsync(
        EmployeeEntity employee,
        ClockInRequest? request,
        CancellationToken cancellationToken)
    {
        var employeeTimeZone = await ResolveEmployeeTimeZoneAsync(employee.DepartmentId!.Value, cancellationToken);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, employeeTimeZone));
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

        var clockContext = await LoadClockContextAsync(assignmentId, cancellationToken);
        if (clockContext is null)
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.NoAssignmentToday);

        if (IsAfterShiftEnd(clockContext.Assignment.Date, clockContext.Shift.EndTime, clockContext.TimeZone))
            return ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Attendance.AssignmentWindowPassed);

        var record = new AttendanceEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = employee.OrganizationId,
            EmployeeId = employee.Id,
            AssignmentId = assignmentId,
            Mode = AttendanceMode.Assignment,
            PayrollEligible = true,
            ClockIn = DateTimeOffset.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await ApplyVerificationAsync(employee, record, clockContext.Location, request, cancellationToken);

        await unitOfWork.Attendance.AddAsync(record, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await platformActivityRecorder.TryRecordAsync(
            employee.OrganizationId,
            employee.UserId,
            "attendance.clock_in",
            "AttendanceRecord",
            record.Id,
            cancellationToken);

        return ApiResponse<AttendanceResponse>.SuccessResponse(
            await BuildAttendanceResponseAsync(record, cancellationToken),
            AppMessages.Attendance.ClockedIn);
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

        await autoCloseService.AutoCloseIfExpiredAsync(record, cancellationToken);

        if (record.ClockOut is not null)
            return ApiResponse<AttendanceResponse>.SuccessResponse(
                await BuildAttendanceResponseAsync(record, cancellationToken),
                AppMessages.Attendance.ClockedOut);

        record.ClockOut = DateTimeOffset.UtcNow;
        record.WorkedMinutes = ComputeWorkedMinutes(record.ClockIn, record.ClockOut.Value);
        unitOfWork.Attendance.Update(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await platformActivityRecorder.TryRecordAsync(
            record.OrganizationId,
            employee.UserId,
            "attendance.clock_out",
            "AttendanceRecord",
            record.Id,
            cancellationToken);

        return ApiResponse<AttendanceResponse>.SuccessResponse(
            await BuildAttendanceResponseAsync(record, cancellationToken),
            AppMessages.Attendance.ClockedOut);
    }

    public async Task<ApiResponse<PagedResponse<AttendanceResponse>>> ListAsync(
        AttendanceListRequest request,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.Attendance.ListAsync(
            page,
            pageSize,
            organizationScope.GetCurrentOrganizationId(),
            request.EmployeeId,
            request.FromDate,
            request.ToDate,
            locationIds,
            request.Mode,
            request.PayrollEligible,
            cancellationToken);

        return ApiResponse<PagedResponse<AttendanceResponse>>.SuccessPagedResponse(
            await BuildAttendanceResponsesAsync(items, cancellationToken),
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
            await BuildAttendanceResponsesAsync(items, cancellationToken),
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
        if (record is null || !organizationScope.IsSameOrganization(record.OrganizationId))
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

        return ApiResponse<AttendanceResponse>.SuccessResponse(
            await BuildAttendanceResponseAsync(record, cancellationToken),
            AppMessages.Attendance.Adjusted);
    }

    public async Task<ApiResponse<AttendanceDailySummaryResponse>> GetDailySummaryAsync(
        Guid locationId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var summary = await unitOfWork.Attendance.GetLocationDailySummaryAsync(locationId, date, cancellationToken);

        return ApiResponse<AttendanceDailySummaryResponse>.SuccessResponse(
            new AttendanceDailySummaryResponse(
                locationId,
                date,
                summary.ScheduledCount,
                summary.ClockedInCount,
                summary.ClockedOutCount,
                summary.NotClockedInCount),
            AppMessages.Attendance.SummaryFound);
    }

    private async Task<IReadOnlyList<AttendanceResponse>> BuildAttendanceResponsesAsync(
        IReadOnlyList<AttendanceEntity> records,
        CancellationToken cancellationToken)
    {
        var assignments = new Dictionary<Guid, ShiftAssignmentEntity?>();
        var shifts = new Dictionary<Guid, ShiftDefinitionEntity?>();
        var schedules = new Dictionary<Guid, ScheduleEntity?>();
        var departments = new Dictionary<Guid, DepartmentEntity?>();
        var locations = new Dictionary<Guid, LocationEntity?>();

        var responses = new List<AttendanceResponse>(records.Count);
        foreach (var record in records)
            responses.Add(await BuildAttendanceResponseAsync(
                record,
                assignments,
                shifts,
                schedules,
                departments,
                locations,
                cancellationToken));

        return responses;
    }

    private async Task<AttendanceResponse> BuildAttendanceResponseAsync(
        AttendanceEntity record,
        CancellationToken cancellationToken)
    {
        return await BuildAttendanceResponseAsync(
            record,
            new Dictionary<Guid, ShiftAssignmentEntity?>(),
            new Dictionary<Guid, ShiftDefinitionEntity?>(),
            new Dictionary<Guid, ScheduleEntity?>(),
            new Dictionary<Guid, DepartmentEntity?>(),
            new Dictionary<Guid, LocationEntity?>(),
            cancellationToken);
    }

    private async Task<AttendanceResponse> BuildAttendanceResponseAsync(
        AttendanceEntity record,
        Dictionary<Guid, ShiftAssignmentEntity?> assignments,
        Dictionary<Guid, ShiftDefinitionEntity?> shifts,
        Dictionary<Guid, ScheduleEntity?> schedules,
        Dictionary<Guid, DepartmentEntity?> departments,
        Dictionary<Guid, LocationEntity?> locations,
        CancellationToken cancellationToken)
    {
        if (record.AssignmentId is not Guid assignmentId)
            return record.ToResponse();

        var assignment = await GetOrLoadAsync(
            assignments,
            assignmentId,
            id => unitOfWork.ShiftAssignments.GetByIdAsync(id, cancellationToken: cancellationToken));
        if (assignment is null)
            return record.ToResponse();

        var shift = await GetOrLoadAsync(
            shifts,
            assignment.ShiftDefinitionId,
            id => unitOfWork.ShiftDefinitions.GetByIdAsync(id, cancellationToken: cancellationToken));

        var schedule = await GetOrLoadAsync(
            schedules,
            assignment.ScheduleId,
            id => unitOfWork.Schedules.GetByIdAsync(id, cancellationToken: cancellationToken));

        DepartmentEntity? department = null;
        LocationEntity? location = null;

        if (schedule is not null)
        {
            department = await GetOrLoadAsync(
                departments,
                schedule.DepartmentId,
                id => unitOfWork.Departments.GetByIdAsync(id, cancellationToken: cancellationToken));
        }

        if (department is not null)
        {
            location = await GetOrLoadAsync(
                locations,
                department.LocationId,
                id => unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken));
        }

        var status = ComputeStatus(record, assignment, shift, location);
        return record.ToResponse(assignment, shift, department, location, status);
    }

    private static async Task<T?> GetOrLoadAsync<T>(
        Dictionary<Guid, T?> cache,
        Guid id,
        Func<Guid, Task<T?>> loadAsync)
        where T : class
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;

        var value = await loadAsync(id);
        cache[id] = value;
        return value;
    }

    private async Task<TimeZoneInfo> ResolveEmployeeTimeZoneAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var department = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: cancellationToken);
        if (department is null)
            return TimeZoneInfo.Utc;

        var location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);
        return SwapCutoffRules.ResolveTimeZone(location?.TimeZone ?? "UTC");
    }

    private async Task<ClockContext?> LoadClockContextAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(assignmentId, cancellationToken: cancellationToken);
        if (assignment is null)
            return null;

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(
            assignment.ShiftDefinitionId,
            cancellationToken: cancellationToken);
        if (shift is null)
            return null;

        var schedule = await unitOfWork.Schedules.GetByIdAsync(
            assignment.ScheduleId,
            cancellationToken: cancellationToken);
        if (schedule is null)
            return null;

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return null;

        var location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);
        var timeZone = SwapCutoffRules.ResolveTimeZone(location?.TimeZone ?? "UTC");

        return new ClockContext(assignment, shift, timeZone, location);
    }

    /// <summary>
    /// Advisory-only: GPS/IP/face signals are flags for Manager/Admin audit, never a clock-in blocker.
    /// Any failure (bad base64, Cloudinary unconfigured/unreachable) is swallowed so the punch always succeeds.
    /// </summary>
    private async Task ApplyVerificationAsync(
        EmployeeEntity employee,
        AttendanceEntity record,
        LocationEntity? location,
        ClockInRequest? request,
        CancellationToken cancellationToken)
    {
        if (request?.Latitude is double lat && request.Longitude is double lng)
        {
            record.ClockInLatitude = lat;
            record.ClockInLongitude = lng;

            if (location?.Latitude is double locLat && location.Longitude is double locLng)
            {
                var distanceMeters = GeoDistance.HaversineMeters(lat, lng, locLat, locLng);
                record.GpsOutOfRange = distanceMeters > verificationSettings.DefaultGeofenceRadiusMeters;
            }
        }

        var callerIp = currentUser.IpAddress;
        if (!string.IsNullOrWhiteSpace(callerIp))
        {
            record.ClockInIpAddress = callerIp;
            if (!string.IsNullOrWhiteSpace(location?.NetworkIpOrCidr))
                record.IpMismatch = !IpNetworkMatcher.IsInRange(callerIp, location.NetworkIpOrCidr);
        }

        var isFirstEnrollment = false;
        if (request?.PhotoBase64 is { Length: > 0 } photoBase64 && !string.IsNullOrWhiteSpace(request.PhotoContentType))
        {
            try
            {
                var photoBytes = Convert.FromBase64String(photoBase64);

                using var checkInStream = new MemoryStream(photoBytes);
                var checkInPhoto = await imageStorage.UploadCheckInPhotoAsync(
                    checkInStream,
                    $"checkin-{record.Id}.jpg",
                    request.PhotoContentType,
                    employee.OrganizationId,
                    employee.Id,
                    record.Id,
                    cancellationToken);
                record.ClockInPhotoUrl = checkInPhoto.Url;
                record.ClockInPhotoPublicId = checkInPhoto.PublicId;

                if (employee.FaceEnrollmentPhotoUrl is null)
                {
                    using var enrollmentStream = new MemoryStream(photoBytes);
                    var enrollmentPhoto = await imageStorage.UploadFaceEnrollmentPhotoAsync(
                        enrollmentStream,
                        $"face-enrollment-{employee.Id}.jpg",
                        request.PhotoContentType,
                        employee.OrganizationId,
                        employee.Id,
                        cancellationToken);

                    employee.FaceEnrollmentPhotoUrl = enrollmentPhoto.Url;
                    employee.FaceEnrollmentPhotoPublicId = enrollmentPhoto.PublicId;
                    employee.FaceEmbedding = request.FaceEmbeddingJson;
                    unitOfWork.Employees.Update(employee);
                    isFirstEnrollment = true;
                }
            }
            catch
            {
                // Advisory input — bad base64 or Cloudinary failure must never block the punch.
            }
        }

        if (!isFirstEnrollment && request?.FaceMatch is bool faceMatch)
            record.FaceMismatch = !faceMatch;
    }

    private static bool IsAfterShiftEnd(DateOnly date, TimeOnly endTime, TimeZoneInfo timeZone)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var shiftEnd = date.ToDateTime(endTime, DateTimeKind.Unspecified);
        return now > shiftEnd;
    }

    private static AttendanceStatus ComputeStatus(
        AttendanceEntity record,
        ShiftAssignmentEntity? assignment,
        ShiftDefinitionEntity? shift,
        LocationEntity? location)
    {
        if (record.AdjustedBy is not null)
            return AttendanceStatus.Adjusted;

        if (assignment is not null && shift is not null)
        {
            var timeZone = SwapCutoffRules.ResolveTimeZone(location?.TimeZone ?? "UTC");
            var scheduledStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                assignment.Date.ToDateTime(shift.StartTime, DateTimeKind.Unspecified),
                timeZone);

            if (record.ClockIn.UtcDateTime > scheduledStartUtc)
                return AttendanceStatus.Late;
        }

        return record.ClockOut is null ? AttendanceStatus.Open : AttendanceStatus.OnTime;
    }

    private sealed record ClockContext(
        ShiftAssignmentEntity Assignment,
        ShiftDefinitionEntity Shift,
        TimeZoneInfo TimeZone,
        LocationEntity? Location);

    private async Task<bool> IsPayPeriodLockedForRecordAsync(
        AttendanceEntity record,
        CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(record.EmployeeId, cancellationToken: cancellationToken);
        if (employee is null)
            return false;

        if (!employee.DepartmentId.HasValue)
            return false;

        var clockDate = DateOnly.FromDateTime(record.ClockIn.UtcDateTime);
        var period = await unitOfWork.PayPeriods.GetContainingDateAsync(employee.DepartmentId.Value, clockDate, cancellationToken);
        return period?.Status == PayPeriodStatus.Locked;
    }

    private static int ComputeWorkedMinutes(DateTimeOffset clockIn, DateTimeOffset clockOut)
    {
        var minutes = (int)Math.Round((clockOut - clockIn).TotalMinutes, MidpointRounding.AwayFromZero);
        return Math.Max(0, minutes);
    }
}
