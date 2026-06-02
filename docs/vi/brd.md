# Tài liệu yêu cầu nghiệp vụ (BRD)

## Wokki Shift Ops MVP — Backend

| | |
|---|---|
| **Tài liệu** | BRD v1.0 (Tiếng Việt) |
| **Sản phẩm** | Wokki Shift Ops |
| **Thành phần** | `wokki-server` (REST + SignalR API) |
| **Trạng thái** | Đã triển khai (Phase 1–5) |
| **Cập nhật** | 2026-06-02 |
| **Bản tiếng Anh** | [../brd.md](../brd.md) |

---

## 1. Tóm tắt điều hành

Wokki Shift Ops là nền tảng vận hành nhân sự **đa tổ chức**. Mỗi org có dữ liệu chi nhánh, phòng ban, user, lịch, chấm công, lương và chat riêng trong cùng API/database theo ranh giới tenant.

Backend cung cấp API phiên bản (`/api/v1`) cho web và mobile-web. Tài liệu này mô tả **làm gì**, **ai dùng**, và **quy tắc không được phá** — logic nằm ở tầng Application.

---

## 2. Bối cảnh kinh doanh

### 2.1 Vấn đề

Đội vận hành mất nhiều thời gian cho lịch thủ công, phiếu chấm công và đổi ca qua kênh không chính thức. Bộ phận lương thiếu nguồn dữ liệu thống nhất gắn với ca đã publish và giờ chấm thực tế.

### 2.2 Mục tiêu kinh doanh

| ID | Mục tiêu | Chỉ số thành công |
|----|----------|-------------------|
| OBJ-01 | Rút ngắn thời gian lập và publish lịch tuần | Manager publish trong app; hỗ trợ copy tuần |
| OBJ-02 | Chuẩn hóa đổi ca có audit | Máy trạng thái swap + thông báo |
| OBJ-03 | Bỏ timesheet thủ công cho nhân viên giờ | Clock-in/out gắn phân ca |
| OBJ-04 | Tăng tốc chuẩn bị lương | Tổng hợp theo department + xuất CSV |
| OBJ-05 | Giảm phụ thuộc chat bên ngoài cho ca | Kênh nội bộ + real-time |
| OBJ-06 | Hỗ trợ manager gợi ý phân ca và giải thích kết quả lịch tuần | Suggest/apply trên lịch Draft; Bedrock insight chat tùy chọn chỉ đọc context lịch |
| OBJ-07 | Giúp Wokki admin vận hành tenant và hỗ trợ khách hàng | Org registry, ledger gói, support console, health và usage analytics |

### 2.3 Mô hình triển khai

- **Organization là tenant root**: dữ liệu nghiệp vụ được scope bằng JWT `organization_id`.
- Khách hàng mới có thể tự đăng ký org, nhưng org mới mặc định chưa có gói sử dụng.
- Wokki admin (`PlatformOperator`) quản lý danh sách user/org toàn hệ thống và bật, tắt hoặc gia hạn gói org.

---

## 3. Stakeholder (các bên liên quan)

| Stakeholder | Vai trò | Quan tâm | Khả năng chính |
|-------------|---------|----------|----------------|
| **Quản trị hệ thống (Admin)** | IT / HR | User, dữ liệu gốc, xuất payroll, tuân thủ | Mọi chi nhánh, API Admin, quản lý user, CSV lương |
| **Wokki Platform Operator** | Admin Wokki | Giám sát platform, kích hoạt/gia hạn gói org, hỗ trợ khách hàng | `/platform`, org registry, ledger gói, support console, diagnostics |
| **Quản lý vận hành (Manager)** | Quản lý chi nhánh/phòng ban | Lịch, duyệt đổi ca, xem chấm công trong chi nhánh được gán | Workspace chi nhánh được gán, lịch, phân ca, override swap, payroll view |
| **Nhân viên (User)** | Vận hành tuyến đầu | Xem ca, đổi ca, chấm công | `/self/*`, swap, clock |
| **Nhân sự lương** | Tài chính (có thể dùng Admin) | Tổng kỳ, xuất file | Payroll summary + export |
| **Product owner** | Chủ sản phẩm | Phạm vi MVP, lộ trình | `plans/shift-ops-mvp/` |
| **Kỹ sư / AI agents** | Xây dựng & bảo trì | Đúng quy tắc, contract ổn định | BRD + `business-rules.md` |

---

## 4. Phạm vi

### 4.1 Trong phạm vi (MVP backend)

| Phase | Năng lực |
|-------|----------|
| 1 | Role Manager, employee, location, department |
| 2 | Mẫu ca, lịch tuần, publish/unpublish, copy tuần, `/self/schedule` |
| 3 | Yêu cầu đổi ca, accept/decline, manager override, thông báo |
| 4 | Clock-in/out, danh sách/điều chỉnh chấm công, tổng hợp lương & CSV |
| 5 | Chat (REST + SignalR), gợi ý lịch, trợ lý Bedrock insight tùy chọn |
| Platform | Control center cho Wokki admin: org registry, subscription ledger, support console, health, usage analytics |

### 4.2 Ngoài phạm vi (MVP)

| Hạng mục | Lý do |
|----------|-------|
| App iOS/Android native | Client gọi API; UI không nằm repo này |
| Thanh toán/checkout self-service | Gói org do Wokki admin kích hoạt thủ công qua platform API |
| RBAC động / role tùy chỉnh | Cố định Admin / Manager / User |
| LLM ngoài cho lịch | Bedrock chỉ hỗ trợ insight/chat; không sinh hoặc apply phân ca |
| SignalR Redis backplane | MVP một node |
| API khóa kỳ lương công khai | Khóa qua trạng thái DB |
| Workflow `ScheduleStatus.Locked` | Có enum; API chỉ Draft/Published |
| UI audit log đầy đủ | Entity có; chưa wire hết hành động |

### 4.3 Giả định

- Client lấy JWT qua `/api/v1/auth/login`.
- User thao tác ca/chấm công/chat phải có `Employee` liên kết.
- Admin quản lý mọi chi nhánh. Manager chỉ có quyền trên chi nhánh được gán qua `LocationManager`; nhân viên cần Active `LocationMembership` trước khi vào workspace được bảo vệ.
- `Location.TimeZone` là mã IANA hợp lệ cho cutoff đổi ca.
- PostgreSQL là nguồn dữ liệu chính.
- SMTP cấu hình qua secrets khi cần email.

### 4.4 Ràng buộc

- Clean Architecture: không logic nghiệp vụ trong `Wokki.Api`.
- Service trả `ApiResponse<T>`.
- Module API: `Apis/{Feature}/`, đăng ký trong `PipelineExtensions.MapEndpoints()`.

---

## 5. Yêu cầu chức năng (FR)

**P1** = bắt buộc cho MVP.

### 5.1 Định danh & tổ chức (Phase 1)

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-101 | P1 | Hỗ trợ vai trò Admin, Manager, User. |
| FR-102 | P1 | Admin CRUD user và employee (tạo employee kèm user). |
| FR-103 | P1 | Admin quản lý mọi location/department; Manager chỉ quản lý location được gán qua `LocationManager`. |
| FR-104 | P1 | Employee lưu chức danh, đơn giá giờ, department, ngày chấm dứt. |
| FR-105 | P1 | Org Admin tạo staff qua workflow nhân viên kèm `DepartmentId`; hệ thống tạo cả tài khoản User và hồ sơ Employee, rồi tự tạo Active `LocationMembership` tại chi nhánh phòng ban. User legacy cùng org chưa có Employee có thể được liên kết qua workflow này. Không có tài khoản staff trần. |
| FR-106 | P1 | Register anonymous tạo Organization + Org Admin, nhưng org chưa dùng được cho tới khi Wokki admin kích hoạt gói. |
| FR-108 | P1 | Nhân viên tự đăng ký (`POST /auth/register-employee`), xem danh bạ org (`GET /organizations/directory`), gửi một yêu cầu tham gia org. Admin duyệt (gán phòng ban) hoặc từ chối. Song song FR-105. Xem [fe/org-join-request-handoff.md](../fe/org-join-request-handoff.md). |
| FR-107 | P1 | PlatformOperator xem user/org toàn hệ thống và bật, tắt hoặc gia hạn gói org — số ngày do Wokki admin chọn trên console (`durationDays`). |
| FR-109 | P1 | PlatformOperator dùng Platform Control Center để lọc org theo trạng thái gói, xem lịch sử gói, tra cứu support context, và xem diagnostics/usage analytics chỉ dành cho platform. |

### 5.2 Lập lịch (Phase 2)

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-201 | P1 | Manager tạo lịch tuần theo department (bắt đầu thứ Hai). |
| FR-202 | P1 | Manager chỉ phân ca cho nhân viên có Active membership đúng chi nhánh của lịch và membership đúng phòng ban của lịch (chỉ Draft). |
| FR-203 | P1 | Manager publish/unpublish lịch. |
| FR-204 | P1 | Manager copy tuần sang Draft mới. |
| FR-205 | P1 | Nhân viên xem ca sắp tới qua `/self/schedule`. |
| FR-206 | P1 | Chặn trùng khung giờ cùng nhân viên/ngày. |

### 5.3 Đổi ca (Phase 3)

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-301 | P1 | Nhân viên tạo yêu cầu đổi giữa hai phân ca published. |
| FR-302 | P1 | Đối tác accept hoặc decline. |
| FR-303 | P1 | Accept đổi ownership phân ca nguyên tử và hoàn tất yêu cầu. |
| FR-304 | P1 | Manager override approve/reject. |
| FR-305 | P1 | Áp dụng cutoff theo múi giờ location. |
| FR-306 | P1 | Gửi thông báo khi đổi ca và khi publish lịch. |

### 5.4 Chấm công & lương (Phase 4)

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-401 | P1 | Clock-in chỉ khi có phân ca published hôm nay. |
| FR-402 | P1 | Clock-out bản ghi mở; hệ thống tính phút. |
| FR-403 | P1 | Manager xem danh sách và điều chỉnh chấm công (có ghi chú). |
| FR-404 | P1 | Chặn điều chỉnh khi kỳ lương locked cho ngày đó. |
| FR-405 | P1 | Manager xem tổng hợp lương theo department & kỳ. |
| FR-406 | P1 | Admin xuất CSV (tối đa 500 dòng). |

### 5.5 Chat & gợi ý (Phase 5)

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-501 | P1 | User đã đăng nhập xem danh sách kênh tham gia. |
| FR-502 | P1 | Manager tạo kênh Direct/Group. |
| FR-503 | P1 | Member đọc (cursor) và gửi tin; xóa mềm giữ placeholder. |
| FR-504 | P1 | Real-time qua SignalR `/ws/chat` + JWT. |
| FR-505 | P1 | Manager gợi ý phân ca không ghi DB. |
| FR-506 | P1 | Manager apply gợi ý lên lịch Draft trong một transaction. |
| FR-507 | P1 | Manager/Admin tạo snapshot context insight cho lịch tuần mà không gọi Bedrock và không đổi phân ca. |
| FR-508 | P1 | Manager/Admin hỏi Bedrock dựa trên context lịch đã tạo; câu trả lời chỉ hỗ trợ và không được mutate lịch. |

### 5.6 Platform control center

| ID | Ưu tiên | Yêu cầu |
|----|---------|---------|
| FR-601 | P1 | PlatformOperator lọc/sort org registry theo trạng thái gói, tên org, ngày hết hạn, và cửa sổ "sắp hết hạn". |
| FR-602 | P1 | Mỗi lần cập nhật gói qua platform phải ghi subscription ledger bất biến và `AuditLog` trong cùng transaction với thay đổi gói. |
| FR-603 | P1 | PlatformOperator xem lịch sử ledger toàn platform hoặc theo từng org. Ledger chỉ lưu lịch sử trạng thái/thời hạn; chưa lưu doanh thu hay billing ngoài. |
| FR-604 | P1 | PlatformOperator tìm theo org id, tên org hoặc email user và đọc support context mà không impersonate, không sửa dữ liệu nghiệp vụ tenant, không xóa dữ liệu tenant. |
| FR-605 | P2 | PlatformOperator xem platform health cho API, Bedrock và email diagnostics. Public `/health` không đổi. |
| FR-606 | P2 | PlatformOperator xem usage analytics theo window thời gian. Active org dựa trên login thành công, publish/suggest/apply lịch, clock-in/out, hoặc gửi tin chat. |

---

## 6. Quy tắc nghiệp vụ

Chi tiết đầy đủ: **[business-rules.md](./business-rules.md)** (`BR-xxx`).

Tóm tắt:

1. **Truy cập** — ma trận vai trò; User chỉ self-service; chặn org chưa kích hoạt hoặc hết hạn gói.
2. **Lịch** — sửa Draft; Published để xem/đổi ca; chống trùng giờ.
3. **Đổi ca** — máy trạng thái, cutoff, apply nguyên tử khi accept.
4. **Chấm công** — một bản ghi mở; audit khi điều chỉnh; khóa kỳ chặn sửa.
5. **Lương** — phút × đơn giá; snapshot khi kỳ locked có dòng.
6. **Chat** — membership; làm sạch HTML; xóa mềm có placeholder.
7. **Gợi ý** — suggest read-only; apply transactional; ngưỡng lịch sử.
8. **Platform operations** — ledger/audit gói, support console read-only, diagnostics chỉ PlatformOperator, active org analytics.

---

## 7. Yêu cầu phi chức năng (NFR)

| ID | Nhóm | Yêu cầu |
|----|------|---------|
| NFR-01 | Bảo mật | JWT cho REST và SignalR; redact `access_token` trong log |
| NFR-02 | Bảo mật | Chat loại HTML; giới hạn độ dài tin |
| NFR-03 | Tin cậy | Thông báo không rollback transaction chính |
| NFR-04 | Hiệu năng | Rate limit API; cao hơn cho clock |
| NFR-05 | Mở rộng | SignalR một node (chưa backplane) |
| NFR-06 | Bảo trì | Ranh giới Clean Architecture |
| NFR-07 | Quan sát | Serilog + correlation id |
| NFR-08 | API | OpenAPI/Scalar; envelope `ApiResponse` thống nhất |
| NFR-09 | Dữ liệu | EF migrations; PostgreSQL |
| NFR-10 | i18n | Mã `AppMessages` tiếng Anh; mô tả OpenAPI có thể tiếng Việt |

---

## 8. Hành trình người dùng (tóm tắt)

### 8.1 Manager — publish tuần

1. Tạo lịch Draft (department + thứ Hai).
2. Thêm phân ca (hoặc suggest → apply).
3. Publish → thông báo nhân viên → hiện trên `/self/schedule`.

### 8.2 Nhân viên — đổi ca

1. Xem `/self/schedule`.
2. Tạo swap với ca đồng nghiệp.
3. Đồng nghiệp accept → hệ thống đổi phân ca tự động.

### 8.3 Nhân viên — ngày làm việc

1. Clock-in (có ca hôm nay).
2. Clock-out cuối ca.
3. (Tuỳ chọn) Đọc/gửi chat kênh nhóm.

### 8.4 Admin — lương

1. Gọi payroll summary (department + khoảng ngày).
2. Xuất CSV cho hệ thống tài chính.

### 8.5 Wokki admin — kích hoạt gói org

1. Đăng nhập bằng `PlatformOperator`.
2. Xem danh sách user/org toàn hệ thống.
3. Bật hoặc gia hạn gói org — Wokki admin chọn `durationDays` trên console platform (FE không mặc định 30 ngày).
4. Kiểm tra ledger/audit bất biến của thay đổi gói.
5. Dùng support search/context, platform health và usage analytics để hỗ trợ khách hàng.

Sơ đồ chi tiết: **[process-flows.md](./process-flows.md)**.

---

## 9. Giao diện hệ thống

| Giao diện | Chi tiết |
|-----------|----------|
| REST | `/api/v1/*` — [api-catalog.md](./api-catalog.md) |
| WebSocket | SignalR `/ws/chat` |
| Email | SMTP tuỳ chọn (`Smtp`) |
| Database | PostgreSQL qua EF Core |

---

## 10. Khái niệm dữ liệu

| Thực thể | Mục đích |
|----------|----------|
| User, Employee | Đăng nhập + danh tính nhân sự |
| Location, Department | Cơ cấu tổ chức |
| ShiftDefinition | Mẫu ca |
| Schedule, ShiftAssignment | Kế hoạch tuần |
| SwapPost | Bảng tin đổi ca (Draft, Cover/CrossSwap, FCFS) |
| AttendanceRecord | Chấm công |
| PayPeriod, PayrollLine | Lương |
| Channel, ChannelMember, Message | Chat |
| OrganizationSubscriptionLedgerEntry | Lịch sử thay đổi gói bất biến của platform |
| PlatformActivityEvent | Tín hiệu usage analytics của platform |

Thuật ngữ: **[glossary.md](./glossary.md)**.

---

## 11. Truy vết (traceability)

| Phần BRD | Kế hoạch code |
|----------|---------------|
| Phase 1 | `plans/shift-ops-mvp/phase-01-foundation.md` |
| Phase 2 | `phase-02-scheduling.md` |
| Phase 3 | `phase-03-swap-workflow.md` |
| Phase 4 | `phase-04-attendance-payroll.md` |
| Phase 5 | `phase-05-chat-ai.md` |
| Platform Control Center | `plans/platform-admin-control-center/plan.md` |

| Phần BRD | Trong codebase |
|----------|----------------|
| Services | `src/Wokki.Application/Services/{Feature}/` |
| Quy tắc / message | `business-rules.md`, `AppMessages.cs` |
| HTTP | `src/Wokki.Api/Apis/` |
| Enum | `src/Wokki.Domain/Enums/` |

---

## 12. Hạng mục mở & tương lai

| Chủ đề | Quyết định MVP | Tương lai |
|--------|----------------|-----------|
| `ScheduleStatus.Locked` | Chỉ enum | Khóa sau ký duyệt lương |
| API khóa kỳ lương | Trạng thái DB | Endpoint Admin lock |
| Audit log | Entity một phần | Wire đủ hành động nhạy cảm |
| Insight lịch | Bedrock chat tùy chọn trên JSON context | RAG/file storage, diagnostic và analytics sâu hơn |
| Tự động thanh toán/gia hạn | Wokki admin kích hoạt/gia hạn thủ công | Tích hợp thanh toán và gia hạn tự động |
| Scale SignalR | Một server | Redis / Azure SignalR |

---

## 13. Phê duyệt & thay đổi

Thay đổi **quy tắc khóa** (`BR-xxx`) cần phê duyệt product và cập nhật đồng bộ:

1. `docs/vi/business-rules.md` (và bản EN nếu có)
2. `docs/vi/brd.md`
3. `docs/vi/process-flows.md` nếu luồng đổi
4. Application services và `AppMessages`

Agents: khi implement, nêu rõ `FR-` / `BR-` đã đáp ứng trong PR hoặc commit.
