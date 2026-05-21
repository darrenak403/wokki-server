# Quy tắc nghiệp vụ (đã khóa)

Quy tắc được đánh số **`BR-xxx`**. Service và API phải tuân thủ. Khi tiện lợi mâu thuẫn quy tắc, **ưu tiên quy tắc**.

Tham chiếu: [process-flows.md](./process-flows.md), [api-catalog.md](./api-catalog.md).

---

## Định danh & phân quyền

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-001 | Vai trò cố định: `Admin`, `Manager`, `User`. Không ma trận quyền động trong MVP. | JWT + `RequireRole` |
| BR-002 | `User` không truy cập API lịch quản lý (`/api/v1/schedules/*`). Chỉ xem lịch của mình qua `/api/v1/me/schedule`. | Authorization route |
| BR-003 | `Admin` quản lý user, xuất payroll, xóa mềm mọi tin nhắn chat. | `ChannelService`, `PayrollEndpoints` |
| BR-004 | `Manager` quản lý lịch, phân ca, ghi đè đổi ca, điều chỉnh chấm công, tạo kênh chat. | Authorization route |
| BR-005 | Thao tác nhân viên cần bản ghi `Employee` gắn với `User` đăng nhập. | Service trả `*_NO_EMPLOYEE` / 404 |

---

## Tổ chức & dữ liệu gốc

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-010 | `Department` thuộc đúng một `Location`. | EF FK |
| BR-011 | `Employee.DepartmentId` phải trùng department của lịch được phân ca. | `TryPrepareAssignmentAsync` |
| BR-012 | Nhân viên đã chấm dứt (`TerminatedAt`) không được phân ca mới hoặc làm đối tác đổi ca. | `EmployeeService`, validator |
| BR-013 | `ShiftDefinition` phải khớp phạm vi lịch: cùng `LocationId`; nếu có `DepartmentId` thì phải bằng department của lịch. | `TryPrepareAssignmentAsync` |

---

## Lập lịch (Scheduling)

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-020 | `WeekStartDate` phải là **thứ Hai** (`ScheduleRules.IsMonday`). | Tạo/sửa lịch |
| BR-021 | Tối đa một lịch cho mỗi `(DepartmentId, WeekStartDate)`. | Unique + service |
| BR-022 | Vòng đời MVP: **`Draft` → `Published`** (unpublish về `Draft`). Enum `Locked` có trong code nhưng **chưa** dùng ở API publish. | `ScheduleService` |
| BR-023 | Chỉ tạo/sửa/xóa phân ca khi lịch ở trạng thái **`Draft`**. | `CreateAssignmentAsync`, xóa phân ca |
| BR-024 | Một nhân viên không được trùng **khung giờ** trên cùng ngày trong một lịch. | `HasTimeOverlapAsync` |
| BR-025 | Từ chối trùng `(schedule, shiftDefinition, employee, date)`. | `ExistsAsync` |
| BR-026 | Khi publish, gửi thông báo `schedule.published` cho nhân viên được phân ca (không rollback nếu gửi lỗi). | `PublishAsync` |
| BR-027 | `GET /api/v1/me/schedule` chỉ trả phân ca của user trong **28 ngày** tới trên lịch **Published**. | `GetMyScheduleAsync` |

---

## Đổi ca (Swap)

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-030 | Chỉ đổi ca trên phân ca thuộc lịch **`Published`**. | `CreateAsync` |
| BR-031 | Người gửi phải sở hữu phân ca đề xuất. | `NotOwner` |
| BR-032 | Không đổi ca với chính mình. | `SameEmployee` |
| BR-033 | Tối đa một yêu cầu `Pending` cho mỗi phân ca gửi. | `HasOpenSwapForAssignmentAsync` |
| BR-034 | **Cutoff** (múi giờ location): ca tuần sau — tạo trước hết thứ Sáu; accept/decline trước 00:00 thứ Hai. | `SwapCutoffRules` |
| BR-035 | Chuyển trạng thái hợp lệ; không hợp lệ → **409**. | Guard từng action |
| BR-036 | Khi đồng nghiệp **accept**: `Pending` → `PeerAccepted` → đổi phân ca **trong một transaction** → `ManagerApproved`. | `AcceptAsync` |
| BR-037 | Thông báo (`swap.*`, `schedule.published`) **không** được làm rollback transaction chính nếu gửi thất bại. | try/catch `INotificationService` |

### Chuyển trạng thái swap (cho phép)

| Từ | Hành động | Đến |
|----|-----------|-----|
| `Pending` | Đối tác accept | `ManagerApproved` (auto-apply) |
| `Pending` | Đối tác decline | `PeerDeclined` |
| `Pending` | Người gửi cancel | `Cancelled` |
| `Pending` | Manager override approve | `ManagerApproved` |
| `Pending` | Manager override reject | `ManagerRejected` |

---

## Chấm công (Attendance)

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-040 | Clock-in chỉ khi **không** có bản ghi mở (`ClockOut IS NULL`). | Index + service |
| BR-041 | Clock-in cần ít nhất một phân ca **published** trong **ngày hôm nay**. | Repository |
| BR-042 | Clock-out cần bản ghi mở; tính `WorkedMinutes` (làm tròn phút). | `AttendanceService` |
| BR-043 | Điều chỉnh thủ công: chỉ `Admin`/`Manager`; **bắt buộc** ghi chú điều chỉnh. | `AdjustAsync` |
| BR-044 | Chặn điều chỉnh nếu kỳ lương **Locked** chứa ngày clock-in của bản ghi. | `IsPayPeriodLockedForRecordAsync` |
| BR-045 | Endpoint clock dùng rate limit `Clock` (300/phút). | `AttendanceEndpoints` |

---

## Lương (Payroll)

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-050 | Tổng hợp `WorkedMinutes` từ chấm công (có assignment) trong kỳ × `HourlyRate` hiện tại. | `PayrollService` |
| BR-051 | Tự tạo pay period lần đầu gọi summary nếu không trùng kỳ khác. | `PayPeriodRepository` |
| BR-052 | Kỳ **Locked** và đã có dòng snapshot → đọc **`PayrollLine`**, không tính live. | `PayrollService` |
| BR-053 | `HourlyRate` trên `PayrollLine` là **snapshot** khi khóa kỳ. | Domain |
| BR-054 | Xuất CSV tối đa **500** dòng; chỉ Admin. | `ExportCsvAsync` |
| BR-055 | Chỉ chấm công có `AssignmentId` và `ClockOut` mới tính vào payroll. | `SumWorkedMinutesByEmployeeAsync` |

---

## Chat

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-060 | Chỉ **thành viên kênh** được đọc/gửi tin; không phải member → **403**. | `ChannelService` |
| BR-061 | `Admin` đọc lịch sử mọi kênh không cần membership. | `ResolveMemberAccessAsync` |
| BR-062 | Kênh Direct: đúng **hai** thành viên; tái sử dụng DM nếu đã tồn tại. | `CreateAsync` |
| BR-063 | Kênh Group bắt buộc có **tên**. | `CreateAsync` |
| BR-064 | Nội dung: text thuần, tối đa **4000** ký tự; loại bỏ thẻ HTML. | `SanitizeBody` |
| BR-065 | Xóa mềm: `DeletedAt` + placeholder `[Message deleted]`. | `DeleteMessageAsync` |
| BR-066 | Lưu DB trước; SignalR `ReceiveMessage` best-effort. | `SendMessageAsync` |
| BR-067 | WebSocket `/ws/chat` cần JWT (`access_token`); log **redact** token. | JwtBearer + Serilog |

---

## Gợi ý lịch (Heuristic / AI MVP)

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-070 | `POST .../suggest` **chỉ đọc**, không ghi DB. | `HeuristicScheduleSuggestionService` |
| BR-071 | Suggest/apply chỉ trên lịch **`Draft`**. | Services |
| BR-072 | Cần ≥ **3** phân ca published trong **4 tuần** trước; không đủ → `reason: insufficient_history`. | Ngưỡng heuristic |
| BR-073 | Gợi ý không double-book trong tuần mục tiêu. | `HasOverlapInPlan` |
| BR-074 | `Position` nhân viên phải khớp `RequiredRole` của ca. | `RoleMatches` |
| BR-075 | `apply-suggestions` validate **tất cả** dòng rồi commit **một transaction** (cả hoặc không). | `ApplySuggestionsAsync` |

---

## Thông báo & chung

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-080 | Email SMTP khi cấu hình `Smtp:Host` + `Smtp:From`; không thì no-op log. | Infrastructure DI |
| BR-081 | API trả envelope `ApiResponse<T>` + mã `AppMessage`. | Toàn Application |
| BR-082 | Logic nghiệp vụ chỉ ở **Application**; API chỉ map HTTP. | Kiến trúc |
