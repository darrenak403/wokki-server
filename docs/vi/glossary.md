# Thuật ngữ

| Thuật ngữ | Định nghĩa |
|-----------|------------|
| **Admin** | Quản trị hệ thống: dữ liệu gốc, user, xuất payroll, quyền ghi đè. |
| **Manager** | Quản lý vận hành: lịch, phân ca, duyệt đổi ca, điều chỉnh chấm công, tạo kênh chat. |
| **User** | Nhân viên: self-service (`/me/*`), chấm công, thao tác đổi ca với đồng nghiệp. |
| **Location** | Địa điểm vật lý, có múi giờ (`TimeZone` IANA). |
| **Department** | Đơn vị thuộc location; lịch tuần và kỳ lương theo department. |
| **Employee** | Hồ sơ nhân sự gắn 1:1 với `User`; có `Position`, `HourlyRate`, `DepartmentId`. |
| **Shift definition** | Mẫu ca: tên, giờ bắt đầu/kết thúc, `RequiredRole`, phạm vi location/department. |
| **Schedule** | Lịch tuần một department; `WeekStartDate` phải là **thứ Hai**. |
| **Shift assignment** | Một nhân viên trên một mẫu ca trong một ngày của lịch. |
| **Publish** | Chuyển lịch `Draft` → `Published`; nhân viên xem ca và có thể đổi ca. |
| **Swap request** | Yêu cầu đổi ca giữa hai phân công đã publish. |
| **Attendance record** | Bản ghi chấm công (vào/ra) và `WorkedMinutes`. |
| **Pay period** | Kỳ lương theo department: `Open` hoặc `Locked`. |
| **Payroll line** | Dòng snapshot khi kỳ khóa (phút, đơn giá, tổng lương). |
| **Channel** | Kênh chat: `Direct` (2 người) hoặc `Group`. |
| **Schedule suggestion** | Gợi ý phân ca tạm thời; chỉ lưu DB khi apply. |
| **Heuristic engine** | Engine gợi ý theo luật (`HeuristicScheduleSuggestionService`), không dùng LLM ngoài. |
| **Single-tenant** | Một doanh nghiệp / một deployment / một database. |

## Enum trạng thái (tham chiếu code)

| Enum | Giá trị | Ghi chú |
|------|---------|---------|
| `ScheduleStatus` | `Draft`, `Published`, `Locked` | MVP dùng **Draft/Published**; `Locked` dự phòng. |
| `SwapStatus` | `Pending`, `PeerAccepted`, `PeerDeclined`, `ManagerApproved`, `ManagerRejected`, `Cancelled` | Xem [process-flows.md](./process-flows.md). |
| `PayPeriodStatus` | `Open`, `Locked` | `Locked` chặn điều chỉnh chấm công trong kỳ. |
| `ChannelType` | `Direct`, `Group` | |
