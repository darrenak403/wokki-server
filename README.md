# Wokki Server

Workforce management backend API built with Clean Architecture, providing scheduling, attendance tracking, employee management, chat, and payroll features for multi-tenant organizations.

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 |
| API | Minimal API with Scalar (OpenAPI) |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core |
| Caching | Redis 7 |
| Auth | JWT Bearer tokens |
| Real-time | SignalR |
| AI | AWS Bedrock (Google Gemma) |
| Email | Brevo SMTP |
| Image Storage | Cloudinary |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL and Redis)
- [Task](https://taskfile.dev) — task runner

## Installation

```bash
# Clone the repository
git clone <repository-url>
cd wokki-server

# Copy environment configuration
cp docker/.env.example docker/.env.local
```

## Local Development

### Option A: Full Docker Stack (Recommended)

Build and start all services (PostgreSQL, Redis, API, pgweb):

```bash
task docker:build
```

### Option B: Local .NET + Docker Services

Start only database services, then run the API locally:

```bash
task docker:postgres   # PostgreSQL + Redis only
task run              # Run API at http://localhost:8386
```

### Access Points

| Service | URL |
|---------|-----|
| API | http://localhost:8386 |
| API Docs (Scalar) | http://localhost:8386/scalar |
| Database UI (pgweb) | http://localhost:8888 |

### Default Seed Account

The database is seeded on first run when no users exist ([`SeedData.cs`](src/Wokki.Infrastructure/Persistence/SeedData.cs)).

| Email | Password | Role |
|-------|----------|------|
| admin@gmail.com | 12345@Abc | PlatformOperator (Wokki admin) |

### Authentication

```bash
# Login
curl -s -X POST http://localhost:8386/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gmail.com","password":"12345@Abc"}'

# Use accessToken on protected routes
curl -s http://localhost:8386/api/v1/auth/me -H "Authorization: Bearer <accessToken>"
```

## Available Commands

```bash
task ls                # List all available tasks
task build             # Build the solution
task run               # Run the API locally
task clean             # Clean build artifacts
task docker:build      # Build and start full Docker stack
task docker:up         # Start Docker containers (existing images)
task docker:down       # Stop containers (preserve volumes)
task docker:clear      # Stop and remove all volumes (full reset)
task migration:add -- <Name>   # Add a new migration
task migration:update          # Apply pending migrations
task migration:remove          # Remove last migration
task migration:list            # List migrations status
```

## Database Migrations

With `task docker:build`, migrations are applied automatically on startup (`Database:AutoMigrate: true`).

For local development with `task run`:

```bash
task migration:update
```

## Environment Variables

Key environment variables (see `docker/.env.example` for full list):

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | PostgreSQL username | wokki |
| `POSTGRES_PASSWORD` | PostgreSQL password | - |
| `POSTGRES_DB` | Database name | wokki |
| `POSTGRES_PORT` | PostgreSQL port | 5432 |
| `REDIS_PORT` | Redis port | 6379 |
| `API_PORT` | API exposed port | 8386 |
| `JWT_SECRET` | JWT signing key (min 32 chars) | - |
| `JWT_ACCESS_TOKEN_MINUTES` | Token expiry | 60 |
| `AWS_REGION` | AWS region for Bedrock | us-west-2 |
| `BEDROCK_MODEL_ID` | Bedrock model ID | google.gemma-3-27b-it |
| `SMTP_HOST` | SMTP server | smtp-relay.brevo.com |
| `SMTP_PORT` | SMTP port | 587 |
| `DATABASE_AUTOMIGRATE` | Auto-apply migrations | true |

## Solution Structure

```
src/
├── Wokki.Api/           Minimal API layer (HTTP handlers only)
├── Wokki.Application/   Business logic, DTOs, validators
├── Wokki.Infrastructure/ EF Core, JWT, Redis, external services
├── Wokki.Domain/        Entities, repository interfaces
└── Wokki.Common/       Shared utilities (ApiResponse, errors)
```

For detailed architecture documentation, see [docs/architecture.md](docs/architecture.md) and [docs/minimal-api.md](docs/minimal-api.md).
