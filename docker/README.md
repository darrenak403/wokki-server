# Docker

## Env

| File | Khi nào dùng |
|------|----------------|
| `.env.example` | Template (commit) — `cp` sang file bên dưới |
| `.env.local` | **Luôn dùng khi dev local** (không commit) |
| `.env` | **Chỉ prod** — tạo khi deploy, không dùng cho dev (không commit) |

```bash
# Dev — một lần
cp .env.example .env.local
```

## Dev

```bash
cd docker
docker compose -f docker-compose.dev.yml --env-file .env.local up -d --build
```

- http://localhost:8386 — API  
- http://localhost:8386/scalar — docs  
- http://localhost:8888 — pgweb (DB UI, auto-connect)

## Prod

```bash
cd docker
cp .env.example .env
# Sửa .env: mật khẩu DB, JWT_SECRET, DATABASE_AUTOMIGRATE=false, APIDOCS_ENABLED=false
# pgAdmin prod chạy local-only: http://127.0.0.1:8081

docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

## Chỉ Postgres (API chạy `dotnet run` local)

```bash
docker compose -f docker-compose.dev.yml --env-file .env.local up -d postgres
dotnet run --project ../src/Wokki.Api
```
