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
| **Manager**          | Ops lead       | Draft/Publish, assignments, swap override | If has Employee       | View           |
| **User**             | Staff          | No manager schedule APIs                  | Schedule, swap, clock | No             |

`GET /api/v1/auth/me` = login account. `GET /api/v1/self/*` = workforce profile (requires `Employee` row).

Org package gate: register creates `Organization` + Org Admin but package is NotActivated. `PlatformOperator` uses platform APIs to activate/disable/renew with admin-chosen `durationDays` (no FE default of 30 days). Org users blocked with `ORG_PACKAGE_NOT_ACTIVATED` or `ORG_PACKAGE_EXPIRED` until activated/renewed.

---

## Core domain flows

### Branch workspace

Admin may manage every branch in the JWT organization; Manager scope is only `LocationManager` assignments. FE workspace/sidebar actions are selected-branch scoped (`/{orgId}/{locationId}/{role}/...`). Department transfer is branch-local: target department `LocationId` must match the employee's Active `LocationMembership`; use location transfer before cross-branch placement.

### Schedule lifecycle

```text
Create schedule (Draft, Monday weekStart)
  → add/edit/delete assignments (Draft only)
  → optional: copy preferences, suggest, apply-suggestions (Draft)
  → Publish → Published (employees see via /self/schedule, swaps allowed)
  → Unpublish → Draft
```

- One schedule per `(departmentId, weekStartDate)`.
- Overlap and duplicate assignment guards in `ScheduleService`.

### Swap (published assignments only)

```text
User creates swap (Pending)
  → peer Accept → assignments swapped in one transaction → ManagerApproved
  → peer Decline → PeerDeclined
  → requester Cancel → Cancelled
  → Manager override approve/reject
```

Cutoff: `SwapCutoffRules` (location timezone). Notifications must not roll back core transaction.

### Attendance & payroll

- Clock-in: published assignment today, no open record.
- Payroll summary: `WorkedMinutes` × `HourlyRate`; locked period uses `PayrollLine` snapshot.

### Auto-scheduling & Bedrock

1. **Branch policy** `LocationSchedulingPolicy` (`location-scheduling-policy.v5`) — minimal solver rules + optional custom (not read by solver yet). Map via `LocationSchedulingSolverPolicy`; caps/weights in `SchedulingSolverDefaults`. Hierarchy: location → department → employee (no job positions).
2. **Department** `DepartmentSchedulingConfig` — overrides.
3. `POST .../suggest` — heuristic only, no DB write; may refresh insight context (`BR-070`).
4. `POST .../apply-suggestions` — Draft only; all rows validated then one transaction (`BR-075`).
5. **Bedrock** — `ScheduleInsightService` + context snapshot + chat; **never** mutates assignments (`BR-077`–`BR-079`).

Missing setup returns explicit reasons: `missing_location_rules`, `no_employees`, `no_shifts`, `missing_preferences` (`BR-072`).

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
| `EmployeeSelfEndpoints` | schedule/swap/attendance read models for self                                                                                    |
| `SwapRequestEndpoints`  | `SwapRequestService`                                                                                                             |
| `AttendanceEndpoints`   | `AttendanceService`                                                                                                              |
| `PayrollEndpoints`      | `PayrollService`                                                                                                                 |
| `ChannelEndpoints`      | `ChannelService`                                                                                                                 |
| `BedrockEndpoints`      | `BedrockService`                                                                                                                 |
| `PlatformEndpoints`     | `StatsService`, `PlatformAdminService`                                                                                           |

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
