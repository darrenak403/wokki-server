# Wokki Server — Product & codebase context

Use this file for **business meaning** and **where code lives**. Locked rules: `docs/business-rules.md` (`BR-xxx`). Do not contradict `docs/process-flows.md`.

---

## Product (one paragraph)

**Wokki Shift Ops MVP** is a multi-organization workforce backend: weekly scheduling, shift swaps, attendance, payroll prep, internal chat, and **deterministic** schedule suggestions. Web/mobile-web clients call `/api/v1`; business APIs are scoped by JWT `organization_id`. Managers publish official schedules; employee **preferences** are advisory and separate from `ShiftAssignment`.

---

## Roles & API access

| Role                 | Typical caller | Scheduling                                | Self (`/self/*`)      | Payroll export |
| -------------------- | -------------- | ----------------------------------------- | --------------------- | -------------- |
| **PlatformOperator** | Wokki admin    | No org scheduling                         | No                    | No             |
| **Admin**            | IT/HR          | Full + users                              | If has Employee       | Yes (CSV)      |
| **Manager**          | Ops lead       | Draft/Publish, assignments, swap audit | If has Employee       | View           |
| **User**             | Staff          | No manager schedule APIs                  | Schedule, swap, clock | No             |

`GET /api/v1/auth/me` = login account. `GET /api/v1/self/*` = workforce profile (requires `Employee` row).

Org package gate: register creates `Organization` + Org Admin but package is NotActivated. `PlatformOperator` uses platform APIs to activate/disable/renew with admin-chosen `durationDays` (no FE default of 30 days). Org users blocked with `ORG_PACKAGE_NOT_ACTIVATED` or `ORG_PACKAGE_EXPIRED` until activated/renewed.

Platform control center invariants: subscription changes must write immutable `OrganizationSubscriptionLedgerEntry` + `AuditLog` in the same transaction; ledger writes are mandatory. Support Console phase 1 is read-only for tenant business data: no impersonation, no tenant row contents, no schedule/payroll/attendance/chat edits. Active org analytics are based only on tracked `PlatformActivityEvent` types: login, schedule publish/suggest/apply, attendance clock-in/out, or chat message. `/api/v1/platform/health` is PlatformOperator-only; public `/health` remains anonymous and unchanged.

---

## Core domain flows

### Branch workspace

Admin may manage every branch in the JWT organization; Manager scope is only `LocationManager` assignments. FE workspace/sidebar actions are selected-branch scoped (`/{orgId}/{locationId}/{role}/...`). Department transfer is branch-local: target department `LocationId` must match the employee's Active `LocationMembership`; use location transfer before cross-branch placement.

Org staff creation is a single workflow: `POST /api/v1/employees` creates both the login `User` and linked `Employee` profile. Same-org legacy Users without Employee are linked through this workflow. Do not create standalone org staff with `/api/v1/users`; without an Employee profile the account has no department, branch membership, schedule, attendance, or chat context.

### Schedule lifecycle

```text
Create schedule (Draft, Monday weekStart)
  → employees submit preferences (advisory; /self/schedule-preferences/*)
  → Admin views preference-board → suggest/assign (Draft)
  → Publish → Published (employees see via /self/schedule; swap marketplace locked)
  → Unpublish → Draft (swap marketplace reopens for that week)
```

- One schedule per `(departmentId, weekStartDate)`.
- Overlap and duplicate assignment guards in `ScheduleService`.

### Shift swap marketplace (Draft only)

```text
While schedule is Draft:
  User posts Cover or CrossSwap on /api/v1/swap-posts
  → peers accept FCFS (first valid accept wins)
  → assignments update immediately on Draft schedule
  → email author + accepter on success (best-effort)
On Publish: Pending posts → Hidden; marketplace locked
Admin/Manager: GET /swap-posts/audit only (no create/accept)
```

Rules: `BR-030`–`BR-037`. Services: `SwapPostService`, `SwapPostPolicyValidator`. Legacy `SwapRequest` table read-only; old API removed.

### Attendance & payroll

- Clock-in: published assignment today, no open record.
- Payroll summary: `WorkedMinutes` × `HourlyRate`; locked period uses `PayrollLine` snapshot.

### Auto-scheduling & Bedrock

1. **Org policy** `OrganizationSchedulingPolicy` (`org-scheduling-policy.v1`) — catalog `GET /scheduling/rule-catalog`; org Admin configures `GET/PUT /org/scheduling-policy` (Manager read-only). **4 enforced rules** (SMB F&B: preferences, min staff/shift, role match) + up to 20 advisory customs. Coverage/rest/caps in `SchedulingSolverDefaults`. Map via `OrganizationSchedulingSolverPolicy`.
2. **Department** `DepartmentSchedulingConfig` — overrides.
3. `POST .../suggest` — **CP-SAT only** (`CpSatScheduleSuggestionService`; `useAi` ignored). Inputs via `ScheduleSuggestionContextLoader` (org policy, employees, shifts, **Submitted** preferences, existing assignments, availabilities, history). Returns suggestions only; auto-refreshes insight context snapshot (`BR-070`, `BR-086`). Existing assignment slots stay locked on re-suggest.
4. `POST .../apply-suggestions` — Draft only; all rows validated then one transaction (`BR-075`). Apply is keyed by exact `(shiftDefinitionId, employeeId, date)`: same shift/date may keep multiple employees when policy allows. Re-suggest unlocks only employees whose own preferences changed or whose assignments conflict with Unavailable; `ClearOrphanAssignments` removes omitted tuples only for affected employees.
5. **Bedrock** — `ScheduleInsightService` chat on context snapshot; **never** mutates assignments (`BR-077`–`BR-079`).
6. **Rebalance hints** — `GET /schedules/{id}` includes `rebalanceHints` when Draft assignments conflict with submitted Unavailable or pending leave (`BR-086`). Admin re-suggests via same **Tạo gợi ý AI** button.
7. **Draft leave** — `/self/leave-requests` + Manager approve removes conflicting assignment + sets Unavailable (`BR-087`); no auto CP-SAT.

Missing setup returns explicit reasons: `no_employees`, `no_shifts`, `missing_preferences` (`BR-072`).

Use **department membership** for guards when employee spans multiple departments (`BR-074`).

**Key services:** `ScheduleService`, `SchedulePreferenceService`, `HeuristicScheduleSuggestionService`, `ScheduleInsightService`, `DepartmentSchedulingConfigService`, `LocationService` (policy).

---

## Solution map

| Layer          | Project                | Do / Don't                                                   |
| -------------- | ---------------------- | ------------------------------------------------------------ |
| API            | `Wokki.Api`            | Map HTTP, validate, `ToHttpResult()` — **no** business logic |
| Application    | `Wokki.Application`    | Services, DTOs, validators — **no** `DbContext`              |
| Domain         | `Wokki.Domain`         | Entities, `IUnitOfWork`, `RoleConstants`                     |
| Infrastructure | `Wokki.Infrastructure` | EF, JWT                                                      |
| Common         | `Wokki.Common`         | `ApiResponse<T>`, `AppMessages`                              |

### Endpoints → services (quick lookup)

| Endpoints               | Service(s)                                                                                                                       |
| ----------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `AuthEndpoints`         | `AuthService`                                                                                                                    |
| `UserEndpoints`         | `UserService`                                                                                                                    |
| `EmployeeEndpoints`     | `EmployeeService`                                                                                                                |
| `LocationEndpoints`     | `LocationService` (+ policy in schedule/location DTOs)                                                                           |
| `DepartmentEndpoints`   | `DepartmentService`                                                                                                              |
| `ShiftEndpoints`        | `ShiftDefinitionService`                                                                                                         |
| `ScheduleEndpoints`     | `ScheduleService`, `SchedulePreferenceService`, `DepartmentSchedulingConfigService`, `ScheduleInsightService`, suggestion engine |
| `EmployeeSelfEndpoints` | schedule/swap-posts/attendance self                                                                                    |
| `SwapPostEndpoints`     | `SwapPostService`                                                                                                      |
| `AttendanceEndpoints`   | `AttendanceService`                                                                                                              |
| `PayrollEndpoints`      | `PayrollService`                                                                                                                 |
| `ChannelEndpoints`      | `ChannelService`                                                                                                                 |
| `BedrockEndpoints`      | `BedrockService`                                                                                                                 |
| `PlatformEndpoints`     | `StatsService`, `PlatformAdminService`, `PlatformDiagnosticsService`, `PlatformUsageAnalyticsService`                             |

### Adding a feature (checklist)

1. Entity + repository + `IUnitOfWork` (Domain)
2. EF config + migration (`task migration:add`)
3. `AppMessages` codes
4. DTOs, validator, service interface/impl (Application)
5. `Apis/{Feature}/{Feature}Endpoints.cs` + `MapEndpoints()` registration
6. Update `docs/api-catalog.md` + `BR-xxx` if new rules

---

## Messages & errors

Business codes live in `Wokki.Common` → `AppMessages`. FE maps `message.code` (e.g. `SCHEDULE_NOT_DRAFT`, `SWAP_CUTOFF`). Prefer reusing existing codes.

---

## Related repos & docs

| Path                                  | Content                         |
| ------------------------------------- | ------------------------------- |
| `../wokki-client`                     | Next.js UI                      |
| `docs/vi/fe-integration-guide.md`     | FE waves, auth, SignalR         |
| `docs/fe/`                            | Per-wave handoff (when present) |
| `plans/shift-ops-mvp/`                | Implementation plan             |
| `plans/fe-handoff-flow-verification/` | Smoke scripts                   |

---

## Seed & local URLs

- API: `http://localhost:8386` · Scalar: `/scalar`
- Platform seed: `admin@gmail.com` / `12345@Abc` (`PlatformOperator`). Org Manager/User accounts are created by an Org Admin after the org package is active.

---

## Claude quick commands

| Command       | Purpose                                  |
| ------------- | ---------------------------------------- |
| `/ck:wokki`   | Load BRD + `BR-xxx` + file map for topic |
| `/ck:cook`    | Implement from plan                      |
| Skill `wokki` | `.claude/skills/wokki/SKILL.md`          |

**Auto-loaded:** `CLAUDE.md` + `wokki-bootstrap.md` on session start (hook).
