# AGENTS.md — Wokki Server

**Claude Code:** [CLAUDE.md](./CLAUDE.md) · [.claude/README.md](./.claude/README.md) · `/ck:wokki` · **Map:** [.claude/contexts/wokki.md](./.claude/contexts/wokki.md)

Backend: **.NET 10**, Clean Architecture, Minimal API, EF Core + PostgreSQL, Scalar docs.

## Before you code

1. Read [docs/README.md](docs/README.md) — documentation index (EN); Vietnamese: [docs/vi/README.md](docs/vi/README.md)
2. Read [docs/brd.md](docs/brd.md) and [docs/business-rules.md](docs/business-rules.md) — business intent and locked rules (`BR-xxx`); VI: [docs/vi/brd.md](docs/vi/brd.md), [docs/vi/business-rules.md](docs/vi/business-rules.md)
3. Read [docs/process-flows.md](docs/process-flows.md) when changing workflows or state machines
4. Read [docs/architecture.md](docs/architecture.md) and [docs/minimal-api.md](docs/minimal-api.md)
5. Follow [.cursor/rules/wokki-backend.mdc](.cursor/rules/wokki-backend.mdc)

## Hard rules

- **Always update agents/docs for business changes** — any time a task introduces, removes, or changes business behavior, workflow, permissions, status rules, API business meaning, or user-facing business copy, the agent must update the relevant docs, `AGENTS.md`, `CLAUDE.md`, and agent context in the same task. Do this proactively; do not wait for the user to ask. Update locked docs (`docs/brd.md`, `docs/business-rules.md`, `docs/process-flows.md`, API/FE handoff docs if needed) and mirror durable guidance in both backend and frontend `AGENTS.md`/`CLAUDE.md` files when the rule affects both apps.
- **Org package gate is platform-controlled** — `POST /auth/register` creates an org + Org Admin, but the org starts without an activated package. `PlatformOperator` enables/disables/renews via `/api/v1/platform/organizations/{id}/subscription` with admin-chosen `durationDays` (platform UI; no fixed 30-day default in FE). Org users are blocked at login/refresh and authenticated org APIs with `ORG_PACKAGE_NOT_ACTIVATED` or `ORG_PACKAGE_EXPIRED` until Wokki admin activates/renews.
- **Platform control center invariants** — every subscription change must write immutable `OrganizationSubscriptionLedgerEntry` + `AuditLog` in the same transaction; ledger writes are mandatory. Support Console phase 1 is read-only for tenant business data: no impersonation, no tenant row contents, no schedule/payroll/attendance/chat edits; only existing platform subscription actions are allowed writes. Active org analytics come only from tracked events (`auth.login`, `schedule.publish`, `schedule.suggest`, `schedule.apply_suggestions`, `attendance.clock_in`, `attendance.clock_out`, `chat.message`) and must not store payload bodies or roll back core workflows. `/api/v1/platform/health` is PlatformOperator-only; public `/health` stays unchanged.
- **Schedule preferences are advisory** — employee đăng ký ca is separate from official `ShiftAssignment`; Admin/Manager decides final Draft/Published schedule. Users can update preferences only while the schedule is Draft; Published preferences are view-only.
- **Shift swap marketplace is Draft-only** — employees post **Cover/CrossSwap** on `/api/v1/swap-posts` (same branch + department); FCFS accept updates Draft assignments immediately; publish hides pending posts; Admin/Manager audit only (no approval). Draft assignment picker: `GET /api/v1/self/schedule/draft/{weekStartDate}/assignments`. Legacy `swap_requests` table kept read-only; old `/api/v1/swap-requests` removed.
- **Branch workspace access is scoped** — Admin manages every branch in their org. Manager manages only locations assigned through `LocationManager` (set via `locationIds` when creating a Manager, or later in workspace). User sees only branches with Active `LocationMembership` (from department at create). UI workspace/sidebar actions are scoped to the selected branch URL (`/{orgId}/{locationId}/{role}/...`); an org-level workspace is only a redirect/selection fallback. Org Admin **creates staff via `POST /api/v1/employees`**: **User** requires `departmentId` (branch + department, auto `LocationMembership`); **Manager** requires `locationIds` (no department). **Role changes** for existing staff use `POST /api/v1/employees/{id}/role-transition` (not `PATCH /users/{id}/role`). Handoff: [docs/fe/employee-role-transition-handoff.md](docs/fe/employee-role-transition-handoff.md). Same-org legacy Users without Employee are linked there. **Parallel self-onboard:** `POST /auth/register-employee` → org directory → `POST /api/v1/org-join-requests` → Admin approve (`PATCH .../approve` with `departmentId` + `hourlyRate`) — see BR-020, [docs/fe/org-join-request-handoff.md](docs/fe/org-join-request-handoff.md). Branch changes use workspace transfer APIs (`POST /api/v1/workspace/location/transfer`), and department transfer must target the employee's active branch.
- **Auto-scheduling uses org-wide policy** — `OrganizationSchedulingPolicy` (`org-scheduling-policy.v1`) + platform catalog `GET /api/v1/scheduling/rule-catalog`; org Admin configures via `GET/PUT /api/v1/org/scheduling-policy` (Manager read-only). **4 enforced rules** in catalog (preferences, min staff/shift, role match); coverage fill, rest, weekly caps in `SchedulingSolverDefaults`. Map enforced keys with `OrganizationSchedulingSolverPolicy`. Up to 20 advisory custom rules per org (solver ignores). Suggestions applied only via explicit UI action, keyed by exact `(shiftDefinitionId, employeeId, date)` so one employee never overwrites another employee on the same shift/date. On re-suggest after preference changes, unlock only the employees who changed preferences or have Unavailable conflicts; `ClearOrphanAssignments` clears omitted tuples only for affected employees. No per-branch policy.
- **Org chat** — one org-wide channel (`Organization`) + direct messages only; no custom groups. `GET /api/v1/channels/org/members` for DM picker. SignalR `/ws/chat` via `SignalRProvider` + `useChatHub`. Org Admin from `POST /register` gets an auto-linked `Employee` profile (legacy Admins provisioned on first chat/self-profile).
- **Bedrock schedule insight is advisory only** — AWS Bedrock may answer questions from a generated weekly schedule context snapshot and may generate ephemeral per-call scheduling hints (PreferenceWeight, TempMinMax, AvoidPairing) via `/insights/chat?HintMode=true`, but it must not generate, apply, update, or publish `ShiftAssignment`. Hints are passed to `suggest` only, never persisted. `suggest` / `apply-suggestions` must keep working when Bedrock is unavailable.
- **No business logic in `Wokki.Api`** — only map HTTP → application service
- **Services return `ApiResponse<T>`** — use `SuccessResponse`, `FailureResponse`, `SuccessPagedResponse` only
- **New endpoints** → `Apis/{Feature}/{Feature}Endpoints.cs` with `MapXxxApi` / `MapXxxRoutes` / static handlers; register in `PipelineExtensions.MapEndpoints()`
- **Validation in handlers** → `request.ValidateRequest(validator, out var validationResult)` (see `ValidationExtensions`)
- **Paths** → `/api/v1/{resource}` (health stays `/health`)
- **Roles** → `Wokki.Domain.Constants.RoleConstants`
- **Data access** → inject `IUnitOfWork` in services, not `DbContext` in Application
- **Application structure is fixed**:
  - `Dtos/{Feature}`
  - `Services/{Feature}/Interfaces`
  - `Services/{Feature}/Implementations`
  - `Validators/{Feature}`
  - `Mappings/{Feature}`
- **Do not use/recreate `Wokki.Application/Features/*`**
- **Namespace must match folder path** (example: `Wokki.Application.Services.Auth.Interfaces`)
- **Definition of done**: build passes and project still respects the structure above

## Commands (Taskfile — mandatory for agents)

Use **[Task](https://taskfile.dev)** from the repo root. Do **not** run raw `docker compose` / `dotnet ef` unless no task exists (then add one to `Taskfile.yml` first).

| Task                           | Purpose                                                                                        |
| ------------------------------ | ---------------------------------------------------------------------------------------------- |
| `task ls`                      | List all tasks                                                                                 |
| `task build`                   | `dotnet build` solution                                                                        |
| `task run`                     | Run API locally (.NET)                                                                         |
| `task docker:build`            | Build images + start dev stack (Postgres + Redis + API; applies migrations via `Database:AutoMigrate`) |
| `task docker:postgres`         | Postgres + Redis only (for `task run`)                                                               |
| `task docker:up`               | Start dev containers (no rebuild)                                                              |
| `task docker:down`             | Stop dev containers                                                                            |
| `task migration:add -- <Name>` | Add EF migration                                                                               |
| `task migration:update`        | Apply pending migrations to DB                                                                 |
| `task migration:remove`        | Remove last migration                                                                          |

Prerequisite: `cp docker/.env.example docker/.env.local` (once).

## Run locally

```bash
task docker:postgres   # Postgres + Redis — or task docker:build for full stack
task run
```

Docs: http://localhost:8386/scalar

**Bedrock (AWS):** `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `BEDROCK_MODEL_ID` in `docker/.env.local` / `.env` or User Secrets for `task run`. **Email (Brevo SMTP):** `SMTP_*` in same env files. Template: [docker/.env.example](docker/.env.example), [docker/README.md](docker/README.md). Health: `GET /api/v1/bedrock/health`.

# [wokki-server] recent context, 2026-05-24 2:13pm GMT+7
