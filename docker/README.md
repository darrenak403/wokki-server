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

## GitHub Secrets

| Secret | Mục đích |
| ------ | -------- |
| `DOCKER_USERNAME` | Login + push Docker Hub |
| `DOCKER_PASSWORD` | Token Docker Hub |
| `DOKPLOY_WEBHOOK_URL` | Bắt buộc — trigger Dokploy redeploy sau khi push image (xem cách lấy bên dưới) |
| `DOKPLOY_API_TOKEN` | Optional, **thường không cần** — Webhook URL của Dokploy (dạng `/api/deploy/compose/<id>`) đã tự chứa token trong path, không cần thêm header. Chỉ set biến này nếu Dokploy báo lỗi 401/403 khi gọi webhook |

Optional GitHub repo **Variable**: `API_HEALTH_URL` (default `https://api.wokki.io.vn/health/`) nếu domain prod đổi.

**Mọi biến khác** → env trên **Dokploy** (hoặc `docker/.env` khi chạy local).

### Lấy Dokploy webhook URL

1. Vào Dokploy UI → Application (compose `BE`) → tab **Deployments**
2. Copy **Webhook URL** hiển thị sẵn (dạng `https://<dokploy-domain>/api/deploy/compose/<id>`) — URL này đã chứa token riêng trong path, giữ kín như secret
3. Add vào GitHub repo → Settings → Secrets and variables → Actions → `DOKPLOY_WEBHOOK_URL`
4. Không cần `DOKPLOY_API_TOKEN` với loại webhook này — bỏ qua secret này

## CI/CD → Dokploy (tự động)

```
push main → GitHub Actions build/push image (+ verify non-Alpine)
         → job "deploy": POST DOKPLOY_WEBHOOK_URL (tự động, không cần bấm tay)
         → Dokploy compose pull (pull_policy: always) + up -d (env từ Dokploy UI)
         → job "deploy": poll /health đến khi container healthy, fail loudly nếu timeout
```

Không còn bước thủ công — chỉ cần `git push` lên `main`, workflow tự build → push image → trigger Dokploy → verify health. Nếu `DOKPLOY_WEBHOOK_URL` chưa được set, job `deploy` fail rõ ràng (không silent-skip) để nhắc cấu hình secret.

`pull_policy: always` trên `wokki_api` (và `wokki_dbgate`) trong `docker-compose.prod.yml` đảm bảo mỗi lần bấm Deploy trên Dokploy, image luôn được kiểm tra/pull lại từ registry — không bị kẹt ở image cũ cache local trên server.

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

**Domains (prod):** FE `https://wokki.io.vn` · API `https://api.wokki.io.vn`

> CORS phải có `https://wokki.io.vn`. FE Dokploy: `NEXT_PUBLIC_APP_URL` / `NEXT_PUBLIC_API_URL` khớp hai URL trên.

### Tối ưu (prod)

| | Chi tiết |
|---|----------|
| **Image** | Debian-based .NET runtime (OR-Tools compatible), non-root, build cache (NuGet/npm), GHA cache CI |
| **BE concurrency** | Kestrel 1000 conn / 256 WS, Npgsql pool 5–100, Redis LRU 256MB |
| **Postgres** | shared_buffers 256MB, max_connections 100 |
| **Log** | Giới hạn 10MB × 3 file |
| **RAM limit** | Tune qua `API_MEMORY_LIMIT`, `POSTGRES_MEMORY_LIMIT`, `REDIS_MEMORY_LIMIT`, `CLIENT_MEMORY_LIMIT` |

### CI reliability notes

- `docker-publish.yml` dùng retry 2 lần cho push để giảm lỗi mạng thoáng qua từ Docker Hub.
- Workflow có bước verify `latest` bằng `docker pull` + kiểm tra `/etc/os-release` để ngăn publish nhầm Alpine runtime.
- Prod compose hỗ trợ `DOCKER_PLATFORM` (mặc định `linux/amd64`) để tránh pull nhầm kiến trúc.

## Dev

```bash
task docker:build    # full stack
task docker:postgres # chỉ Postgres + Redis
task run             # API local
```

API http://localhost:8386 · pgweb http://localhost:8888
