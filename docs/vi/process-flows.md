# Luồng xử lý (Process Flows)

Tham chiếu cho agents. Mã quy tắc: [business-rules.md](./business-rules.md) (`BR-xxx`).

---

## 1. Triển khai & tenancy

```mermaid
flowchart LR
    Customer[Admin khách hàng] --> Register[POST /auth/register]
    Register --> Org[(Organization tenant)]
    Platform[Wokki PlatformOperator] --> Package[Bật / gia hạn gói]
    Package --> Org
    Org --> API[API nghiệp vụ org]
    API --> DB[(PostgreSQL)]
```

`Organization` là tenant root. Register tạo org và Org Admin, nhưng org mới mặc định chưa có gói sử dụng. Wokki admin phải kích hoạt hoặc gia hạn gói trước khi user trong org đăng nhập/dùng API nghiệp vụ.

### 1.0 Gate gói sử dụng org

```mermaid
sequenceDiagram
    participant C as Admin khách hàng
    participant API as Auth API
    participant P as PlatformOperator
    participant PA as Platform API

    C->>API: POST /auth/register
    API-->>C: JWT Org Admin; package NotActivated
    C->>API: POST /auth/login
    API-->>C: 403 ORG_PACKAGE_NOT_ACTIVATED
    P->>PA: PUT /platform/organizations/{id}/subscription { enabled: true, durationDays }
    PA-->>P: subscriptionStatus Active + expiresAt
    C->>API: POST /auth/login
    API-->>C: accessToken + refreshToken
```

Org hết hạn trả `ORG_PACKAGE_EXPIRED` (402) khi login/refresh và khi gọi API org bằng token cũ. Org chưa kích hoạt hoặc bị tắt trả `ORG_PACKAGE_NOT_ACTIVATED` (403).

---

## 1.1 Quyền truy cập workspace chi nhánh

Org Admin **tạo nhân viên** (email + phòng ban) → hệ thống tự gán **Active** `LocationMembership` tại chi nhánh của phòng ban. Nhân viên **đăng nhập trực tiếp** — **không** có `/join` hay duyệt yêu cầu tham gia.

```mermaid
sequenceDiagram
    participant A as Org Admin
    participant API as Employee API
    participant E as Nhân viên

    A->>API: POST /employees { email, departmentId (User) | locationIds (Manager), ... }
    API-->>A: employeeId + temporaryPassword
    A->>E: Gửi email + mật khẩu tạm
    E->>API: POST /auth/login
    E->>API: GET /location-memberships/my
    Note over E,API: Active membership — vào /app ngay
```

Đổi chi nhánh sau này: Admin/Manager dùng `POST /api/v1/workspace/location/transfer`. Chuyển phòng ban (`/workspace/department/transfer`) chỉ hợp lệ trong chi nhánh Active hiện tại của nhân viên; nếu phòng ban đích thuộc chi nhánh khác thì phải chuyển chi nhánh trước.

---

## 2. Vòng đời lịch (MVP)

```mermaid
stateDiagram-v2
    [*] --> Draft: Tạo / Copy tuần
    Draft --> Published: Publish
    Published --> Draft: Unpublish
    note right of Published: Nhân viên xem /self/schedule\nCho phép đổi ca
```

`ScheduleStatus.Locked` có trong code nhưng **chưa có API** gán trạng thái này.

### Luồng publish

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant S as ScheduleService
    participant N as NotificationService
    participant E as Nhân viên

    M->>API: POST /schedules/{id}/publish
    API->>S: PublishAsync
    S->>S: Draft → Published
    loop Mỗi nhân viên được phân ca
        S->>N: schedule.published
        N-->>E: email hoặc log
    end
    API-->>M: 200 Published
```

### Luồng đăng ký ca (tuần Draft)

Đăng ký ca **chỉ tham khảo**; lịch chính thức = `ShiftAssignment` sau khi Admin publish.

```mermaid
sequenceDiagram
    participant A as Admin/Manager
    participant E as Nhân viên
    participant API as Schedule API
    participant P as SchedulePreferenceService

    A->>API: POST /schedules (Draft, phòng ban + thứ Hai)
    E->>API: GET /self/schedule-preferences/week/{week}
    API-->>E: Lịch Draft + danh sách ca
    E->>API: PUT /self/schedule-preferences/{id}
    E->>API: POST /self/schedule-preferences/{id}/submit
    A->>API: GET /schedules/{id}/preference-board
    API-->>A: NV × ca × ô đăng ký + submittedCount
    A->>API: POST /schedules/{id}/suggest (tuỳ chọn)
    A->>API: POST /schedules/{id}/apply-suggestions (tuỳ chọn)
    A->>API: POST /schedules/{id}/assignments (thủ công)
    A->>API: POST /schedules/{id}/publish
    E->>API: GET /self/schedule
    API-->>E: Chỉ phân ca Published
```

**UI:** Admin **Lịch ca** — stepper + **Bảng đăng ký ca** + **Công bố lịch**. NV **Lịch của tôi → Đăng ký ca** — chọn ô → **Lưu nháp** → **Gửi đăng ký**; tuần Published → chỉ xem; lịch chính thức ở tab **Lịch đã công bố**.

---

## 3. Tạo phân ca

```mermaid
flowchart TD
    A[Yêu cầu tạo phân ca] --> B{Lịch Draft?}
    B -->|không| X[400 Not Draft]
    B -->|có| C{Nhân viên active ở location?}
    C -->|không| X2[400 Sai location]
    C -->|có| C2{Nhân viên đúng department?}
    C2 -->|không| X5[400 Sai department]
    C2 -->|có| D{Mẫu ca đúng phạm vi?}
    D -->|không| X3[400 Sai phạm vi ca]
    D -->|có| E{Trùng / chồng giờ?}
    E -->|có| X4[409 Conflict]
    E -->|không| F[Lưu ShiftAssignment]
```

Validator dùng chung: `ScheduleService.TryPrepareAssignmentAsync` (phân ca thủ công + apply gợi ý).

---

## 4. Đổi ca (Swap)

```mermaid
stateDiagram-v2
    [*] --> Pending: User tạo
    Pending --> PeerDeclined: Đối tác từ chối
    Pending --> Cancelled: Người gửi hủy
    Pending --> ManagerApproved: Đối tác accept auto-apply
    Pending --> ManagerApproved: Manager override duyệt
    Pending --> ManagerRejected: Manager override từ chối
```

### Accept đồng nghiệp (nguyên tử)

```mermaid
sequenceDiagram
    participant T as Nhân viên đối tác
    participant API as Swap API
    participant S as SwapRequestService
    participant DB as Database

    T->>API: POST /swap-requests/{id}/accept
    API->>S: AcceptAsync
    S->>DB: BEGIN TRANSACTION
    S->>S: Pending → PeerAccepted
    S->>S: Đổi EmployeeId hai phân ca
    S->>S: → ManagerApproved
    S->>DB: COMMIT
    S->>S: Thông báo người gửi + đối tác
    API-->>T: 200
```

**BR-034**: cutoff theo `Date` phân ca và `Location.TimeZone`.

---

## 5. Chấm công

```mermaid
sequenceDiagram
    participant U as User
    participant API as Attendance API
    participant S as AttendanceService

    U->>API: POST /attendance/clock-in
    S->>S: Không có bản ghi mở?
    S->>S: Có phân ca published hôm nay?
    S->>S: Insert ClockIn
    API-->>U: 201

    U->>API: POST /attendance/clock-out
    S->>S: Có bản ghi mở
    S->>S: ClockOut + WorkedMinutes
    API-->>U: 200
```

### Chặn điều chỉnh thủ công

```mermaid
flowchart TD
    A[Yêu cầu adjust] --> B{Có ghi chú?}
    B -->|không| X[400]
    B -->|có| C{Kỳ lương locked cho ngày đó?}
    C -->|có| X2[400 Period locked]
    C -->|không| D[Cập nhật giờ + AdjustedBy]
```

---

## 6. Tổng hợp lương

```mermaid
flowchart TD
    R[GET /payroll/summary] --> P{Đã có pay period?}
    P -->|chưa| C[Tạo kỳ Open]
    P -->|rồi| L{Locked + có PayrollLine?}
    L -->|có| S[Trả snapshot PayrollLine]
    L -->|không| A[Tổng phút chấm công × HourlyRate]
    A --> OUT[JSON tổng hợp]
    S --> OUT
```

Export: `POST /payroll/summary/export` → CSV (Admin, tối đa 500 dòng).

---

## 7. Gợi ý lịch (CP-SAT)

Solver MVP = **CP-SAT only** (`useAi` trên suggest bị bỏ qua). Bedrock chỉ chat hỗ trợ (BR-077).

Luồng: suggest (đọc org policy + NV + ca + đăng ký **Submitted** + phân ca hiện có) → context JSON → Admin **Áp dụng** (explicit) → Publish. **Không auto-rebalance** khi NV đổi đăng ký sau apply (BR-086) — banner trên Lịch ca, Admin dùng lại **Tạo gợi ý AI**; CP-SAT chỉ mở khóa NV tự đổi đăng ký hoặc đang conflict Unavailable. Apply gợi ý theo tuple chính xác `(shiftDefinitionId, employeeId, date)`, nên nhiều NV có thể cùng ca/ngày nếu policy cho phép; chỉ xóa phân ca bị omit của nhóm NV bị ảnh hưởng khi request bật clear orphan tuple.

**Xin nghỉ (Draft):** NV `POST /self/leave-requests` → Manager duyệt → Unavailable + xóa phân ca conflict (BR-087).

### Trợ lý insight lịch (Bedrock hỗ trợ)

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant I as ScheduleInsightService
    participant B as AWS Bedrock

    M->>API: POST /schedules/{id}/suggest
    API->>I: GenerateContextAsync sau khi gợi ý thành công
    I->>I: Serialize luật, preference, phân ca, gợi ý, summary
    API-->>M: 200 danh sách gợi ý; context được refresh trong DB

    M->>API: POST /schedules/{id}/insights/chat
    API->>I: ChatAsync(câu hỏi)
    I->>B: Converse với context snapshot
    B-->>I: Giải thích hỗ trợ
    API-->>M: 200 câu trả lời
```

Bedrock không nằm trong bước sinh lịch hoặc apply. Nếu Bedrock không hoạt động, `suggest` và `apply-suggestions` vẫn chạy; chỉ endpoint chat lỗi độc lập.

---

## 8. Chat

```mermaid
sequenceDiagram
    participant U as User
    participant API as Channel API
    participant S as ChannelService
    participant DB as DB
    participant H as SignalR Hub

    U->>API: POST /channels/{id}/messages
    API->>S: SendMessageAsync
    S->>S: Kiểm tra membership
    S->>DB: INSERT message
    S->>H: ReceiveMessage (group)
    API-->>U: 201

    Note over U,H: WS: /ws/chat?access_token=…\nJoinChannel(channelId)
```

---

## 9. Cây quyết định cho agent (sửa code ở đâu)

| Loại thay đổi            | Tầng                                                   |
| ------------------------ | ------------------------------------------------------ |
| Quy tắc / validation mới | `Wokki.Application` service                            |
| Route HTTP mới           | `Wokki.Api/Apis/{Feature}/*Endpoints.cs`               |
| Truy vấn DB mới          | Interface repo `Wokki.Domain` + impl `Infrastructure`  |
| Message hiển thị         | `AppMessages` + service return                         |
| Trạng thái enum mới      | `Wokki.Domain.Enums` + chuyển trạng thái trong service |

Không đặt EF hay quy tắc nghiệp vụ trong handler `Wokki.Api`.
