# AGENTS.md — Wokki Server

Backend: **.NET 10**, Clean Architecture, Minimal API, EF Core + PostgreSQL, Scalar docs.

## Before you code

1. Read [docs/architecture.md](docs/architecture.md)
2. Read [docs/minimal-api.md](docs/minimal-api.md)
3. Follow [.cursor/rules/wokki-backend.mdc](.cursor/rules/wokki-backend.mdc)

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
