# Wokki Server — Claude Code

Backend **Wokki Shift Ops MVP**: .NET 10, Clean Architecture, Minimal API, EF Core + PostgreSQL, SignalR chat, heuristic scheduling, optional AWS Bedrock schedule insight.

**Coding rules (mandatory):** [AGENTS.md](./AGENTS.md) · Cursor: [.cursor/rules/wokki-backend.mdc](./.cursor/rules/wokki-backend.mdc) · Claude: [.claude/rules/wokki-backend.md](./.claude/rules/wokki-backend.md)

**Product & codebase map:** [.cursor/contexts/wokki.md](./.cursor/contexts/wokki.md) (mirror: [.claude/contexts/wokki.md](./.claude/contexts/wokki.md))

**Sibling frontend:** `../wokki-client` — Next.js App Router; business docs below apply to both repos.

---

## Before you change behavior

Read in this order:

| # | Document | Why |
|---|----------|-----|
| 1 | [docs/README.md](./docs/README.md) | Doc index |
| 2 | [docs/brd.md](./docs/brd.md) | Product scope, stakeholders, FR/NFR |
| 3 | [docs/business-rules.md](./docs/business-rules.md) | Locked rules `BR-xxx` — never violate |
| 4 | [docs/process-flows.md](./docs/process-flows.md) | State machines (schedule, swap, pay period) |
| 5 | [docs/api-catalog.md](./docs/api-catalog.md) | REST + SignalR by role |
| 6 | [docs/architecture.md](./docs/architecture.md) + [docs/minimal-api.md](./docs/minimal-api.md) | Layers + endpoint pattern |

**Tiếng Việt:** [docs/vi/README.md](./docs/vi/README.md) · [docs/vi/fe-integration-guide.md](./docs/vi/fe-integration-guide.md)

When business behavior changes, update locked docs **and** mirror durable rules in `AGENTS.md` + `wokki-client/AGENTS.md` in the same task.

---

## Non-negotiable business (summary)

- **Roles:** `Admin`, `Manager`, `User` only — JWT + `RequireRole`.
- **Schedule:** `Draft` → `Published`; assignments editable only in **Draft**; `WeekStartDate` = Monday.
- **Preferences vs assignments:** Employee preferences are **advisory**; official schedule = published `ShiftAssignment`.
- **Self-service:** `GET /api/v1/self/*` requires linked `Employee` (not the same as `GET /auth/me`).
- **Swaps:** Only on **Published** schedules; cutoff rules in location timezone.
- **Auto-schedule:** Branch `LocationSchedulingPolicy` required first (`location-scheduling-policy.v3`); department overrides; use **department membership**, not only `Employee.DepartmentId`.
- **Bedrock:** Advisory insight/chat on schedule context snapshot only — must **not** create/apply/publish assignments; `suggest` / `apply-suggestions` work without Bedrock.

Full rule IDs: [docs/business-rules.md](./docs/business-rules.md).

---

## Repository layout

```text
src/
  Wokki.Api/              # HTTP only — Apis/{Feature}/{Feature}Endpoints.cs
  Wokki.Application/      # Services, Dtos, Validators, Mappings (no Features/* folder)
  Wokki.Domain/           # Entities, IUnitOfWork, repositories, RoleConstants
  Wokki.Infrastructure/   # EF, JWT, adapters
  Wokki.Common/           # ApiResponse<T>, AppMessages, ToHttpResult()
docs/                     # BRD, BR-xxx, API catalog, architecture
plans/                    # shift-ops-mvp, fe-handoff-flow-verification
.cursor/  .claude/        # Agent kit (skills, commands, hooks) + wokki context
```

### API modules (`src/Wokki.Api/Apis/`)

Register every new module in `Bootstrapping/PipelineExtensions.MapEndpoints()`.

| Module | File | Base path |
|--------|------|-----------|
| Auth | `Auth/AuthEndpoints.cs` | `/api/v1/auth` |
| Users | `Users/UserEndpoints.cs` | `/api/v1/users` |
| Employees | `Employees/EmployeeEndpoints.cs` | `/api/v1/employees` |
| Locations | `Locations/LocationEndpoints.cs` | `/api/v1/locations` (+ scheduling policy) |
| Departments | `Departments/DepartmentEndpoints.cs` | `/api/v1/departments` |
| Shifts | `Shifts/ShiftEndpoints.cs` | `/api/v1/shifts` |
| Schedules | `Schedules/ScheduleEndpoints.cs` | `/api/v1/schedules` (+ suggest, apply, insights) |
| Employee self | `EmployeeSelf/EmployeeSelfEndpoints.cs` | `/api/v1/self` |
| Swap | `SwapRequests/SwapRequestEndpoints.cs` | `/api/v1/swap-requests` |
| Attendance | `Attendance/AttendanceEndpoints.cs` | `/api/v1/attendance` |
| Payroll | `Payroll/PayrollEndpoints.cs` | `/api/v1/payroll` |
| Chat | `Chat/ChannelEndpoints.cs` | `/api/v1/channels` + SignalR hub |
| Bedrock | `Bedrock/BedrockEndpoints.cs` | `/api/v1/bedrock` |
| Health | `Health/HealthEndpoints.cs` | `/health` |

### Application services (`src/Wokki.Application/Services/`)

| Folder | Responsibility |
|--------|----------------|
| `Auth/`, `User/` | Login, tokens, user admin |
| `Employee/`, `Location/`, `Department/` | Master data |
| `Shift/` | Shift definitions |
| `Schedule/` | Schedules, assignments, preferences, department config, insights |
| `SwapRequest/` | Swap lifecycle |
| `Attendance/` | Clock in/out, adjust |
| `Payroll/` | Summary, export |
| `Chat/` | Channels, messages |
| `Bedrock/` | Optional LLM health/insight |

Pattern: inject `IUnitOfWork`, return `ApiResponse<T>` only; validators in `Validators/{Feature}/`.

---

## Commands (use Taskfile — not raw docker/ef)

```bash
task build                    # required before done
task run                      # API :8386
task docker:postgres          # DB for local run
task migration:add -- <Name>
task migration:update
```

Prerequisite once: `cp docker/.env.example docker/.env.local`

**API docs:** http://localhost:8386/scalar

**Seed users:** admin/manager/user `@gmail.com` — password `12345@Abc` (see repo README).

---

## Agent toolkit (`.cursor/` · `.claude/`)

Shared **cursor-skills** kit: `/ck-cook`, `/ck-plan`, `/ck-fix`, code-review skill, planner/debugger agents.

| Path | Purpose |
|------|---------|
| `contexts/dev.md` | Fast coding mode |
| `contexts/wokki.md` | **This product** — business + code map |
| `skills/code-review/` | Verify before claiming done |
| `commands/ck-*.md` | Slash command prompts |

Hooks (`.claude/settings.json`): build check after edits, session init, privacy block.

---

## Definition of done

1. `task build` passes  
2. `BR-xxx` and process flows respected  
3. New endpoints follow minimal-api pattern and are registered  
4. Business/doc updates included when behavior changed  
