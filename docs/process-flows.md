# Process Flows

Visual reference for agents. Rule IDs refer to [business-rules.md](./business-rules.md).

---

## 1. Deployment & tenancy

```mermaid
flowchart LR
    Enterprise[Enterprise customer] --> Instance[Dedicated Wokki instance]
    Instance --> DB[(PostgreSQL)]
    Instance --> API[Wokki.Api]
```

One company per environment. No shared multi-tenant database in MVP.

---

## 2. Schedule lifecycle (MVP)

```mermaid
stateDiagram-v2
    [*] --> Draft: Create / Copy week
    Draft --> Published: Publish
    Published --> Draft: Unpublish
    note right of Published: Employees see /me/schedule\nSwaps allowed
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
    B -->|yes| C{Employee in dept?}
    C -->|no| X2[400 Wrong dept]
    C -->|yes| D{Shift in scope?}
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

| Change type | Layer |
|-------------|--------|
| New business rule / validation | `Wokki.Application` service |
| New HTTP route | `Wokki.Api/Apis/{Feature}/*Endpoints.cs` |
| New persistence query | `Wokki.Domain` repo interface + `Infrastructure` impl |
| New user-visible message | `AppMessages` + service return |
| New enum state | `Wokki.Domain.Enums` + service transitions |

Never add EF or business rules in `Wokki.Api` handlers.
