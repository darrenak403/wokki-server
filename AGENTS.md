# AGENTS.md — Wokki Server

Backend: **.NET 10**, Clean Architecture, Minimal API, EF Core + PostgreSQL, Scalar docs.

## Before you code

1. Read [docs/README.md](docs/README.md) — documentation index (EN); Vietnamese: [docs/vi/README.md](docs/vi/README.md)
2. Read [docs/brd.md](docs/brd.md) and [docs/business-rules.md](docs/business-rules.md) — business intent and locked rules (`BR-xxx`); VI: [docs/vi/brd.md](docs/vi/brd.md), [docs/vi/business-rules.md](docs/vi/business-rules.md)
3. Read [docs/process-flows.md](docs/process-flows.md) when changing workflows or state machines
4. Read [docs/architecture.md](docs/architecture.md) and [docs/minimal-api.md](docs/minimal-api.md)
5. Follow [.cursor/rules/wokki-backend.mdc](.cursor/rules/wokki-backend.mdc)

## Hard rules

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

## Run locally

```bash
docker compose -f docker/docker-compose.dev.yml --env-file docker/.env.local up -d postgres
dotnet run --project src/Wokki.Api
```

Docs: http://localhost:8386/scalar


<claude-mem-context>
# Memory Context

# [wokki-server] recent context, 2026-05-23 8:31am GMT+7

No previous sessions found.
</claude-mem-context>