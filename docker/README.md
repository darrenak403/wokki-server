# Docker

## Env files

| File           | Môi trường              | Commit? | Dùng với                          |
| -------------- | ----------------------- | ------- | --------------------------------- |
| `.env.example` | Template                | Có      | `cp` → `.env.local` hoặc `.env`   |
| `.env.local`   | Dev Docker              | Không   | `task docker:build`, `docker:up`  |
| `.env`         | Production Docker       | Không   | `task docker:prod` hoặc compose prod |

**Local API (`task run`):** chỉ **User Secrets** (`AWS:AccessKeyId`, `AWS:SecretAccessKey`, `AWS:Bedrock:*`). File `docker/.env.local` **không** được `dotnet run` đọc.

**Dev Docker:** chỉ **`docker/.env.local`** → biến compose map sang `AWS__*`, `AWS__Bedrock__*`.

**Prod Docker:** chỉ **`docker/.env`** (tạo khi deploy).

```bash
# Dev Docker — một lần
cp docker/.env.example docker/.env.local
# Sửa: JWT_SECRET, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, BEDROCK_*
```

## AWS / Bedrock (compose → app config)

| Biến trong `.env` / `.env.local` | ASP.NET config key              |
| -------------------------------- | ------------------------------- |
| `AWS_REGION`                     | `AWS:Region`                    |
| `AWS_ACCESS_KEY_ID`              | `AWS:AccessKeyId`               |
| `AWS_SECRET_ACCESS_KEY`          | `AWS:SecretAccessKey`           |
| `BEDROCK_REGION`                 | `AWS:Bedrock:Region`            |
| `BEDROCK_MODEL_ID`               | `AWS:Bedrock:ModelId`           |
| `BEDROCK_HEALTH_CHECK_MODEL_ID`  | `AWS:Bedrock:HealthCheckModelId` (tùy chọn) |

Tune mặc định (`MaxTokens`, health tokens, …) trong `src/Wokki.Api/appsettings.json` → `AWS:Bedrock`.

Health: `GET /api/v1/bedrock/health` — chi tiết [docs/fe/bedrock-health.md](../docs/fe/bedrock-health.md).

## Dev stack

From **repo root**:

```bash
cp docker/.env.example docker/.env.local
task docker:build
```

- http://localhost:8386 — API
- http://localhost:8386/scalar — docs
- http://localhost:8888 — pgweb

## Prod stack

```bash
cp docker/.env.example docker/.env
# Sửa .env: DB, JWT, AWS_*, BEDROCK_*, DATABASE_AUTOMIGRATE=false, APIDOCS_ENABLED=false, DB_UI_*
task docker:prod
```

pgAdmin: http://127.0.0.1:8081 (chỉ localhost)

## Chỉ Postgres (API chạy local + User Secrets)

```bash
task docker:postgres
task run
```
