# Process Flows

Visual reference for agents. Rule IDs refer to [business-rules.md](./business-rules.md).

---

## 1. Deployment & tenancy

```mermaid
flowchart LR
    Customer[Customer admin] --> Register[POST /auth/register]
    Register --> Org[(Organization tenant)]
    Platform[Wokki PlatformOperator] --> Package[Enable / renew package]
    Package --> Org
    Org --> API[Org business APIs]
    API --> DB[(PostgreSQL)]
```

`Organization` is the tenant root. Register creates the org and Org Admin, but the org starts without an activated package. Wokki admin activates or renews the org before org users can log in/use org APIs.

### 1.0 Org package gate

```mermaid
sequenceDiagram
    participant C as Customer admin
    participant API as Auth API
    participant P as PlatformOperator
    participant PA as Platform API

    C->>API: POST /auth/register
    API-->>C: Org Admin JWT; package NotActivated
    C->>API: POST /auth/login
    API-->>C: 403 ORG_PACKAGE_NOT_ACTIVATED
    P->>PA: PUT /platform/organizations/{id}/subscription { enabled: true, durationDays }
    PA-->>P: subscriptionStatus Active + expiresAt
    C->>API: POST /auth/login
    API-->>C: accessToken + refreshToken
```

Expired org packages return `ORG_PACKAGE_EXPIRED` (402) on login/refresh and authenticated org API calls. Disabled or not-yet-activated packages return `ORG_PACKAGE_NOT_ACTIVATED` (403).

---

## 1.1 Branch workspace access

Org Admin **tạo nhân viên** (email + phòng ban) → hệ thống tự gán **Active** `LocationMembership` tại chi nhánh của phòng ban. Nhân viên **đăng nhập trực tiếp** vào app — **không** có luồng `/join` hay duyệt yêu cầu tham gia.

```mermaid
sequenceDiagram
    participant A as Org Admin
    participant API as Employee API
    participant E as Employee

    A->>API: POST /employees { email, departmentId (User) | locationIds (Manager), ... }
    API-->>A: employeeId + temporaryPassword
    A->>E: Gửi email + mật khẩu tạm
    E->>API: POST /auth/login
    E->>API: GET /location-memberships/my
    Note over E,API: Active membership — vào /app ngay
```

Đổi chi nhánh sau này: Admin/Manager dùng `POST /api/v1/workspace/location/transfer`. Admin quản mọi chi nhánh trong org; Manager chỉ scope `LocationManager`. Chuyển phòng ban (`/workspace/department/transfer`) chỉ hợp lệ trong chi nhánh Active hiện tại của nhân viên; đổi chi nhánh trước nếu phòng ban đích thuộc chi nhánh khác.

---

## 2. Schedule lifecycle (MVP)

```mermaid
stateDiagram-v2
    [*] --> Draft: Create / Copy week
    Draft --> Published: Publish
    Published --> Draft: Unpublish
    note right of Published: Employees see /self/schedule\nSwaps allowed
```

`ScheduleStatus.Locked` exists in code but **no API sets it yet** (future: lock after payroll close).

### Publish flow

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant S as ScheduleService
    participant N as NotificationService
    participant E as Employees

    M->>API: POST /schedules/{id}/publish
    API->>S: PublishAsync
    S->>S: Draft → Published
    loop Each assigned employee
        S->>N: schedule.published
        N-->>E: email or log
    end
    API-->>M: 200 Published
```

---

## 3. Assignment creation

```mermaid
flowchart TD
    A[Create assignment request] --> B{Schedule Draft?}
    B -->|no| X[400 Not Draft]
    B -->|yes| C{Employee active in location?}
    C -->|no| X2[400 Wrong location]
    C -->|yes| C2{Employee in dept?}
    C2 -->|no| X5[400 Wrong dept]
    C2 -->|yes| D{Shift in scope?}
    D -->|no| X3[400 Shift scope]
    D -->|yes| E{Overlap / duplicate?}
    E -->|yes| X4[409 Conflict]
    E -->|no| F[Insert ShiftAssignment]
```

Shared validator: `ScheduleService.TryPrepareAssignmentAsync` (manual assign + apply-suggestions).

---

## 4. Shift swap

```mermaid
stateDiagram-v2
    [*] --> Pending: User creates
    Pending --> PeerDeclined: Target declines
    Pending --> Cancelled: Requester cancels
    Pending --> ManagerApproved: Target accepts auto-apply
    Pending --> ManagerApproved: Manager override approve
    Pending --> ManagerRejected: Manager override reject
```

### Peer accept (atomic)

```mermaid
sequenceDiagram
    participant T as Target employee
    participant API as Swap API
    participant S as SwapRequestService
    participant DB as Database

    T->>API: POST /swap-requests/{id}/accept
    API->>S: AcceptAsync
    S->>DB: BEGIN TRANSACTION
    S->>S: Pending → PeerAccepted
    S->>S: Swap EmployeeId on both assignments
    S->>S: → ManagerApproved
    S->>DB: COMMIT
    S->>S: Notify requester + target
    API-->>T: 200
```

**BR-034** cutoff uses assignment `Date` and `Location.TimeZone`.

---

## 5. Attendance

```mermaid
sequenceDiagram
    participant U as User
    participant API as Attendance API
    participant S as AttendanceService

    U->>API: POST /attendance/clock-in
    S->>S: No open record?
    S->>S: Published assignment today?
    S->>S: Insert ClockIn
    API-->>U: 201

    U->>API: POST /attendance/clock-out
    S->>S: Open record exists
    S->>S: Set ClockOut + WorkedMinutes
    API-->>U: 200
```

### Manual adjust guard

```mermaid
flowchart TD
    A[Adjust request] --> B{Note provided?}
    B -->|no| X[400]
    B -->|yes| C{Pay period locked for date?}
    C -->|yes| X2[400 Period locked]
    C -->|no| D[Update times + AdjustedBy]
```

---

## 6. Payroll summary

```mermaid
flowchart TD
    R[GET /payroll/summary] --> P{Pay period exists?}
    P -->|no| C[Create Open period]
    P -->|yes| L{Status Locked + lines?}
    L -->|yes| S[Return PayrollLine snapshot]
    L -->|no| A[Aggregate attendance minutes × HourlyRate]
    A --> OUT[Summary JSON]
    S --> OUT
```

Export: `POST /payroll/summary/export` → CSV (Admin, max 500 rows).

---

## 7. Schedule suggestions (heuristic)

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant H as HeuristicScheduleSuggestionService

    M->>API: POST /schedules/{id}/suggest
    API->>H: GenerateAsync read-only
    alt history < 3 assignments
        H-->>API: empty + insufficient_history
    else
        H-->>API: ranked suggestions
    end
    API-->>M: 200 no DB change

    M->>API: POST /schedules/{id}/apply-suggestions
    API->>API: Validate all rows
    API->>API: Transaction insert all
    API-->>M: 201 assignments
```

### Schedule insight assistant (Bedrock advisory)

```mermaid
sequenceDiagram
    participant M as Manager
    participant API as Schedule API
    participant I as ScheduleInsightService
    participant B as AWS Bedrock

    M->>API: POST /schedules/{id}/suggest
    API->>I: GenerateContextAsync after successful suggestions
    I->>I: Serialize rules, preferences, assignments, suggestions, summaries
    API-->>M: 200 suggestions; context is refreshed in DB

    M->>API: POST /schedules/{id}/insights/chat
    API->>I: ChatAsync(question)
    I->>B: Converse with context snapshot
    B-->>I: Advisory explanation
    API-->>M: 200 answer
```

Bedrock is not part of schedule generation or apply. If Bedrock is unavailable, `suggest` and `apply-suggestions` still work; only the chat endpoint fails independently.

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
    S->>S: Member check
    S->>DB: INSERT message
    S->>H: ReceiveMessage group
    API-->>U: 201

    Note over U,H: WS: connect /ws/chat?access_token=…\nJoinChannel(channelId)
```

---

## 9. Agent decision tree (where to implement)

| Change type                    | Layer                                                 |
| ------------------------------ | ----------------------------------------------------- |
| New business rule / validation | `Wokki.Application` service                           |
| New HTTP route                 | `Wokki.Api/Apis/{Feature}/*Endpoints.cs`              |
| New persistence query          | `Wokki.Domain` repo interface + `Infrastructure` impl |
| New user-visible message       | `AppMessages` + service return                        |
| New enum state                 | `Wokki.Domain.Enums` + service transitions            |

Never add EF or business rules in `Wokki.Api` handlers.
