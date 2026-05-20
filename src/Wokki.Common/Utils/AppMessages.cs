using Microsoft.AspNetCore.Http;

namespace Wokki.Common.Utils;

public static class AppMessages
{
    public static class Health
    {
        public static readonly AppMessage Ok = new("HEALTH_OK", "Service is healthy.", StatusCodes.Status200OK);
    }

    public static class User
    {
        public static readonly AppMessage Found = new("USER_FOUND", "User found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("USER_LISTED", "Users listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("USER_CREATED", "User created.", StatusCodes.Status201Created);
        public static readonly AppMessage Exists = new("USER_EXISTS", "Email already registered.", StatusCodes.Status409Conflict);
    }

    public static class Employee
    {
        public static readonly AppMessage Found = new("EMPLOYEE_FOUND", "Employee found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("EMPLOYEE_LISTED", "Employees listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("EMPLOYEE_NOT_FOUND", "Employee not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("EMPLOYEE_CREATED", "Employee created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("EMPLOYEE_UPDATED", "Employee updated.", StatusCodes.Status200OK);
        public static readonly AppMessage Deleted = new("EMPLOYEE_DELETED", "Employee terminated.", StatusCodes.Status200OK);
        public static readonly AppMessage AlreadyTerminated = new("EMPLOYEE_ALREADY_TERMINATED", "Employee is already terminated.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage UserAlreadyLinked = new("EMPLOYEE_USER_EXISTS", "Email already registered.", StatusCodes.Status409Conflict);
        public static readonly AppMessage DepartmentNotFound = new("EMPLOYEE_DEPARTMENT_NOT_FOUND", "Department not found.", StatusCodes.Status404NotFound);
    }

    public static class Location
    {
        public static readonly AppMessage Found = new("LOCATION_FOUND", "Location found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("LOCATION_LISTED", "Locations listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("LOCATION_CREATED", "Location created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("LOCATION_UPDATED", "Location updated.", StatusCodes.Status200OK);
        public static readonly AppMessage Exists = new("LOCATION_EXISTS", "Location name already exists.", StatusCodes.Status409Conflict);
    }

    public static class Department
    {
        public static readonly AppMessage Found = new("DEPARTMENT_FOUND", "Department found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("DEPARTMENT_LISTED", "Departments listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("DEPARTMENT_NOT_FOUND", "Department not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("DEPARTMENT_CREATED", "Department created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("DEPARTMENT_UPDATED", "Department updated.", StatusCodes.Status200OK);
        public static readonly AppMessage LocationNotFound = new("DEPARTMENT_LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
    }

    public static class Shift
    {
        public static readonly AppMessage Found = new("SHIFT_FOUND", "Shift definition found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("SHIFT_LISTED", "Shift definitions listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("SHIFT_NOT_FOUND", "Shift definition not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("SHIFT_CREATED", "Shift definition created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("SHIFT_UPDATED", "Shift definition updated.", StatusCodes.Status200OK);
        public static readonly AppMessage Deleted = new("SHIFT_DELETED", "Shift definition deactivated.", StatusCodes.Status200OK);
        public static readonly AppMessage LocationNotFound = new("SHIFT_LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage InvalidTimeRange = new("SHIFT_INVALID_TIME_RANGE", "End time must be after start time.", StatusCodes.Status400BadRequest);
    }

    public static class Schedule
    {
        public static readonly AppMessage Found = new("SCHEDULE_FOUND", "Schedule found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("SCHEDULE_LISTED", "Schedules listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("SCHEDULE_NOT_FOUND", "Schedule not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("SCHEDULE_CREATED", "Schedule created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("SCHEDULE_UPDATED", "Schedule updated.", StatusCodes.Status200OK);
        public static readonly AppMessage Deleted = new("SCHEDULE_DELETED", "Schedule deleted.", StatusCodes.Status200OK);
        public static readonly AppMessage Published = new("SCHEDULE_PUBLISHED", "Schedule published.", StatusCodes.Status200OK);
        public static readonly AppMessage Unpublished = new("SCHEDULE_UNPUBLISHED", "Schedule reverted to draft.", StatusCodes.Status200OK);
        public static readonly AppMessage Copied = new("SCHEDULE_COPIED", "Schedule copied.", StatusCodes.Status201Created);
        public static readonly AppMessage DepartmentNotFound = new("SCHEDULE_DEPARTMENT_NOT_FOUND", "Department not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage WeekNotMonday = new("SCHEDULE_WEEK_NOT_MONDAY", "Week start date must be a Monday.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AlreadyExists = new("SCHEDULE_ALREADY_EXISTS", "A schedule already exists for this department and week.", StatusCodes.Status409Conflict);
        public static readonly AppMessage NotDraft = new("SCHEDULE_NOT_DRAFT", "Schedule is not in draft state.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage NotPublished = new("SCHEDULE_NOT_PUBLISHED", "Schedule is not published.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AlreadyPublished = new("SCHEDULE_ALREADY_PUBLISHED", "Schedule is already published.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AssignmentListed = new("SCHEDULE_ASSIGNMENT_LISTED", "Assignments listed.", StatusCodes.Status200OK);
        public static readonly AppMessage AssignmentCreated = new("SCHEDULE_ASSIGNMENT_CREATED", "Assignment created.", StatusCodes.Status201Created);
        public static readonly AppMessage AssignmentDeleted = new("SCHEDULE_ASSIGNMENT_DELETED", "Assignment removed.", StatusCodes.Status200OK);
        public static readonly AppMessage AssignmentNotFound = new("SCHEDULE_ASSIGNMENT_NOT_FOUND", "Assignment not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage AssignmentConflict = new("SCHEDULE_ASSIGNMENT_CONFLICT", "Employee has an overlapping shift on this date.", StatusCodes.Status409Conflict);
        public static readonly AppMessage AssignmentDuplicate = new("SCHEDULE_ASSIGNMENT_DUPLICATE", "Employee is already assigned to this shift on this date.", StatusCodes.Status409Conflict);
        public static readonly AppMessage ShiftInactive = new("SCHEDULE_SHIFT_INACTIVE", "Shift definition is inactive.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage EmployeeNotFound = new("SCHEDULE_EMPLOYEE_NOT_FOUND", "Employee not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage MyScheduleListed = new("ME_SCHEDULE_LISTED", "Your schedule listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NoEmployeeProfile = new("ME_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
    }

    public static class Swap
    {
        public static readonly AppMessage Found = new("SWAP_FOUND", "Swap request found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("SWAP_LISTED", "Swap requests listed.", StatusCodes.Status200OK);
        public static readonly AppMessage Created = new("SWAP_CREATED", "Swap request created.", StatusCodes.Status201Created);
        public static readonly AppMessage Accepted = new("SWAP_ACCEPTED", "Swap accepted and applied.", StatusCodes.Status200OK);
        public static readonly AppMessage Declined = new("SWAP_DECLINED", "Swap declined.", StatusCodes.Status200OK);
        public static readonly AppMessage Cancelled = new("SWAP_CANCELLED", "Swap cancelled.", StatusCodes.Status200OK);
        public static readonly AppMessage OverrideApproved = new("SWAP_OVERRIDE_APPROVED", "Swap approved by manager.", StatusCodes.Status200OK);
        public static readonly AppMessage OverrideRejected = new("SWAP_OVERRIDE_REJECTED", "Swap rejected by manager.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("SWAP_NOT_FOUND", "Swap request not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Forbidden = new("SWAP_FORBIDDEN", "You are not allowed to perform this action on the swap request.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage InvalidTransition = new("SWAP_INVALID_TRANSITION", "Invalid swap status transition.", StatusCodes.Status409Conflict);
        public static readonly AppMessage CutoffExceeded = new("SWAP_CUTOFF_EXCEEDED", "Swap cutoff window has passed for next-week shifts.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AssignmentNotFound = new("SWAP_ASSIGNMENT_NOT_FOUND", "Shift assignment not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage ScheduleNotPublished = new("SWAP_SCHEDULE_NOT_PUBLISHED", "Schedule must be published before swapping.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage NotOwner = new("SWAP_NOT_OWNER", "You do not own the offered shift assignment.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage SameEmployee = new("SWAP_SAME_EMPLOYEE", "Cannot swap with yourself.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage OpenSwapExists = new("SWAP_OPEN_EXISTS", "A pending swap already exists for this assignment.", StatusCodes.Status409Conflict);
        public static readonly AppMessage PeerAcceptedExists = new("SWAP_PEER_ACCEPTED_EXISTS", "Another swap is already accepted for this assignment.", StatusCodes.Status409Conflict);
        public static readonly AppMessage NoEmployeeProfile = new("SWAP_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
    }

    public static class Attendance
    {
        public static readonly AppMessage Found = new("ATTENDANCE_FOUND", "Attendance record found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("ATTENDANCE_LISTED", "Attendance records listed.", StatusCodes.Status200OK);
        public static readonly AppMessage ClockedIn = new("ATTENDANCE_CLOCKED_IN", "Clock-in recorded.", StatusCodes.Status201Created);
        public static readonly AppMessage ClockedOut = new("ATTENDANCE_CLOCKED_OUT", "Clock-out recorded.", StatusCodes.Status200OK);
        public static readonly AppMessage Adjusted = new("ATTENDANCE_ADJUSTED", "Attendance record adjusted.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("ATTENDANCE_NOT_FOUND", "Attendance record not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage OpenRecordExists = new("ATTENDANCE_OPEN_EXISTS", "Employee already has an open attendance record.", StatusCodes.Status409Conflict);
        public static readonly AppMessage NoOpenRecord = new("ATTENDANCE_NO_OPEN", "No open attendance record to clock out.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage NoAssignmentToday = new("ATTENDANCE_NO_ASSIGNMENT", "No scheduled assignment found for today.", StatusCodes.Status404NotFound);
        public static readonly AppMessage NoEmployeeProfile = new("ATTENDANCE_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
        public static readonly AppMessage PeriodLocked = new("ATTENDANCE_PERIOD_LOCKED", "Pay period is locked.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AdjustmentNoteRequired = new("ATTENDANCE_NOTE_REQUIRED", "Adjustment note is required.", StatusCodes.Status400BadRequest);
    }

    public static class Payroll
    {
        public static readonly AppMessage Summary = new("PAYROLL_SUMMARY", "Payroll summary computed.", StatusCodes.Status200OK);
        public static readonly AppMessage EmployeeSummary = new("PAYROLL_EMPLOYEE_SUMMARY", "Employee payroll breakdown.", StatusCodes.Status200OK);
        public static readonly AppMessage Exported = new("PAYROLL_EXPORTED", "Payroll CSV exported.", StatusCodes.Status200OK);
        public static readonly AppMessage PeriodLocked = new("PAYROLL_PERIOD_LOCKED", "Pay period locked.", StatusCodes.Status200OK);
        public static readonly AppMessage DepartmentNotFound = new("PAYROLL_DEPARTMENT_NOT_FOUND", "Department not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage EmployeeNotFound = new("PAYROLL_EMPLOYEE_NOT_FOUND", "Employee not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage PeriodOverlap = new("PAYROLL_PERIOD_OVERLAP", "Pay period dates overlap an existing period.", StatusCodes.Status409Conflict);
        public static readonly AppMessage ExportTooLarge = new("PAYROLL_EXPORT_TOO_LARGE", "Payroll export exceeds maximum row limit.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidDateRange = new("PAYROLL_INVALID_RANGE", "Invalid pay period date range.", StatusCodes.Status400BadRequest);
    }

    public static class Auth
    {
        public static readonly AppMessage LoginSuccess = new("AUTH_LOGIN_SUCCESS", "Login successful.", StatusCodes.Status200OK);
        public static readonly AppMessage RefreshSuccess = new("AUTH_REFRESH_SUCCESS", "Token refreshed.", StatusCodes.Status200OK);
        public static readonly AppMessage Unauthorized = new("AUTH_UNAUTHORIZED", "Unauthorized.", StatusCodes.Status401Unauthorized);
        public static readonly AppMessage Forbidden = new("AUTH_FORBIDDEN", "Forbidden.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage InvalidCredentials = new("AUTH_INVALID_CREDENTIALS", "Invalid credentials.", StatusCodes.Status401Unauthorized);
        public static readonly AppMessage Me = new("AUTH_ME", "Current user profile.", StatusCodes.Status200OK);
        public static readonly AppMessage LogoutSuccess = new("AUTH_LOGOUT_SUCCESS", "Logged out.", StatusCodes.Status200OK);
        public static readonly AppMessage NotLoggedIn = new("AUTH_NOT_LOGGED_IN", "Not authenticated.", StatusCodes.Status401Unauthorized);
    }

    public static class Validation
    {
        public static readonly AppMessage Failed = new("VALIDATION_FAILED", "Validation failed.", StatusCodes.Status400BadRequest);
    }

    public static class Internal
    {
        public static readonly AppMessage Error = new("INTERNAL_ERROR", "An unexpected error occurred.", StatusCodes.Status500InternalServerError);
    }
}
