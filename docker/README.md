# Docker — Wokki Server (BE)

## Env files

| File | Dùng khi |
| ---- | -------- |
| `docker/.env.example` | Template |
| `docker/.env.local` | Dev — `task docker:build`, `docker:up` |
| `docker/.env` | Prod local / tham chiếu Dokploy |

```bash
cp docker/.env.example docker/.env.local   # dev
cp docker/.env.example docker/.env         # prod
```

## GitHub Secrets (chỉ 2 — cả 2 repo BE/FE)

| Secret | Mục đích |
| ------ | -------- |
| `DOCKER_USERNAME` | Login + push Docker Hub |
| `DOCKER_PASSWORD` | Token Docker Hub |

**Mọi biến khác** → env trên **Dokploy** (hoặc `docker/.env` khi chạy local).

## CI/CD → Dokploy

```
push main → GitHub Actions build/push image
         → Dokploy pull ${DOCKER_USERNAME}/wokki-backend:latest
         → compose up (env từ Dokploy UI)
```

## Prod compose

File: `docker/docker-compose.prod.yml`

| Service | Container | Image / volume |
| ------- | --------- | -------------- |
| API | `wokki_api` | `${DOCKER_USERNAME}/wokki-backend:latest` |
| Postgres | `wokki_postgres` | `wokki_postgres_data` |
| Redis | `wokki_redis` | `wokki_redis_data` |
| DbGate | `wokki_dbgate` | `127.0.0.1:5050` (optional admin) |

```bash
task docker:prod
```

### Env Dokploy (BE) — ví dụ

**Quan trọng:** `DOCKER_USERNAME` = username Docker Hub **thật** (VD `darrenak403`).  
Không để `your-dockerhub-username` hay placeholder từ `.env.example` → lỗi `not found`.

1. GitHub Actions **Docker publish** chạy OK → image có trên Docker Hub  
2. Dokploy **Registry** đã cấu hình token Docker Hub  
3. Dokploy **Environment**:

| Biến | Ghi chú |
| ---- | ------- |
| `DOCKER_USERNAME` | **Bắt buộc** — username Docker Hub thật |
| `IMAGE_TAG` | Optional, default `latest` |
| `API_PORT` | Optional, default `8386` |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Database |
| `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SECRET` | Auth |
| `CORS_ALLOW_ANY_ORIGIN` | Default `true` |
| `DATABASE_AUTOMIGRATE` | **`true` lần deploy đầu** (tạo bảng), sau đó đổi `false` |
| `SMTP_*`, `AWS_*`, `CLOUDINARY_*` | Integrations |

Deploy **BE trước FE** — tạo network `wokki-network`.

Domains: API `https://api.wokki.beyond8.io.vn` · FE `https://wokki.beyond8.io.vn`

### Tối ưu (prod)

| | Chi tiết |
|---|----------|
| **Image** | Alpine, non-root, build cache (NuGet/npm), GHA cache CI |
| **BE concurrency** | Kestrel 1000 conn / 256 WS, Npgsql pool 5–100, Redis LRU 256MB |
| **Postgres** | shared_buffers 256MB, max_connections 100 |
| **Log** | Giới hạn 10MB × 3 file |
| **RAM limit** | Tune qua `API_MEMORY_LIMIT`, `POSTGRES_MEMORY_LIMIT`, `REDIS_MEMORY_LIMIT`, `CLIENT_MEMORY_LIMIT` |

## Dev

```bash
task docker:build    # full stack
task docker:postgres # chỉ Postgres + Redis
task run             # API local
```

API http://localhost:8386 · pgweb http://localhost:8888
