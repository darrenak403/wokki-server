using System.Net;
using System.Text;
using System.Text.Json;

namespace Wokki.Application.Notifications;

public sealed record ComposedEmail(string Subject, string PlainTextBody, string HtmlBody);

public static class NotificationEmailComposer
{
    public static ComposedEmail Compose(string eventName, object payload)
    {
        if (eventName == "schedule.published" && payload is SchedulePublishedNotificationPayload schedulePayload)
            return ComposeSchedulePublished(schedulePayload);

        return ComposeFallback(eventName, payload);
    }

    private static ComposedEmail ComposeSchedulePublished(SchedulePublishedNotificationPayload payload)
    {
        var weekRange = FormatWeekRange(payload.WeekStartDate);
        var location = string.IsNullOrWhiteSpace(payload.LocationName) ? "chi nhánh" : payload.LocationName.Trim();
        var department = string.IsNullOrWhiteSpace(payload.DepartmentName) ? "phòng ban" : payload.DepartmentName.Trim();
        var greetingName = string.IsNullOrWhiteSpace(payload.EmployeeFirstName)
            ? "bạn"
            : payload.EmployeeFirstName.Trim();

        var subject = $"Lịch ca tuần {weekRange} đã được công bố";

        var plain = new StringBuilder();
        plain.AppendLine($"Xin chào {greetingName},");
        plain.AppendLine();
        plain.AppendLine($"Lịch ca tuần {weekRange} tại {location} — {department} đã được công bố.");
        plain.AppendLine();
        AppendShiftSchedulePlain(plain, payload.Shifts);
        plain.AppendLine();
        plain.AppendLine("Đăng nhập Wokki để xem chi tiết và quản lý ca làm việc.");

        var html = new StringBuilder();
        html.Append("""
            <!DOCTYPE html>
            <html lang="vi">
            <head><meta charset="utf-8" /></head>
            <body style="margin:0;padding:24px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#0f172a;background:#f8fafc;">
            <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;padding:24px;">
            """);

        html.Append("<p style=\"margin:0 0 12px;font-size:15px;line-height:1.5;\">Xin chào <strong>")
            .Append(WebUtility.HtmlEncode(greetingName))
            .Append("</strong>,</p>");

        html.Append("<p style=\"margin:0 0 20px;font-size:15px;line-height:1.5;\">Lịch ca tuần <strong>")
            .Append(WebUtility.HtmlEncode(weekRange))
            .Append("</strong> tại <strong>")
            .Append(WebUtility.HtmlEncode(location))
            .Append("</strong> — <strong>")
            .Append(WebUtility.HtmlEncode(department))
            .Append("</strong> đã được công bố.</p>");

        AppendShiftScheduleHtml(html, payload.Shifts);

        html.Append("""
            <p style="margin:24px 0 0;font-size:13px;line-height:1.5;color:#64748b;">
            Đăng nhập Wokki để xem chi tiết và quản lý ca làm việc.
            </p>
            </div>
            </body>
            </html>
            """);

        return new ComposedEmail(subject, plain.ToString().TrimEnd(), html.ToString());
    }

    private static void AppendShiftSchedulePlain(StringBuilder plain, IReadOnlyList<SchedulePublishedShiftLine> shifts)
    {
        if (shifts.Count == 0)
        {
            plain.AppendLine("Tuần này bạn chưa được phân ca.");
            return;
        }

        foreach (var dayGroup in shifts.GroupBy(s => s.Date).OrderBy(g => g.Key))
        {
            plain.AppendLine(FormatVietnameseDayLabel(dayGroup.Key));
            foreach (var shift in dayGroup.OrderBy(s => s.StartTime))
            {
                plain.AppendLine($"  • {shift.ShiftName} — {FormatTimeRange(shift.StartTime, shift.EndTime)}");
            }

            plain.AppendLine();
        }
    }

    private static void AppendShiftScheduleHtml(StringBuilder html, IReadOnlyList<SchedulePublishedShiftLine> shifts)
    {
        if (shifts.Count == 0)
        {
            html.Append("<p style=\"margin:0;font-size:14px;color:#64748b;\">Tuần này bạn chưa được phân ca.</p>");
            return;
        }

        html.Append("<div style=\"border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;\">");

        var isFirst = true;
        foreach (var dayGroup in shifts.GroupBy(s => s.Date).OrderBy(g => g.Key))
        {
            if (!isFirst)
                html.Append("<div style=\"height:1px;background:#e2e8f0;\"></div>");
            isFirst = false;

            html.Append("<div style=\"padding:12px 16px;background:#f8fafc;\">")
                .Append("<div style=\"font-size:13px;font-weight:700;color:#0f172a;\">")
                .Append(WebUtility.HtmlEncode(FormatVietnameseDayLabel(dayGroup.Key)))
                .Append("</div></div>");

            html.Append("<div style=\"padding:8px 16px 12px;\">");
            foreach (var shift in dayGroup.OrderBy(s => s.StartTime))
            {
                html.Append("<div style=\"display:flex;justify-content:space-between;gap:12px;padding:6px 0;font-size:14px;line-height:1.4;\">")
                    .Append("<span style=\"font-weight:600;color:#1e293b;\">")
                    .Append(WebUtility.HtmlEncode(shift.ShiftName))
                    .Append("</span><span style=\"color:#64748b;white-space:nowrap;\">")
                    .Append(WebUtility.HtmlEncode(FormatTimeRange(shift.StartTime, shift.EndTime)))
                    .Append("</span></div>");
            }

            html.Append("</div>");
        }

        html.Append("</div>");
    }

    private static ComposedEmail ComposeFallback(string eventName, object payload)
    {
        var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var subject = eventName switch
        {
            "swap.peer.accepted" => "Wokki: Yêu cầu đổi ca đã được chấp nhận",
            "swap.peer.declined" => "Wokki: Yêu cầu đổi ca bị từ chối",
            "swap.cancelled" => "Wokki: Yêu cầu đổi ca đã bị hủy",
            "swap.manager.approved" => "Wokki: Quản lý đã duyệt đổi ca",
            "swap.manager.rejected" => "Wokki: Quản lý từ chối đổi ca",
            _ => $"Wokki: {eventName}",
        };

        var html = $"<pre style=\"font-family:monospace;font-size:13px;\">{WebUtility.HtmlEncode(body)}</pre>";
        return new ComposedEmail(subject, body, html);
    }

    private static string FormatWeekRange(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return $"{weekStart:dd/MM} – {weekEnd:dd/MM}/{weekEnd:yyyy}";
    }

    private static string FormatVietnameseDayLabel(DateOnly date)
    {
        var dayName = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            _ => "Chủ Nhật",
        };

        return $"{dayName}, {date:dd/MM/yyyy}";
    }

    private static string FormatTimeRange(TimeOnly start, TimeOnly end) =>
        $"{start:HH\\:mm} – {end:HH\\:mm}";
}
