# Business Rules (Locked)

Rules are numbered **`BR-xxx`**. Services and APIs must enforce them. When a rule conflicts with a convenience shortcut, **the rule wins**.

Cross-reference: [process-flows.md](./process-flows.md), [api-catalog.md](./api-catalog.md).

---

## Identity & access

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-001 | Roles are fixed: `Admin`, `Manager`, `User` only. No dynamic permission matrix in MVP. | JWT claims + endpoint `RequireRole` |
| BR-002 | `User` may not access manager schedule APIs (`/api/v1/schedules/*` except via published data indirectly). Own schedule only via `/api/v1/me/schedule`. | Route authorization |
| BR-003 | `Admin` may manage users, payroll export, and soft-delete any chat message. | `ChannelService`, `PayrollEndpoints` |
| BR-004 | `Manager` may manage schedules, assignments, swap overrides, attendance adjust, and create chat channels. | Route authorization |
| BR-005 | Every employee-facing action requires an `Employee` row linked to the authenticated `User`. | Services return `*_NO_EMPLOYEE` / 404 |

---

## Organization & master data

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-010 | A `Department` belongs to exactly one `Location`. | EF FK |
| BR-011 | `Employee.DepartmentId` must match the department of schedules they are assigned to. | `TryPrepareAssignmentAsync` |
| BR-012 | Terminated employees (`TerminatedAt` set) cannot receive new assignments or be swap targets. | `EmployeeService`, assignment validators |
| BR-013 | `ShiftDefinition` must match schedule scope: same `LocationId`; if `DepartmentId` set, must equal schedule department. | `TryPrepareAssignmentAsync` |

---

## Scheduling

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-020 | `WeekStartDate` must be a **Monday** (`ScheduleRules.IsMonday`). | Create/update schedule |
| BR-021 | At most one schedule per `(DepartmentId, WeekStartDate)`. | Unique constraint + service guard |
| BR-022 | Schedule lifecycle in MVP: **`Draft` → `Published`** (unpublish reverts to `Draft`). Enum `Locked` exists but is **not** used by publish APIs yet. | `ScheduleService` |
| BR-023 | Assignments may be created/updated/deleted only when schedule is **`Draft`**. | `CreateAssignmentAsync`, delete assignment |
| BR-024 | One employee cannot have **overlapping** shift times on the same date within one schedule. | `HasTimeOverlapAsync` |
| BR-025 | Duplicate tuple `(schedule, shiftDefinition, employee, date)` is rejected. | `ExistsAsync` |
| BR-026 | On publish, assigned employees receive notification `schedule.published` (non-blocking). | `ScheduleService.PublishAsync` |
| BR-027 | `GET /api/v1/me/schedule` returns only the caller's assignments for the next **28 days** on **published** schedules. | `GetMyScheduleAsync` |

---

## Shift swap

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-030 | Swaps allowed only on assignments belonging to **`Published`** schedules. | `SwapRequestService.CreateAsync` |
| BR-031 | Requester must own the offered assignment. | `NotOwner` |
| BR-032 | Cannot swap with self. | `SameEmployee` |
| BR-033 | At most one open (`Pending`) swap per requester assignment. | `HasOpenSwapForAssignmentAsync` |
| BR-034 | **Cutoff** (location timezone): for next-week shifts, create before Friday end; accept/decline before Monday 00:00. | `SwapCutoffRules` |
| BR-035 | Valid transitions enforced; invalid → **409**. | Status guards per action |
| BR-036 | On peer **accept**: `Pending` → `PeerAccepted` → atomic assignment swap → `ManagerApproved` in **one transaction**. | `AcceptAsync` |
| BR-037 | Notifications (`swap.*`, `schedule.published`) must **not** roll back the core transaction if delivery fails. | try/catch around `INotificationService` |

### Swap status transitions (allowed)

| From | Action | To |
|------|--------|-----|
| `Pending` | Target accept | `ManagerApproved` (via peer accept + auto-apply) |
| `Pending` | Target decline | `PeerDeclined` |
| `Pending` | Requester cancel | `Cancelled` |
| `Pending` | Manager override approve | `ManagerApproved` |
| `Pending` | Manager override reject | `ManagerRejected` |

---

## Attendance

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-040 | Clock-in only if employee has **no** open record (`ClockOut IS NULL`). | Partial unique index + service |
| BR-041 | Clock-in requires at least one **published** assignment for **today**. | `ListByEmployeeInDateRangeAsync` |
| BR-042 | Clock-out requires an open record; sets `WorkedMinutes` (rounded minute). | `AttendanceService` |
| BR-043 | Manual adjust: `Admin`/`Manager` only; **adjustment note required**. | `AdjustAsync` |
| BR-044 | Adjust blocked if a **locked** pay period contains the record's clock-in date. | `IsPayPeriodLockedForRecordAsync` |
| BR-045 | Clock endpoints use higher rate limit policy (`Clock`, 300/min). | `AttendanceEndpoints` |

---

## Payroll

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-050 | Summary aggregates `WorkedMinutes` from attendance (with assignment) in pay period date range × current `HourlyRate`. | `PayrollService.BuildSummaryCoreAsync` |
| BR-051 | Pay period auto-created on first summary request if no overlap. | `PayPeriodRepository` |
| BR-052 | When period is **`Locked`** and snapshot lines exist, summary reads **`PayrollLine`** not live attendance. | `PayrollService` |
| BR-053 | `HourlyRate` on `PayrollLine` is a **snapshot** at lock time (future lock workflow). | Domain model |
| BR-054 | CSV export max **500** rows; Admin only. | `ExportCsvAsync` |
| BR-055 | Only attendance with `AssignmentId` and `ClockOut` counts toward payroll minutes. | `SumWorkedMinutesByEmployeeAsync` |

---

## Chat

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-060 | Only **channel members** may list/send messages; non-member → **403**. | `ChannelService` |
| BR-061 | `Admin` may read any channel history without membership. | `ResolveMemberAccessAsync` |
| BR-062 | Direct channel: exactly **two** members; reuse existing DM if present. | `CreateAsync` + `FindDirectChannelAsync` |
| BR-063 | Group channel requires non-empty **name**. | `CreateAsync` |
| BR-064 | Message body: plain text, max **4000** chars; HTML tags stripped. | `SanitizeBody` |
| BR-065 | Soft-delete sets `DeletedAt` and body placeholder `[Message deleted]` (thread preserved). | `DeleteMessageAsync` |
| BR-066 | Persist message first; SignalR `ReceiveMessage` is best-effort (no rollback on push failure). | `SendMessageAsync` |
| BR-067 | WebSocket `/ws/chat` requires JWT (`access_token` query); logs must **redact** token. | JwtBearer + Serilog enricher |

---

## Schedule suggestions (AI/heuristic)

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-070 | `POST .../suggest` is **read-only** (no DB writes). | `HeuristicScheduleSuggestionService` |
| BR-071 | Suggest/apply only on **`Draft`** schedules. | Services |
| BR-072 | Requires ≥ **3** published historical assignments in prior **4 weeks**; else `reason: insufficient_history`. | Heuristic threshold |
| BR-073 | Suggestions must not double-book (in-memory overlap check for the target week). | `HasOverlapInPlan` |
| BR-074 | Employee `Position` must match shift `RequiredRole` to be suggested. | `RoleMatches` |
| BR-075 | `apply-suggestions` validates **all** rows then commits in **one transaction** (all or none). | `ApplySuggestionsAsync` |

---

## Notifications & cross-cutting

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-080 | SMTP email when `Smtp:Host` + `Smtp:From` configured; else no-op logger. | Infrastructure DI |
| BR-081 | API responses use envelope `ApiResponse<T>` with `AppMessage` codes. | Application-wide |
| BR-082 | Business logic only in **Application** services; API is HTTP mapping only. | Architecture guardrail |
