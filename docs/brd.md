# Business Requirements Document (BRD)

## Wokki Shift Ops MVP — Backend

|                  |                                                    |
| ---------------- | -------------------------------------------------- |
| **Document**     | BRD v1.0                                           |
| **Product**      | Wokki Shift Ops                                    |
| **Component**    | `wokki-server` (REST + SignalR API)                |
| **Status**       | Approved for implementation (Phases 1–5 delivered) |
| **Last updated** | 2026-05-29                                         |

---

## 1. Executive summary

Wokki Shift Ops is a multi-organization workforce operations platform. Each customer organization owns its own branches, departments, users, schedules, attendance, payroll prep, and chat data inside the shared API/database tenant boundary.

The backend exposes versioned APIs (`/api/v1`) consumed by web and mobile-web clients. Business logic lives in the Application layer; this document defines **what** the system must do, **who** uses it, and **which rules** are non-negotiable for agents and engineers.

---

## 2. Business context

### 2.1 Problem statement

Operations teams spend excessive time on manual schedules, paper timesheets, and informal shift trades. Payroll preparation lacks a single source of truth tied to published shifts and actual clock data.

### 2.2 Business objectives

| ID     | Objective                                                                        | Success indicator                                                                                         |
| ------ | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| OBJ-01 | Reduce time to build and publish weekly schedules                                | Managers publish in-app; copy-week supported                                                              |
| OBJ-02 | Formalize shift swaps with audit trail                                           | Swap state machine + notifications                                                                        |
| OBJ-03 | Eliminate manual timesheets for hourly staff                                     | Clock-in/out tied to assignments                                                                          |
| OBJ-04 | Accelerate payroll prep                                                          | Department summary + CSV export                                                                           |
| OBJ-05 | Reduce reliance on external chat for shift coordination                          | In-app channels + real-time messages                                                                      |
| OBJ-06 | Assist managers with assignment suggestions and explain weekly schedule outcomes | Deterministic suggest/apply on Draft schedules; optional Bedrock insight chat reads schedule context only |

### 2.3 Deployment model

- **Organization is the tenant root**: business data is scoped by JWT `organization_id`.
- New customers may self-register an org, but new orgs start without an activated package.
- Wokki admin (`PlatformOperator`) manages platform-level user/org visibility and activates, disables, or renews org packages.

---

## 3. Stakeholders

| Stakeholder                 | Role                              | Interest                                                                           | Primary capabilities                                                            |
| --------------------------- | --------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| **System Admin**            | IT / HR systems                   | Users, org data, payroll export, compliance                                        | All branches, admin APIs, user management, payroll CSV                          |
| **Wokki Platform Operator** | Wokki admin                       | Platform oversight, org package activation/renewal                                 | `/platform`, platform stats, system user/org lists, org package toggle          |
| **Operations Manager**      | Branch/department lead            | Build/publish schedules, approve swaps, review attendance inside assigned branches | Assigned branch workspace, schedules, assignments, swaps override, payroll view |
| **Employee (User)**         | Frontline staff                   | See own shifts, swap shifts, clock in/out                                          | `/self/*`, swap peer actions, attendance clock                                  |
| **Payroll clerk**           | Finance (may share Admin account) | Period totals, export                                                              | Payroll summary + export                                                        |
| **Product owner**           | Business sponsor                  | MVP scope, phased delivery                                                         | `plans/shift-ops-mvp/`                                                          |
| **Engineering / AI agents** | Build & maintain API              | Correct rules, stable contracts                                                    | This BRD + `docs/business-rules.md`                                             |

---

## 4. Scope

### 4.1 In scope (MVP backend)

| Phase | Capability                                                                                          |
| ----- | --------------------------------------------------------------------------------------------------- |
| 1     | Manager role, employees, locations, departments                                                     |
| 2     | Shift definitions, weekly schedules, publish/unpublish, copy week, `/self/schedule`                 |
| 3     | Swap requests, peer accept/decline, manager override, notifications                                 |
| 4     | Clock-in/out, attendance list/adjust, payroll summary & CSV export                                  |
| 5     | Internal chat (REST + SignalR), schedule suggest/apply, optional Bedrock schedule insight assistant |

### 4.2 Out of scope (MVP)

| Item                                  | Rationale                                                                        |
| ------------------------------------- | -------------------------------------------------------------------------------- |
| Native iOS/Android apps               | Clients use API; UI out of repo                                                  |
| Dynamic self-service billing/checkout | Package activation is controlled by Wokki admin in platform APIs                 |
| Dynamic RBAC / custom roles           | Fixed Admin / Manager / User                                                     |
| External LLM for scheduling           | Bedrock is advisory insight/chat only; it must not generate or apply assignments |
| SignalR Redis backplane               | Single-instance MVP; document scale limit                                        |
| Public pay-period lock API            | Lock via data/status; export/snapshot pattern                                    |
| Schedule `Locked` status in workflows | Enum present; publish flow uses Draft/Published only                             |
| Full audit log UI                     | `AuditLog` entity exists; not all actions wired                                  |

### 4.3 Assumptions

- Clients obtain JWT via `/api/v1/auth/login`.
- Each `User` performing shift/attendance/chat actions has a linked `Employee`.
- Admin manages all branches. Manager access is scoped to `LocationManager` assignments; an employee must have Active `LocationMembership` before protected workspace access.
- Location `TimeZone` is valid IANA id for swap cutoff.
- PostgreSQL is the system of record.
- SMTP credentials are provided via configuration/secrets when email is required.

### 4.4 Constraints

- Clean Architecture: no business logic in `Wokki.Api`.
- All service results use `ApiResponse<T>`.
- API modules under `Apis/{Feature}/` registered in `PipelineExtensions.MapEndpoints()`.

---

## 5. Functional requirements

Requirements use **`FR-xxx`**. Priority: **P1** = MVP must-have.

### 5.1 Identity & organization (Phase 1)

| ID     | Priority | Requirement                                                                                                                                                                                                      |
| ------ | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-101 | P1       | System shall support roles Admin, Manager, User.                                                                                                                                                                 |
| FR-102 | P1       | Admin shall CRUD users and employees (employee creates linked user).                                                                                                                                             |
| FR-103 | P1       | Admin shall manage every location and department; Manager shall manage only locations assigned through `LocationManager`.                                                                                        |
| FR-104 | P1       | Employee shall store position, hourly rate, department, termination date.                                                                                                                                        |
| FR-105 | P1       | Org Admin creates employees with a `DepartmentId`; the system auto-provisions Active `LocationMembership` at that department's location. Employees log in directly — no self-serve join request or `/join` gate. |
| FR-106 | P1       | Anonymous register shall create an Organization and Org Admin, but the org is not usable until Wokki admin activates its package.                                                                                |
| FR-107 | P1       | PlatformOperator shall list platform users/orgs and enable, disable, or renew an org package for a Wokki-admin-chosen number of days (`durationDays` on platform console).                                       |

### 5.2 Scheduling (Phase 2)

| ID     | Priority | Requirement                                                                                                                                 |
| ------ | -------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-201 | P1       | Manager shall create weekly schedule per department (Monday week start).                                                                    |
| FR-202 | P1       | Manager shall assign only employees with Active membership in the schedule location and membership in the schedule department (Draft only). |
| FR-203 | P1       | Manager shall publish/unpublish schedules.                                                                                                  |
| FR-204 | P1       | Manager shall copy a published/draft week to a new Draft week.                                                                              |
| FR-205 | P1       | Employee shall view own upcoming assignments via `/self/schedule`.                                                                          |
| FR-206 | P1       | System shall prevent overlapping assignments same employee/day.                                                                             |

### 5.3 Swap workflow (Phase 3)

| ID     | Priority | Requirement                                                             |
| ------ | -------- | ----------------------------------------------------------------------- |
| FR-301 | P1       | Employee shall request swap between two published assignments.          |
| FR-302 | P1       | Target employee shall accept or decline.                                |
| FR-303 | P1       | Accept shall swap assignment ownership atomically and complete request. |
| FR-304 | P1       | Manager shall override approve/reject.                                  |
| FR-305 | P1       | System shall enforce swap cutoff by location timezone.                  |
| FR-306 | P1       | System shall send notifications on swap events and schedule publish.    |

### 5.4 Attendance & payroll (Phase 4)

| ID     | Priority | Requirement                                                        |
| ------ | -------- | ------------------------------------------------------------------ |
| FR-401 | P1       | Employee shall clock in only with today's published assignment.    |
| FR-402 | P1       | Employee shall clock out open record; system computes minutes.     |
| FR-403 | P1       | Manager shall list and manually adjust attendance with audit note. |
| FR-404 | P1       | Adjustment shall fail when pay period for that date is locked.     |
| FR-405 | P1       | Manager shall view payroll summary by department and period.       |
| FR-406 | P1       | Admin shall export payroll summary as CSV (row cap 500).           |

### 5.5 Chat & suggestions (Phase 5)

| ID     | Priority | Requirement                                                                                                                       |
| ------ | -------- | --------------------------------------------------------------------------------------------------------------------------------- |
| FR-501 | P1       | Authenticated users shall list channels they belong to.                                                                           |
| FR-502 | P1       | Manager shall create direct/group channels.                                                                                       |
| FR-503 | P1       | Members shall read (cursor) and send messages; soft-delete with placeholder.                                                      |
| FR-504 | P1       | Real-time delivery via SignalR hub `/ws/chat` with JWT.                                                                           |
| FR-505 | P1       | Manager shall request heuristic suggestions without DB change.                                                                    |
| FR-506 | P1       | Manager shall apply suggestions to Draft schedule in one transaction.                                                             |
| FR-507 | P1       | Manager/Admin shall generate a weekly schedule insight context snapshot without calling Bedrock or changing assignments.          |
| FR-508 | P1       | Manager/Admin shall ask Bedrock questions about a generated schedule context; answers are advisory and must not mutate schedules. |

---

## 6. Business rules

All locked rules are maintained in **[business-rules.md](./business-rules.md)** with IDs `BR-xxx`. Implementation must map each rule to service-layer guards (not only documentation).

Summary by domain:

1. **Access** — role matrix; User isolated to self-service paths; org package gate blocks inactive/expired org usage.
2. **Scheduling** — Draft edits; Published visibility; overlap/duplicate prevention.
3. **Swaps** — state machine, cutoff, atomic apply on accept.
4. **Attendance** — single open record; adjust audit; pay-period lock guard.
5. **Payroll** — minutes × rate; snapshot on locked period when lines exist.
6. **Chat** — membership; sanitization; soft-delete policy.
7. **Suggestions** — read-only suggest; transactional apply; history threshold.

---

## 7. Non-functional requirements

| ID     | Category        | Requirement                                                                         |
| ------ | --------------- | ----------------------------------------------------------------------------------- |
| NFR-01 | Security        | JWT for REST and SignalR; scrub `access_token` from logs                            |
| NFR-02 | Security        | Chat HTML stripped; message length cap                                              |
| NFR-03 | Reliability     | Notifications must not roll back core transactions                                  |
| NFR-04 | Performance     | Rate limiting on APIs; higher limit for clock endpoints                             |
| NFR-05 | Scalability     | SignalR single-node MVP (no backplane)                                              |
| NFR-06 | Maintainability | Clean Architecture layer boundaries                                                 |
| NFR-07 | Observability   | Serilog structured logging with correlation id                                      |
| NFR-08 | API             | OpenAPI/Scalar in dev; consistent `ApiResponse` envelope                            |
| NFR-09 | Data            | EF Core migrations; PostgreSQL                                                      |
| NFR-10 | i18n            | Message codes in English (`AppMessages`); descriptions may be Vietnamese in OpenAPI |

---

## 8. User journeys (high level)

### 8.1 Manager — publish week

1. Create Draft schedule for department + Monday date.
2. Add assignments (or suggest → apply).
3. Publish → employees notified → visible on `/self/schedule`.

### 8.2 Employee — swap shift

1. View `/self/schedule`.
2. Create swap targeting colleague's assignment.
3. Colleague accepts → assignments exchanged automatically.

### 8.3 Employee — workday

1. Clock in (assignment today).
2. Clock out at end of shift.
3. Optional: read team chat channel.

### 8.4 Admin — payroll

1. Run payroll summary for department + date range.
2. Export CSV for finance system.

### 8.5 Wokki admin — activate org package

1. Log in as `PlatformOperator`.
2. Review platform users/orgs.
3. Enable or renew an org package with a Wokki-admin-chosen `durationDays` (platform console; no fixed default in FE).

Detailed diagrams: **[process-flows.md](./process-flows.md)**.

---

## 9. System interfaces

| Interface | Detail                                               |
| --------- | ---------------------------------------------------- |
| REST      | `/api/v1/*` — see [api-catalog.md](./api-catalog.md) |
| WebSocket | SignalR `/ws/chat`                                   |
| Email     | SMTP optional (`Smtp` config)                        |
| Database  | PostgreSQL via EF Core                               |

---

## 10. Data concepts (logical model)

| Entity                          | Purpose                             |
| ------------------------------- | ----------------------------------- |
| User, Employee                  | Authentication + workforce identity |
| Location, Department            | Org structure                       |
| ShiftDefinition                 | Shift template                      |
| Schedule, ShiftAssignment       | Weekly plan                         |
| SwapRequest                     | Swap workflow                       |
| AttendanceRecord                | Time tracking                       |
| PayPeriod, PayrollLine          | Payroll                             |
| Channel, ChannelMember, Message | Chat                                |

Glossary: **[glossary.md](./glossary.md)**.

---

## 11. Traceability

| BRD section | Implementation plan                          |
| ----------- | -------------------------------------------- |
| Phase 1     | `plans/shift-ops-mvp/phase-01-foundation.md` |
| Phase 2     | `phase-02-scheduling.md`                     |
| Phase 3     | `phase-03-swap-workflow.md`                  |
| Phase 4     | `phase-04-attendance-payroll.md`             |
| Phase 5     | `phase-05-chat-ai.md`                        |

| BRD section      | Code pointers                               |
| ---------------- | ------------------------------------------- |
| Services         | `src/Wokki.Application/Services/{Feature}/` |
| Rules / messages | `docs/business-rules.md`, `AppMessages.cs`  |
| HTTP             | `src/Wokki.Api/Apis/`                       |
| Enums            | `src/Wokki.Domain/Enums/`                   |

---

## 12. Open points & future phases

| Topic                      | MVP decision                                   | Future                                          |
| -------------------------- | ---------------------------------------------- | ----------------------------------------------- |
| Schedule `Locked`          | Enum only                                      | Lock after payroll sign-off                     |
| Pay period lock API        | Status in DB                                   | Explicit Admin lock endpoint                    |
| Audit log coverage         | Partial entity                                 | Wire all BR-sensitive actions                   |
| Schedule insights          | Bedrock optional chat over JSON context        | RAG/file storage, richer diagnostics, analytics |
| Package billing automation | Wokki admin manually activates/renews packages | Payment integration and automated renewals      |
| SignalR scale              | Single server                                  | Redis backplane / Azure SignalR                 |

---

## 13. Approval & change control

Changes to **locked business rules** (`BR-xxx`) require explicit product approval and synchronized updates to:

1. `docs/business-rules.md`
2. `docs/brd.md` (this file)
3. `docs/process-flows.md` if flows change
4. Application services and `AppMessages`

Agents: when implementing a feature, cite the `FR-` / `BR-` IDs you satisfy in PR descriptions or commit bodies.
