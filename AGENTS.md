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
- **Schedule preferences are advisory** — employee đăng ký ca is separate from official `ShiftAssignment`; Admin/Manager decides final Draft/Published schedule. Users can update preferences only while the schedule is Draft; Published preferences are view-only.
- **Branch workspace access is scoped** — Admin manages every branch in their org. Manager manages only locations assigned through `LocationManager`. UI workspace/sidebar actions are scoped to the selected branch URL (`/{orgId}/{locationId}/{role}/...`); an org-level workspace is only a redirect/selection fallback. Org Admin **creates staff via employee accounts** with a department (`POST /api/v1/employees` creates both `User` + `Employee`; same-org legacy Users without Employee are linked there); never create standalone org staff via `/users`. The system auto-provisions **Active** `LocationMembership` at that department's location — employees log in directly (no self-serve join request). Branch changes use workspace transfer APIs (`POST /api/v1/workspace/location/transfer`), and department transfer must target the employee's active branch.
- **Auto-scheduling is branch-rule first** — `LocationSchedulingPolicy` (`location-scheduling-policy.v5`) exposes a minimal solver UI surface (no publish/apply toggles); map with `LocationSchedulingSolverPolicy`. Suggestions are applied only via explicit UI action (`SchedulingSolverDefaults.SuggestionsRequireExplicitApply`). Weekly max shifts and other caps use `SchedulingSolverDefaults`. Hierarchy: location → department → employee (no job-position sub-entity). Role match uses `Employee.Position` vs shift `RequiredRole`. Custom branch rules stored but not read by solver yet.
- **Bedrock schedule insight is advisory only** — AWS Bedrock may answer questions from a generated weekly schedule context snapshot, but it must not generate, apply, update, or publish `ShiftAssignment`. `suggest` / `apply-suggestions` must keep working when Bedrock is unavailable.
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
| `task docker:build`            | Build images + start dev stack (Postgres + API; applies migrations via `Database:AutoMigrate`) |
| `task docker:postgres`         | Postgres only (for `task run`)                                                                 |
| `task docker:up`               | Start dev containers (no rebuild)                                                              |
| `task docker:down`             | Stop dev containers                                                                            |
| `task migration:add -- <Name>` | Add EF migration                                                                               |
| `task migration:update`        | Apply pending migrations to DB                                                                 |
| `task migration:remove`        | Remove last migration                                                                          |

Prerequisite: `cp docker/.env.example docker/.env.local` (once).

## Run locally

```bash
task docker:postgres   # or task docker:build for full stack
task run
```

Docs: http://localhost:8386/scalar

**Bedrock:** `AWS:Bedrock` in `appsettings.json`. **Local `task run`:** User Secrets (`AWS:AccessKeyId`, `AWS:SecretAccessKey`, `AWS:Bedrock:*`). **Dev Docker:** `docker/.env.local` only. **Prod Docker:** `docker/.env` only. Health: `GET /api/v1/bedrock/health`.

# [wokki-server] recent context, 2026-05-24 2:13pm GMT+7
