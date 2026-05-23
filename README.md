# Wokki Server

Clean Architecture backend (.NET 10, Minimal API, EF Core, PostgreSQL).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL)

## Quick start (Docker — khuyến nghị)

Requires [Task](https://taskfile.dev). From repo root:

```bash
cp docker/.env.example docker/.env.local
task docker:build
```

- API: http://localhost:8386
- Scalar: http://localhost:8386/scalar

Chi tiết: [docker/README.md](docker/README.md) (Postgres, Bedrock env, pgweb)

**Bedrock:** `task run` → User Secrets; Docker dev → `docker/.env.local`; prod → `docker/.env`. Xem [docker/README.md](docker/README.md).

## Quick start (local .NET)

```bash
task docker:postgres   # chỉ Postgres; Bedrock qua User Secrets
task run
```

- API: http://localhost:8386

## Default seed (Wokki Coffê demo)

Seeded on first run when the database has no users ([`SeedData.cs`](src/Wokki.Infrastructure/Persistence/SeedData.cs) → [`CoffeeShopSeedBuilder.cs`](src/Wokki.Infrastructure/Persistence/CoffeeShopSeedBuilder.cs)). Dates use **`Asia/Ho_Chi_Minh`**.

**Password (all accounts):** `12345@Abc`

| Email | Role |
|-------|------|
| admin@gmail.com | Admin (chủ quán) |
| manager@gmail.com | Manager (trưởng ca) |
| user@gmail.com | User (barista demo) |
| barista1@gmail.com … barista5@gmail.com | User |

**Also seeded:** location **Wokki Coffê**, departments **Quầy bar** + **Pha chế**, shift definitions, **published weekly schedule**, attendance (closed), pay period, pending swap, chat channels.

**Reset demo data:** `task docker:clear` then `task docker:build`.

Full IDs: [docs/fe/seed-credentials.md](docs/fe/seed-credentials.md).

## Auth flow

```bash
# Login
curl -s -X POST http://localhost:8386/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gmail.com","password":"12345@Abc"}'

# Use accessToken on protected routes
curl -s http://localhost:8386/api/v1/auth/me -H "Authorization: Bearer <accessToken>"
```

## Migrations

```bash
task migration:add -- <MigrationName>
task migration:update          # local DB when using task docker:postgres + task run
```

With `task docker:build`, the API applies pending migrations on startup (`Database:AutoMigrate`).

`Database:AutoMigrate` defaults to `true` in Development, `false` in Production.

All tasks: `task ls`

## Solution structure

```
src/
  Wokki.Common/         ApiResponse, AppMessages, ToHttpResult
  Wokki.Domain/         Entities, IUnitOfWork, repositories
  Wokki.Application/    Services, DTOs, validators
  Wokki.Infrastructure/ EF Core, JWT, caching
  Wokki.Api/            Minimal API (Bootstrapping + Apis/*)
```

See [docs/architecture.md](docs/architecture.md) and [docs/minimal-api.md](docs/minimal-api.md). Agents: read [AGENTS.md](AGENTS.md).
