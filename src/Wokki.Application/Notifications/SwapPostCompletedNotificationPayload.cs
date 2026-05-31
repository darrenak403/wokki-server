using Wokki.Domain.Enums;

namespace Wokki.Application.Notifications;

public sealed record SwapPostCompletedShiftLine(
    DateOnly Date,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SwapPostCompletedNotificationPayload(
    SwapPostType Type,
    DateOnly WeekStartDate,
    string? LocationName,
    string? DepartmentName,
    SwapPostCompletedShiftLine OfferedShift,
    SwapPostCompletedShiftLine? AcceptedShift,
    string RecipientFirstName = "bạn");
