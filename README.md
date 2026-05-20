# Wokki Server

Clean Architecture backend (.NET 10, Minimal API, EF Core, PostgreSQL).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL)

## Quick start (Docker — khuyến nghị)

```bash
cd docker
cp .env.example .env.local
docker compose -f docker-compose.dev.yml --env-file .env.local up -d --build
```

- API: http://localhost:8386
- Scalar: http://localhost:8386/scalar

Chi tiết: [docker/README.md](docker/README.md)

## Quick start (local .NET)

```bash
cd docker && cp .env.example .env.local
docker compose -f docker-compose.dev.yml --env-file .env.local up -d postgres

dotnet run --project src/Wokki.Api
```

- API: http://localhost:8386

## Default seed users

| Email | Password | Role |
|-------|----------|------|
| admin@wokki.local | admin123 | Admin |
| user@wokki.local | user123 | User |

## Auth flow

```bash
# Login
curl -s -X POST http://localhost:8386/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@wokki.local","password":"admin123"}'

# Use accessToken on protected routes
curl -s http://localhost:8386/api/v1/auth/me -H "Authorization: Bearer <accessToken>"
```

## Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/Wokki.Infrastructure \
  --startup-project src/Wokki.Api \
  --output-dir Persistence/Migrations
```

`Database:AutoMigrate` defaults to `true` in Development, `false` in Production.

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
