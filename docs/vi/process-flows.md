# Luồng xử lý (Process Flows)

Tham chiếu cho agents. Mã quy tắc: [business-rules.md](./business-rules.md) (`BR-xxx`).

---

## 1. Triển khai & tenancy

```mermaid
flowchart LR
    DN[Doanh nghiệp] --> Instance[Instance Wokki riêng]
    Instance --> DB[(PostgreSQL)]
    Instance --> API[Wokki.Api]
```

Một công ty một môi trường. Không chia sẻ database đa tenant trong MVP.

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

---

## 3. Tạo phân ca

```mermaid
flowchart TD
    A[Yêu cầu tạo phân ca] --> B{Lịch Draft?}
    B -->|không| X[400 Not Draft]
    B -->|có| C{Nhân viên đúng department?}
    C -->|không| X2[400 Sai department]
    C -->|có| D{Mẫu ca đúng phạm vi?}
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

## 7. Gợi ý lịch (heuristic)

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant H as HeuristicScheduleSuggestionService

    M->>API: POST /schedules/{id}/suggest
    API->>H: GenerateAsync chỉ đọc
    alt lịch sử < 3 phân ca
        H-->>API: rỗng + insufficient_history
    else
        H-->>API: danh sách gợi ý xếp hạng
    end
    API-->>M: 200 không ghi DB

    M->>API: POST /schedules/{id}/apply-suggestions
    API->>API: Validate tất cả dòng
    API->>API: Transaction ghi hàng loạt
    API-->>M: 201 phân ca
```

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

| Loại thay đổi | Tầng |
|---------------|------|
| Quy tắc / validation mới | `Wokki.Application` service |
| Route HTTP mới | `Wokki.Api/Apis/{Feature}/*Endpoints.cs` |
| Truy vấn DB mới | Interface repo `Wokki.Domain` + impl `Infrastructure` |
| Message hiển thị | `AppMessages` + service return |
| Trạng thái enum mới | `Wokki.Domain.Enums` + chuyển trạng thái trong service |

Không đặt EF hay quy tắc nghiệp vụ trong handler `Wokki.Api`.
