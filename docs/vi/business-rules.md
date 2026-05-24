# Quy tắc nghiệp vụ (đã khóa)

Quy tắc được đánh số **`BR-xxx`**. Service và API phải tuân thủ. Khi tiện lợi mâu thuẫn quy tắc, **ưu tiên quy tắc**.

Tham chiếu: [process-flows.md](./process-flows.md), [api-catalog.md](./api-catalog.md).

---

## Định danh & phân quyền

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-001 | Vai trò cố định: `Admin`, `Manager`, `User`. Không ma trận quyền động trong MVP. | JWT + `RequireRole` |
| BR-002 | `User` không truy cập API lịch quản lý (`/api/v1/schedules/*`). Chỉ xem lịch của mình qua `/api/v1/self/schedule`. | Authorization route |
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
| BR-027 | `GET /api/v1/self/schedule` chỉ trả phân ca của user trong **28 ngày** tới trên lịch **Published**. | `GetMyScheduleAsync` |

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

## Gợi ý lịch

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-070 | `POST .../suggest` không ghi `ShiftAssignment` chính thức và **không** gọi AWS Bedrock. Sau khi gợi ý thành công, API có thể refresh snapshot insight hỗ trợ trong DB. | `ScheduleService`, `ScheduleSuggestionOrchestrator` |
| BR-071 | Suggest/apply chỉ trên lịch **`Draft`**. | Services |
| BR-072 | Auto-scheduling chạy theo từng phòng ban/tuần và cần policy chi nhánh đã lưu, nhân viên active của phòng ban, ca active, và đăng ký ca nếu rule chi nhánh yêu cầu. Policy chi nhánh (`location-scheduling-policy.v3`) gồm 5 luật F&B lõi do thuật toán đọc (`max_shifts_per_week`, `max_shifts_per_day`, `require_role_match`, `require_submitted_preferences`, `unavailable_is_hard_block`) và luật riêng tuỳ chọn Admin/Manager có thể thêm. Thiếu input trả `reason` rõ như `missing_location_rules`, `no_employees`, `no_shifts`, hoặc `missing_preferences`. | `HeuristicScheduleSuggestionService`, `LocationSchedulingPolicyRules` |
| BR-073 | Gợi ý không double-book trong tuần mục tiêu. | `HasOverlapInPlan` |
| BR-074 | Eligibility dùng membership phòng ban. `Employee.DepartmentId` chỉ là phòng ban primary/backward-compatible; guard apply phải chấp nhận mọi membership active của phòng ban lịch. | `EmployeeDepartmentMembership`, `TryPrepareAssignmentAsync` |
| BR-075 | `apply-suggestions` validate **tất cả** dòng rồi commit **một transaction** (cả hoặc không). | `ApplySuggestionsAsync` |
| BR-076 | Context insight lịch là snapshot JSON lưu DB để giải thích, gắn `LocationId`, `DepartmentId`, `WeekStartDate`, và `ExpiresAt`; không phải nguồn dữ liệu chính thức và không thay thế `ShiftAssignment`. | `ScheduleInsightService` |
| BR-077 | Chat insight dùng Bedrock chỉ mang tính hỗ trợ. Nó có thể tóm tắt, giải thích và gợi ý Manager cân nhắc, nhưng không được tạo, cập nhật, apply hoặc publish phân ca. | `ScheduleInsightService.ChatAsync` |
| BR-078 | Bedrock lỗi, hết quota, trả rỗng hoặc timeout không được ảnh hưởng `suggest` hay `apply-suggestions`; chat insight lỗi độc lập bằng response service-unavailable. | `ScheduleInsightService`, `IBedrockService` |
| BR-079 | Tạo/refresh context insight không gọi Bedrock; chỉ serialize lịch, luật, preference, phân ca, gợi ý và metadata tóm tắt hiện có. | `GenerateContextAsync` |

---

## Thông báo & chung

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-080 | Email SMTP khi cấu hình `Smtp:Host` + `Smtp:From`; không thì no-op log. | Infrastructure DI |
| BR-081 | API trả envelope `ApiResponse<T>` + mã `AppMessage`. | Toàn Application |
| BR-082 | Logic nghiệp vụ chỉ ở **Application**; API chỉ map HTTP. | Kiến trúc |
