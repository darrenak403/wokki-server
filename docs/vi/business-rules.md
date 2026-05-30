# Quy tắc nghiệp vụ (đã khóa)

Quy tắc được đánh số **`BR-xxx`**. Service và API phải tuân thủ. Khi tiện lợi mâu thuẫn quy tắc, **ưu tiên quy tắc**.

Tham chiếu: [process-flows.md](./process-flows.md), [api-catalog.md](./api-catalog.md).

---

## Định danh & phân quyền

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-001 | Vai trò cố định: `Admin`, `Manager`, `User`, và `PlatformOperator` (chỉ platform). Không ma trận quyền động trong MVP. | JWT + `RequireRole` |
| BR-002 | `User` không truy cập API lịch quản lý (`/api/v1/schedules/*`). Chỉ xem lịch của mình qua `/api/v1/self/schedule`. | Authorization route |
| BR-003 | `Admin` quản lý user, xuất payroll, xóa mềm mọi tin nhắn chat. | `ChannelService`, `PayrollEndpoints` |
| BR-004 | `Manager` quản lý lịch, phân ca, ghi đè đổi ca, điều chỉnh chấm công, tạo kênh chat. | Authorization route |
| BR-005 | Thao tác nhân viên cần bản ghi `Employee` gắn với `User` đăng nhập. | Service trả `*_NO_EMPLOYEE` / 404 |
| BR-006 | `Admin` có toàn quyền phạm vi chi nhánh **trong tổ chức của mình** và được xem/quản lý mọi workspace `Location` thuộc org đó. | `LocationScopeService`, `IOrganizationScopeService` |
| BR-007 | Phạm vi `Manager` chỉ là các chi nhánh được gán qua `LocationManager`. Không suy quyền Manager từ role global, role claim đơn thuần, hoặc membership employee/department của chính Manager. | `LocationScopeService`, query list theo scope |
| BR-008 | Org Admin tạo staff qua `POST /api/v1/employees`; một workflow tạo cả `User` và `Employee`. **Role `User` bắt buộc `DepartmentId`** (chi nhánh + phòng ban hiện tại) và tự tạo **Active** `LocationMembership` tại chi nhánh của phòng ban đó. **Role `Manager` bắt buộc `LocationIds`** (ít nhất một chi nhánh) — hệ thống tạo `LocationManager` lúc tạo; Manager không cần phòng ban. Khi đăng nhập, **User** và **Manager** chỉ thấy chi nhánh họ có quyền truy cập (`LocationMembership` / `LocationManager`); chi nhánh không được gán sẽ không hiện. Nếu đã có `User` legacy cùng org nhưng chưa có `Employee`, workflow này sẽ liên kết user đó thành hồ sơ nhân viên. Không tạo tài khoản org staff trần qua `/users`. Nhân viên đăng nhập vào `/app` trực tiếp — không có luồng join/pending. | `EmployeeService.CreateAsync`, `EmployeeService.AssignManagerLocationsAsync`, `EmployeeService.EnsureActiveLocationMembershipAsync` |
| BR-009 | Đổi chi nhánh nhân viên hiện có dùng workspace transfer (`POST /api/v1/workspace/location/transfer`) bởi Admin/Manager — không qua join request. Phòng ban: `POST /api/v1/workspace/department/transfer` và phải thuộc chi nhánh Active của nhân viên; muốn sang phòng ban chi nhánh khác phải chuyển chi nhánh trước. | `WorkspaceService.TransferLocationAsync`, `WorkspaceService.TransferDepartmentAsync` |

---

## Tổ chức & dữ liệu gốc

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-010 | `Department` thuộc đúng một `Location`. | EF FK |
| BR-011 | Nhân viên phải có Active `LocationMembership` đúng chi nhánh và membership phòng ban Active trước khi được phân ca. `Employee.DepartmentId` là phòng ban chính hiện tại. Chuyển phòng ban chỉ trong cùng chi nhánh: `LocationId` của phòng ban đích phải khớp Active `LocationMembership` của nhân viên. Khi chuyển phòng ban: đóng dòng Active (`Status=Transferred`, ghi `LeftAt`); quay lại phòng ban cũ thì tạo dòng mới — không ghi đè lịch sử. Thời gian ở phòng ban đọc từ `JoinedAt`/`LeftAt` trên từng dòng `employee_department_memberships`. | `TryPrepareAssignmentAsync`, `WorkspaceService.TransferDepartmentAsync`, `GET /api/v1/employees/{id}/department-memberships` |
| BR-012 | Nhân viên đã chấm dứt (`TerminatedAt`) không được phân ca mới hoặc làm đối tác đổi ca. | `EmployeeService`, validator |
| BR-013 | `ShiftDefinition` phải khớp phạm vi lịch: cùng `LocationId`; nếu có `DepartmentId` thì phải bằng department của lịch. | `TryPrepareAssignmentAsync` |
| BR-014 | Tenant root là `Organization`. Dữ liệu nghiệp vụ có `OrganizationId`; user org phải có JWT claim `organization_id`. Không dùng `organizationId` từ body để authorize. | `OrganizationContextMiddleware`, `IOrganizationScopeService` |
| BR-015 | `PlatformOperator` (seed `admin@gmail.com`) có `OrganizationId = null`, chỉ dùng các route platform (`/api/v1/platform/*`) — không vào route nghiệp vụ org. | `StatsService`, `PlatformAdminService`, route auth |
| BR-016 | `POST /register` tạo atomic `Organization` + Org Admin (`Admin`) + JWT. Một email = một org; email trùng → 409. Org mới mặc định chưa có gói sử dụng cho tới khi Wokki admin kích hoạt. | `AuthService.RegisterAsync` |
| BR-017 | Chuyển chi nhánh phải `location.OrganizationId == employee.organizationId`. Truy cập cross-tenant → 404. | `WorkspaceService`, `EmployeeService` |
| BR-018 | Org stats (`GET /api/v1/org/stats`) cho `Admin` + `Manager`; platform stats cho `PlatformOperator` only. | `StatsService`, endpoint auth |
| BR-019 | Wokki admin (`PlatformOperator`) được xem danh sách user/org toàn hệ thống và bật, tắt hoặc gia hạn gói org. Thời hạn gói do admin chọn qua `durationDays` trên console `/platform` (FE không hardcode mặc định 30 ngày). Gia hạn: có thể gửi `durationDays` mới; bỏ qua thì BE dùng `subscriptionDurationDays` đã lưu của org. User thuộc org không được login, refresh token hoặc gọi API nghiệp vụ khi org chưa kích hoạt gói (`ORG_PACKAGE_NOT_ACTIVATED`) hoặc đã hết hạn (`ORG_PACKAGE_EXPIRED`). | `PlatformAdminService`, `OrganizationSubscriptionService`, `OrganizationContextMiddleware`, `AuthService` |

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
| BR-026 | Khi publish, gửi thông báo `schedule.published` cho nhân viên được phân ca (không rollback nếu gửi lỗi). Email **tiếng Việt**: tiêu đề có khoảng tuần; nội dung liệt kê từng ngày **Thứ …, dd/MM/yyyy** kèm tên ca và giờ (không gửi JSON thô). | `PublishAsync`, `NotificationEmailComposer` |
| BR-027 | `GET /api/v1/self/schedule` chỉ trả phân ca của user trong **28 ngày** tới trên lịch **Published**. | `GetMyScheduleAsync` |
| BR-028 | Đăng ký ca **chỉ mang tính tham khảo** — tách khỏi `ShiftAssignment`; Admin/Manager quyết định phân ca cuối trước khi publish. | `SchedulePreferenceService`, `ScheduleService` |
| BR-029 | Nhân viên lưu/cập nhật đăng ký ca khi lịch **Draft**; sau **Published** chỉ xem, không sửa. | `SaveMineAsync`, `SubmitMineAsync` |
| BR-080 | **Luồng chính (F&B):** Admin tạo Draft → NV gửi đăng ký (`/self/schedule-preferences/*`) → Admin xem `preference-board` → gợi ý/phân ca → publish. Đăng ký không tự thành phân ca. FE phải chuẩn hóa `ScheduleStatus` (BE có thể trả string `"Draft"`/`"Published"`). | `SchedulePanel`, `SchedulePreferenceService`, FE `normalizeScheduleStatus` |

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
| BR-072 | Auto-scheduling theo phòng ban/tuần; cần nhân viên active, ca active, đăng ký ca nếu bật luật org enforced. **Policy org** (`org-scheduling-policy.v1`) dùng chung mọi chi nhánh; catalog `GET /scheduling/rule-catalog`; Admin `GET/PUT /org/scheduling-policy` (Manager chỉ đọc). **4 luật solver enforced** (F&B nhỏ vừa): đăng ký ca, số người tối thiểu/ca, khớp vai trò. Lấp đủ người, nghỉ giữa ca, trần ca/tuần: `SchedulingSolverDefaults` (không hiện trên UI org). Tối đa **20 luật advisory** (solver không đọc). Chưa lưu policy thì dùng catalog mặc định. Apply gợi ý luôn explicit trên UI. | `CpSatScheduleSuggestionService`, `OrganizationSchedulingPolicyRules`, `OrganizationSchedulingSolverPolicy`, `SchedulingRuleCatalog` |
| BR-073 | Gợi ý không double-book trong tuần mục tiêu. | `HasOverlapInPlan` |
| BR-074 | Eligibility của auto-scheduling dùng Active branch membership cùng với membership phòng ban. `Employee.DepartmentId` chỉ là phòng ban primary/backward-compatible; guard apply phải chấp nhận mọi membership phòng ban active của lịch. | `LocationMembership`, `EmployeeDepartmentMembership`, `TryPrepareAssignmentAsync` |
| BR-075 | `apply-suggestions` validate **tất cả** dòng rồi commit **một transaction** (cả hoặc không). | `ApplySuggestionsAsync` |
| BR-076 | Context insight lịch là snapshot JSON lưu DB để giải thích, gắn `LocationId`, `DepartmentId`, `WeekStartDate`, và `ExpiresAt`; không phải nguồn dữ liệu chính thức và không thay thế `ShiftAssignment`. | `ScheduleInsightService` |
| BR-077 | Sao chép mẫu ca chỉ trong **một chi nhánh**: phòng ban nguồn và đích phải thuộc cùng `LocationId`. Chỉ copy ca **active** gắn `DepartmentId` nguồn; ca trùng ở đích (cùng tên + giờ bắt đầu/kết thúc, tên không phân biệt hoa thường) được bỏ qua. Tạo **bản ghi `ShiftDefinition` mới** — lịch/phân ca cũ không đổi. | `ShiftDefinitionService.CopyAsync` |
| BR-077 | Chat insight dùng Bedrock chỉ mang tính hỗ trợ. Nó có thể tóm tắt, giải thích và gợi ý Manager cân nhắc, nhưng không được tạo, cập nhật, apply hoặc publish phân ca. | `ScheduleInsightService.ChatAsync` |
| BR-078 | Bedrock lỗi, hết quota, trả rỗng hoặc timeout không được ảnh hưởng `suggest` hay `apply-suggestions`; chat insight lỗi độc lập bằng response service-unavailable. | `ScheduleInsightService`, `IBedrockService` |
| BR-079 | Tạo/refresh context insight không gọi Bedrock; chỉ serialize lịch, luật, preference, phân ca, gợi ý và metadata tóm tắt hiện có. | `GenerateContextAsync` |
| BR-086 | Gợi ý CP-SAT qua `ScheduleSuggestionContextLoader`. Không auto-apply / auto-rebalance khi đăng ký hoặc xin nghỉ thay đổi sau apply — `rebalanceHints` trên `GET /schedules/{id}`; Admin dùng lại **Tạo gợi ý AI**. | `ScheduleRebalanceAnalyzer`, `CpSatScheduleSuggestionService` |
| BR-087 | **Xin nghỉ chỉ Draft:** NV gửi `/self/leave-requests`, Manager duyệt → Unavailable + xóa phân ca conflict; không auto CP-SAT. Published → chặn form/API. | `ScheduleLeaveRequestService` |

---

## Xác thực & mật khẩu

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-083 | Org Admin tạo nhân viên với mật khẩu tạm → `User.MustChangePassword = true`. Login/refresh trả `mustChangePassword`. FE hiện **banner cảnh báo liên tục** (theo cờ này) cho đến khi đổi mật khẩu thành công — không chặn app. API xóa cờ qua `POST /auth/reset-password`. | `EmployeeService`, `AuthService.ResetPasswordAsync` |
| BR-084 | Quên mật khẩu (anonymous) gồm 3 bước: gửi OTP → xác minh OTP → đặt mật khẩu mới. OTP hết hạn sau **1 phút**. Trong thời gian OTP còn hiệu lực, không cho gửi lại (`AUTH_OTP_RESEND_TOO_SOON`, 429). Trạng thái OTP lưu trong **Redis** (TTL 1 phút); OTP đã dùng xong bị xóa khi đặt lại mật khẩu thành công. | `AuthService`, `IAuthOtpStore`, `RedisAuthOtpStore`, `AuthOtpHelper` |
| BR-085 | Giới hạn gửi OTP theo email trong Redis: tối đa **5** lần; lần thứ 6 khóa **30 phút** (`AUTH_OTP_SEND_LOCKED`, 429). Đếm reset sau khi đặt lại mật khẩu thành công. API gửi OTP luôn trả success chung (không lộ email tồn tại). | `IAuthOtpStore`, `AuthService.ForgotPasswordAsync` |

---

## Thông báo & chung

| ID | Quy tắc | Thực thi |
|----|---------|---------|
| BR-080 | Email SMTP khi cấu hình `Smtp:Host` + `Smtp:From`; không thì no-op log. | Infrastructure DI |
| BR-081 | API trả envelope `ApiResponse<T>` + mã `AppMessage`. | Toàn Application |
| BR-082 | Logic nghiệp vụ chỉ ở **Application**; API chỉ map HTTP. | Kiến trúc |
