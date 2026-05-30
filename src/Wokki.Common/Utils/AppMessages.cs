using Microsoft.AspNetCore.Http;

namespace Wokki.Common.Utils;

public static class AppMessages
{
    public static class Health
    {
        public static readonly AppMessage Ok = new("HEALTH_OK", "Service is healthy.", StatusCodes.Status200OK);
    }

    public static class Bedrock
    {
        public static readonly AppMessage Connected =
            new("BEDROCK_CONNECTED", "AWS Bedrock connection is healthy.", StatusCodes.Status200OK);

        public static readonly AppMessage Disconnected =
            new("BEDROCK_DISCONNECTED", "AWS Bedrock connection failed.", StatusCodes.Status503ServiceUnavailable);

        public static readonly AppMessage Throttled =
            new(
                "BEDROCK_THROTTLED",
                "Bedrock daily token quota exceeded. Wait and retry, use a smaller model, or request a quota increase.",
                StatusCodes.Status429TooManyRequests);
    }

    public static class User
    {
        public static readonly AppMessage Found = new("USER_FOUND", "User found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("USER_LISTED", "Users listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("USER_CREATED", "User created.", StatusCodes.Status201Created);
        public static readonly AppMessage Exists = new("USER_EXISTS", "Email already registered.", StatusCodes.Status409Conflict);
        public static readonly AppMessage EmployeeProfileRequired = new(
            "USER_EMPLOYEE_PROFILE_REQUIRED",
            "Create org accounts through the employee workflow so every account has an employee profile.",
            StatusCodes.Status400BadRequest);
    }

    public static class Platform
    {
        public static readonly AppMessage UsersListed = new("PLATFORM_USERS_LISTED", "Platform users listed.", StatusCodes.Status200OK);
        public static readonly AppMessage OrganizationsListed = new("PLATFORM_ORGS_LISTED", "Platform organizations listed.", StatusCodes.Status200OK);
        public static readonly AppMessage OrganizationUpdated = new("PLATFORM_ORG_UPDATED", "Organization subscription updated.", StatusCodes.Status200OK);
        public static readonly AppMessage OrganizationNotFound = new("PLATFORM_ORG_NOT_FOUND", "Organization not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage SubscriptionDurationRequired = new(
            "SUBSCRIPTION_DURATION_REQUIRED",
            "durationDays is required (1–3650) when enabling or renewing an org package.",
            StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage DepartmentRequiredForUser = new("EMPLOYEE_DEPARTMENT_REQUIRED", "Department is required for employee (User) role.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ManagerLocationsRequired = new("EMPLOYEE_MANAGER_LOCATIONS_REQUIRED", "At least one branch must be assigned when creating a Manager.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ManagerLocationNotFound = new("EMPLOYEE_MANAGER_LOCATION_NOT_FOUND", "One or more assigned branches were not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage DepartmentMembershipsListed = new("EMPLOYEE_DEPT_MEMBERSHIPS_LISTED", "Employee department membership history listed.", StatusCodes.Status200OK);
    }

    public static class Self
    {
        public static readonly AppMessage ProfileFound = new("SELF_PROFILE_FOUND", "Personal profile found.", StatusCodes.Status200OK);
        public static readonly AppMessage ProfileUpdated = new("SELF_PROFILE_UPDATED", "Personal profile updated.", StatusCodes.Status200OK);
        public static readonly AppMessage PaymentQrUploaded = new("SELF_PAYMENT_QR_UPLOADED", "Payment QR image uploaded.", StatusCodes.Status200OK);
        public static readonly AppMessage PaymentQrInvalid = new("SELF_PAYMENT_QR_INVALID", "Invalid payment QR image.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage CloudinaryNotConfigured = new("CLOUDINARY_NOT_CONFIGURED", "Image upload is not configured.", StatusCodes.Status503ServiceUnavailable);
        public static readonly AppMessage NoEmployeeProfile = new("SELF_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
    }

    public static class Location
    {
        public static readonly AppMessage Found = new("LOCATION_FOUND", "Location found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("LOCATION_LISTED", "Locations listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("LOCATION_CREATED", "Location created.", StatusCodes.Status201Created);
        public static readonly AppMessage Updated = new("LOCATION_UPDATED", "Location updated.", StatusCodes.Status200OK);
        public static readonly AppMessage Exists = new("LOCATION_EXISTS", "Location name already exists.", StatusCodes.Status409Conflict);
        public static readonly AppMessage SchedulingPolicyFound = new("LOCATION_SCHEDULING_POLICY_FOUND", "Location scheduling policy found.", StatusCodes.Status200OK);
        public static readonly AppMessage SchedulingPolicyUpdated = new("LOCATION_SCHEDULING_POLICY_UPDATED", "Location scheduling policy updated.", StatusCodes.Status200OK);
        public static readonly AppMessage SchedulingPolicyMissing = new("LOCATION_SCHEDULING_POLICY_MISSING", "Location scheduling policy is required before auto-scheduling.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage SchedulingPolicyInvalid = new("LOCATION_SCHEDULING_POLICY_INVALID", "Location scheduling policy is invalid.", StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage Copied = new("SHIFT_COPIED", "Shift definitions copied.", StatusCodes.Status200OK);
        public static readonly AppMessage CopySourceNotFound = new("SHIFT_COPY_SOURCE_NOT_FOUND", "Source department or shifts not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage CopyTargetInvalid = new("SHIFT_COPY_TARGET_INVALID", "One or more target departments are invalid.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage CopyNothingToCopy = new("SHIFT_COPY_NOTHING_TO_COPY", "No active shifts to copy from the source department.", StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage CopyTargetNotDraft = new(
            "SCHEDULE_COPY_TARGET_NOT_DRAFT",
            "Target week schedule must be draft to overwrite.",
            StatusCodes.Status400BadRequest);
        public static readonly AppMessage CopySameWeek = new(
            "SCHEDULE_COPY_SAME_WEEK",
            "Source and target week must be different.",
            StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage EmployeeWrongDepartment = new("SCHEDULE_EMPLOYEE_WRONG_DEPT", "Employee does not belong to this schedule department.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage EmployeeWrongLocation = new("SCHEDULE_EMPLOYEE_WRONG_LOCATION", "Employee does not have an active membership in this schedule location.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ShiftWrongScope = new("SCHEDULE_SHIFT_WRONG_SCOPE", "Shift definition does not apply to this schedule.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage MyScheduleListed = new("ME_SCHEDULE_LISTED", "Your schedule listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NoEmployeeProfile = new("ME_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
        public static readonly AppMessage SuggestionsGenerated = new("SCHEDULE_SUGGESTIONS_GENERATED", "Schedule suggestions generated.", StatusCodes.Status200OK);
        public static readonly AppMessage SuggestionsApplied = new("SCHEDULE_SUGGESTIONS_APPLIED", "Schedule suggestions applied.", StatusCodes.Status200OK);
        public static readonly AppMessage SuggestionsEmpty = new("SCHEDULE_SUGGESTIONS_EMPTY", "No suggestions to apply.", StatusCodes.Status400BadRequest);
    }

    public static class SchedulePreference
    {
        public static readonly AppMessage Found = new("SCHEDULE_PREFERENCE_FOUND", "Schedule preferences found.", StatusCodes.Status200OK);
        public static readonly AppMessage Saved = new("SCHEDULE_PREFERENCE_SAVED", "Schedule preferences saved.", StatusCodes.Status200OK);
        public static readonly AppMessage Submitted = new("SCHEDULE_PREFERENCE_SUBMITTED", "Schedule preferences submitted.", StatusCodes.Status200OK);
        public static readonly AppMessage BoardListed = new("SCHEDULE_PREFERENCE_BOARD_LISTED", "Preference board listed.", StatusCodes.Status200OK);
        public static readonly AppMessage WrongDepartment = new("SCHEDULE_PREFERENCE_WRONG_DEPT", "Schedule does not belong to your department.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidShift = new("SCHEDULE_PREFERENCE_INVALID_SHIFT", "Shift is not valid for this schedule.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage DateOutOfRange = new("SCHEDULE_PREFERENCE_DATE_OUT_OF_RANGE", "Date is outside the schedule week.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidPreferenceType = new("SCHEDULE_PREFERENCE_INVALID_TYPE", "Preference type must be Preferred, Available, or Unavailable.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage AlreadySubmitted = new("SCHEDULE_PREFERENCE_ALREADY_SUBMITTED", "Preferences are already submitted and cannot be edited.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage Empty = new("SCHEDULE_PREFERENCE_EMPTY", "Submit at least one preference line first.", StatusCodes.Status400BadRequest);
    }

    public static class ScheduleInsight
    {
        public static readonly AppMessage ContextFound = new("SCHEDULE_INSIGHT_CONTEXT_FOUND", "Schedule insight context found.", StatusCodes.Status200OK);
        public static readonly AppMessage ContextGenerated = new("SCHEDULE_INSIGHT_CONTEXT_GENERATED", "Schedule insight context generated.", StatusCodes.Status200OK);
        public static readonly AppMessage ContextNotFound = new("SCHEDULE_INSIGHT_CONTEXT_NOT_FOUND", "Schedule insight context not found. Generate or refresh it first.", StatusCodes.Status404NotFound);
        public static readonly AppMessage ContextExpired = new("SCHEDULE_INSIGHT_CONTEXT_EXPIRED", "Schedule insight context expired. Generate or refresh it first.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ChatAnswered = new("SCHEDULE_INSIGHT_CHAT_ANSWERED", "Schedule insight assistant answered.", StatusCodes.Status200OK);
        public static readonly AppMessage ChatUnavailable = new("SCHEDULE_INSIGHT_CHAT_UNAVAILABLE", "Schedule insight assistant is unavailable. Scheduling is not affected.", StatusCodes.Status503ServiceUnavailable);
    }

    public static class ScheduleLeaveRequest
    {
        public static readonly AppMessage Created = new("SCHEDULE_LEAVE_REQUEST_CREATED", "Leave request submitted.", StatusCodes.Status201Created);
        public static readonly AppMessage Listed = new("SCHEDULE_LEAVE_REQUEST_LISTED", "Leave requests listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("SCHEDULE_LEAVE_REQUEST_NOT_FOUND", "Leave request not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Forbidden = new("SCHEDULE_LEAVE_REQUEST_FORBIDDEN", "You cannot access this leave request.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage Approved = new("SCHEDULE_LEAVE_REQUEST_APPROVED", "Leave request approved.", StatusCodes.Status200OK);
        public static readonly AppMessage Rejected = new("SCHEDULE_LEAVE_REQUEST_REJECTED", "Leave request rejected.", StatusCodes.Status200OK);
        public static readonly AppMessage Cancelled = new("SCHEDULE_LEAVE_REQUEST_CANCELLED", "Leave request cancelled.", StatusCodes.Status200OK);
        public static readonly AppMessage DuplicatePending = new("SCHEDULE_LEAVE_REQUEST_DUPLICATE_PENDING", "A pending leave request already exists for this shift.", StatusCodes.Status409Conflict);
        public static readonly AppMessage InvalidTransition = new("SCHEDULE_LEAVE_REQUEST_INVALID_TRANSITION", "Leave request cannot be updated in its current state.", StatusCodes.Status409Conflict);
        public static readonly AppMessage ReasonRequired = new("SCHEDULE_LEAVE_REQUEST_REASON_REQUIRED", "Leave reason is required.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidDate = new("SCHEDULE_LEAVE_REQUEST_INVALID_DATE", "Leave date is invalid.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ScheduleRequired = new("SCHEDULE_LEAVE_REQUEST_SCHEDULE_REQUIRED", "Schedule id is required.", StatusCodes.Status400BadRequest);
    }

    public static class Chat
    {
        public static readonly AppMessage Listed = new("CHAT_CHANNELS_LISTED", "Channels listed.", StatusCodes.Status200OK);
        public static readonly AppMessage Found = new("CHAT_CHANNEL_FOUND", "Channel found.", StatusCodes.Status200OK);
        public static readonly AppMessage Created = new("CHAT_CHANNEL_CREATED", "Channel created.", StatusCodes.Status201Created);
        public static readonly AppMessage ChannelNotFound = new("CHAT_CHANNEL_NOT_FOUND", "Channel not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage MessagesListed = new("CHAT_MESSAGES_LISTED", "Messages listed.", StatusCodes.Status200OK);
        public static readonly AppMessage MessageSent = new("CHAT_MESSAGE_SENT", "Message sent.", StatusCodes.Status201Created);
        public static readonly AppMessage MessageDeleted = new("CHAT_MESSAGE_DELETED", "Message deleted.", StatusCodes.Status200OK);
        public static readonly AppMessage MessageNotFound = new("CHAT_MESSAGE_NOT_FOUND", "Message not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Forbidden = new("CHAT_FORBIDDEN", "You are not a member of this channel.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage NoEmployeeProfile = new("CHAT_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
        public static readonly AppMessage MembersRequired = new("CHAT_MEMBERS_REQUIRED", "At least one member is required.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage MemberNotFound = new("CHAT_MEMBER_NOT_FOUND", "One or more members were not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage DirectRequiresTwoMembers = new("CHAT_DIRECT_TWO_MEMBERS", "Direct channels require exactly two members.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage GroupNameRequired = new("CHAT_GROUP_NAME_REQUIRED", "Group channels require a name.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage BodyRequired = new("CHAT_BODY_REQUIRED", "Message body is required.", StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage AssignmentWindowPassed = new("ATTENDANCE_ASSIGNMENT_WINDOW_PASSED", "Shift has already ended; clock-in is no longer allowed.", StatusCodes.Status400BadRequest);
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
        public static readonly AppMessage PasswordChanged = new("AUTH_PASSWORD_CHANGED", "Password updated.", StatusCodes.Status200OK);
        public static readonly AppMessage OtpSent = new("AUTH_OTP_SENT", "If the email exists, a verification code was sent.", StatusCodes.Status200OK);
        public static readonly AppMessage OtpVerified = new("AUTH_OTP_VERIFIED", "Verification code accepted.", StatusCodes.Status200OK);
        public static readonly AppMessage OtpInvalid = new("AUTH_OTP_INVALID", "Invalid or expired verification code.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage OtpNotVerified = new("AUTH_OTP_NOT_VERIFIED", "Verify the email code before setting a new password.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage OtpResendTooSoon = new("AUTH_OTP_RESEND_TOO_SOON", "Please wait for the current code to expire before requesting a new one.", StatusCodes.Status429TooManyRequests);
        public static readonly AppMessage OtpSendLocked = new("AUTH_OTP_SEND_LOCKED", "Too many verification requests. Try again in 30 minutes.", StatusCodes.Status429TooManyRequests);
        public static readonly AppMessage PasswordConfirmMismatch = new("AUTH_PASSWORD_CONFIRM_MISMATCH", "New password and confirmation do not match.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage PasswordResetSuccess = new("AUTH_PASSWORD_RESET", "Password reset successful.", StatusCodes.Status200OK);
    }

    public static class OvertimeRequest
    {
        public static readonly AppMessage Submitted = new("OT_SUBMITTED", "Overtime request submitted.", StatusCodes.Status201Created);
        public static readonly AppMessage ClockedOut = new("OT_CLOCKED_OUT", "Overtime clock-out recorded.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("OT_LISTED", "Overtime requests listed.", StatusCodes.Status200OK);
        public static readonly AppMessage Approved = new("OT_APPROVED", "Overtime request approved.", StatusCodes.Status200OK);
        public static readonly AppMessage Rejected = new("OT_REJECTED", "Overtime request rejected.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("OT_NOT_FOUND", "Overtime request not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Forbidden = new("OT_FORBIDDEN", "You are not allowed to perform this action on the overtime request.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage NoEmployeeProfile = new("OT_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
        public static readonly AppMessage AssignmentNotFound = new("OT_ASSIGNMENT_NOT_FOUND", "Shift assignment not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage ScheduleNotPublished = new("OT_SCHEDULE_NOT_PUBLISHED", "Schedule must be published before submitting OT.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ShiftNotEnded = new("OT_SHIFT_NOT_ENDED", "Shift must end before submitting an OT request.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage ActiveOTExists = new("OT_ACTIVE_EXISTS", "An active overtime request already exists for this shift.", StatusCodes.Status409Conflict);
        public static readonly AppMessage AlreadyClosed = new("OT_ALREADY_CLOSED", "Overtime session is not open.", StatusCodes.Status409Conflict);
        public static readonly AppMessage InvalidTransition = new("OT_INVALID_TRANSITION", "Overtime request is in an invalid state for this action.", StatusCodes.Status409Conflict);
        public static readonly AppMessage PeriodLocked = new("OT_PERIOD_LOCKED", "Pay period is locked.", StatusCodes.Status400BadRequest);
    }

    public static class LocationMembership
    {
        public static readonly AppMessage Requested = new("LM_REQUESTED", "Join request submitted.", StatusCodes.Status201Created);
        public static readonly AppMessage Reviewed = new("LM_REVIEWED", "Membership reviewed.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("LM_LISTED", "Memberships listed.", StatusCodes.Status200OK);
        public static readonly AppMessage Found = new("LM_FOUND", "Membership found.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("LM_NOT_FOUND", "Membership not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage NoEmployeeProfile = new("LM_NO_EMPLOYEE", "No employee profile linked to this account.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Forbidden = new("LM_FORBIDDEN", "You are not authorized to manage memberships for this location.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage LocationNotFound = new("LM_LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage DuplicateRequest = new("LM_DUPLICATE", "A pending or active membership for this location already exists.", StatusCodes.Status409Conflict);
        public static readonly AppMessage ActiveMembershipConflict = new("LM_ACTIVE_CONFLICT", "Employee already has an active membership at another location.", StatusCodes.Status409Conflict);
        public static readonly AppMessage InvalidReviewStatus = new("LM_INVALID_STATUS", "Membership is not in Pending status and cannot be reviewed.", StatusCodes.Status409Conflict);
    }

    public static class LocationManager
    {
        public static readonly AppMessage Assigned = new("LMG_ASSIGNED", "Manager assigned to location.", StatusCodes.Status201Created);
        public static readonly AppMessage Removed = new("LMG_REMOVED", "Manager removed from location.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("LMG_LISTED", "Location managers listed.", StatusCodes.Status200OK);
        public static readonly AppMessage MyLocationsListed = new("LMG_MY_LOCATIONS", "Assigned locations listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("LMG_NOT_FOUND", "Manager assignment not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage AlreadyAssigned = new("LMG_ALREADY_ASSIGNED", "User is already a manager of this location.", StatusCodes.Status409Conflict);
        public static readonly AppMessage LocationNotFound = new("LMG_LOCATION_NOT_FOUND", "Location not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage UserNotFound = new("LMG_USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);
    }

    public static class Workspace
    {
        public static readonly AppMessage RoleChanged = new("WS_ROLE_CHANGED", "User role updated.", StatusCodes.Status200OK);
        public static readonly AppMessage CannotModifyAdmin = new("WS_CANNOT_MODIFY_ADMIN", "Admin accounts cannot be modified.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage LocationTransferred = new("WS_LOCATION_TRANSFERRED", "Employee transferred to new location.", StatusCodes.Status200OK);
        public static readonly AppMessage DepartmentTransferred = new("WS_DEPT_TRANSFERRED", "Employee transferred to new department.", StatusCodes.Status200OK);
        public static readonly AppMessage TransferForbidden = new("WS_TRANSFER_FORBIDDEN", "You are not authorized to manage this employee.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage AlreadyAtLocation = new("WS_ALREADY_AT_LOCATION", "Employee already has an active membership at this location.", StatusCodes.Status409Conflict);
        public static readonly AppMessage AlreadyInDepartment = new("WS_ALREADY_IN_DEPT", "Employee is already in this department.", StatusCodes.Status409Conflict);
        public static readonly AppMessage EmployeeWrongLocation = new("WS_EMPLOYEE_WRONG_LOCATION", "Employee must have an active membership in the target department's location before department transfer.", StatusCodes.Status400BadRequest);
    }

    public static class Stats
    {
        public static readonly AppMessage PlatformFound = new("STATS_PLATFORM_FOUND", "Platform statistics retrieved.", StatusCodes.Status200OK);
        public static readonly AppMessage OrgFound = new("STATS_ORG_FOUND", "Organization statistics retrieved.", StatusCodes.Status200OK);
        public static readonly AppMessage OrgSubscriptionFound = new("STATS_ORG_SUBSCRIPTION_FOUND", "Organization subscription retrieved.", StatusCodes.Status200OK);
        public static readonly AppMessage Forbidden = new("STATS_FORBIDDEN", "You are not authorized to view these statistics.", StatusCodes.Status403Forbidden);
    }

    public static class Organization
    {
        public static readonly AppMessage CrossTenant = new("ORG_CROSS_TENANT", "Resource not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Required = new("ORG_REQUIRED", "Organization context is required.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage Disabled = new("ORG_DISABLED", "Organization is disabled.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage PackageNotActivated = new("ORG_PACKAGE_NOT_ACTIVATED", "Organization package is not activated.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage PackageExpired = new("ORG_PACKAGE_EXPIRED", "Organization package has expired.", StatusCodes.Status402PaymentRequired);
        public static readonly AppMessage SchedulingCatalogFound = new("ORG_SCHEDULING_CATALOG_FOUND", "Scheduling rule catalog found.", StatusCodes.Status200OK);
        public static readonly AppMessage SchedulingPolicyFound = new("ORG_SCHEDULING_POLICY_FOUND", "Organization scheduling policy found.", StatusCodes.Status200OK);
        public static readonly AppMessage SchedulingPolicyUpdated = new("ORG_SCHEDULING_POLICY_UPDATED", "Organization scheduling policy updated.", StatusCodes.Status200OK);
        public static readonly AppMessage SchedulingPolicyInvalid = new("ORG_SCHEDULING_POLICY_INVALID", "Organization scheduling policy is invalid.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage SchedulingPolicyInfeasible = new("ORG_SCHEDULING_POLICY_INFEASIBLE", "Organization scheduling policy is not feasible.", StatusCodes.Status422UnprocessableEntity);
        public static readonly AppMessage SchedulingPolicyWizardDraftCreated = new("ORG_SCHEDULING_POLICY_WIZARD_DRAFT", "Scheduling policy wizard draft created.", StatusCodes.Status200OK);
    }

    public static class Validation
    {
        public static readonly AppMessage Failed = new("VALIDATION_FAILED", "Validation failed.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidPageSize = new("VALIDATION_INVALID_PAGE_SIZE", "pageSize must be between 1 and 100.", StatusCodes.Status400BadRequest);
        public static readonly AppMessage InvalidPage = new("VALIDATION_INVALID_PAGE", "page must be greater than or equal to 1.", StatusCodes.Status400BadRequest);
    }

    public static class Internal
    {
        public static readonly AppMessage Error = new("INTERNAL_ERROR", "An unexpected error occurred.", StatusCodes.Status500InternalServerError);
    }
}
