# Docker

## Env files

| File           | Môi trường        | Dùng với                         |
| -------------- | ----------------- | -------------------------------- |
| `.env.example` | Template          | `cp` → `.env.local` hoặc `.env`  |
| `.env.local`   | Dev Docker        | `task docker:build`, `docker:up` |
| `.env`         | Production Docker | `task docker:prod`               |

```bash
cp docker/.env.example docker/.env.local   # dev
cp docker/.env.example docker/.env         # prod
```

## Biến chính

| Nhóm | Biến |
| ---- | ---- |
| **GitHub Secrets** | `DOCKER_USERNAME`, `DOCKER_PASSWORD` — CI build/push image (Settings → Secrets and variables → Actions) |
| **Deploy (docker/.env)** | `DOCKER_USERNAME` (trùng secret, cho tên image); **không** ghi `DOCKER_PASSWORD` vào file |
| **Deploy (docker/.env)** | `IMAGE_TAG` (optional, default `latest`) |
| **AWS (Bedrock)** | `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `BEDROCK_MODEL_ID` |
| **Brevo (SMTP)** | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USE_SSL`, `SMTP_FROM`, `SMTP_USERNAME`, `SMTP_PASSWORD` |
| **Cloudinary** | `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET` |
| **Redis** | `REDIS_PORT` (dev expose host; compose nội bộ `wokki_redis:6379`) |
| **Prod DB UI** | `DBGATE_PORT` (default `5050`, bind `127.0.0.1`) |

Compose chỉ **map** env → `Smtp__*` / `AWS__*`; không hardcode host SMTP.

### Lấy key

- **AWS IAM:** [IAM Console](https://console.aws.amazon.com/iam) → Access key  
- **Bedrock model:** [Bedrock Console](https://console.aws.amazon.com/bedrock) → Model access  
- **Brevo SMTP:** [app.brevo.com](https://app.brevo.com) → SMTP & API → SMTP key  
  - `SMTP_USERNAME` = email đăng ký Brevo  
  - `SMTP_PASSWORD` = SMTP key (`xsmtpsib-...`)
- **Cloudinary:** [cloudinary.com/console](https://cloudinary.com/console) → Dashboard → API Keys  
  - Dùng cho upload ảnh QR thanh toán lương (`/api/v1/self/profile/payment-qr`)

## Local API (`task run`)

`.env.local` không được `dotnet run` đọc. User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project src/Wokki.Api

dotnet user-secrets set "AWS:Region" "us-west-2" --project src/Wokki.Api
dotnet user-secrets set "AWS:AccessKeyId" "..." --project src/Wokki.Api
dotnet user-secrets set "AWS:SecretAccessKey" "..." --project src/Wokki.Api
dotnet user-secrets set "AWS:Bedrock:Region" "us-west-2" --project src/Wokki.Api
dotnet user-secrets set "AWS:Bedrock:ModelId" "google.gemma-3-27b-it" --project src/Wokki.Api

dotnet user-secrets set "Smtp:Host" "smtp-relay.brevo.com" --project src/Wokki.Api
dotnet user-secrets set "Smtp:Port" "587" --project src/Wokki.Api
dotnet user-secrets set "Smtp:UseSsl" "true" --project src/Wokki.Api
dotnet user-secrets set "Smtp:From" "noreply@your-domain.com" --project src/Wokki.Api
dotnet user-secrets set "Smtp:Username" "your-brevo-email" --project src/Wokki.Api
dotnet user-secrets set "Smtp:Password" "YOUR_BREVO_SMTP_KEY" --project src/Wokki.Api

dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name" --project src/Wokki.Api
dotnet user-secrets set "Cloudinary:ApiKey" "..." --project src/Wokki.Api
dotnet user-secrets set "Cloudinary:ApiSecret" "..." --project src/Wokki.Api
```

Chưa cấu hình SMTP → dev log OTP ra console.

## Production stack (Dokploy)

### Quy trình

```
GitHub Actions (wokki-server / wokki-client, push main)
  → build & push Docker Hub
  → Dokploy: pull image + docker compose up (env đã cấu hình trên UI)
```

### GitHub Secrets (repo wokki-server)

| Secret | Mô tả |
| ------ | ----- |
| `DOCKER_USERNAME` | Username Docker Hub |
| `DOCKER_PASSWORD` | Access token registry |

Workflow **Docker publish** → `${DOCKER_USERNAME}/wokki-backend:latest` (+ tag `sha`).

### Dokploy (app BE)

| Cấu hình | Giá trị |
| -------- | ------- |
| Loại | Docker Compose |
| Compose file | `docker/docker-compose.prod.yml` |
| Registry | Docker Hub (credentials trong Dokploy — **không** cần `docker login` thủ công) |
| Env runtime | Xem bảng dưới |

**Env bắt buộc trên Dokploy (BE):**

| Biến | Ghi chú |
| ---- | ------- |
| `DOCKER_USERNAME` | Trùng Docker Hub username (cho tên image) |
| `IMAGE_TAG` | Optional, default `latest` |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Postgres container |
| `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SECRET` | Auth |
| `CORS_ALLOW_ANY_ORIGIN` | Optional, default `true` — cho phép mọi origin gọi API |
| `SMTP_*`, `AWS_*`, `CLOUDINARY_*` | Integrations |
| `DATABASE_AUTOMIGRATE` | `true` lần deploy đầu, sau đó `false` |
| `APIDOCS_ENABLED` | `false` prod |

**Deploy BE trước FE** — stack BE tạo network `wokki-network`.

| Service | Container | Port / ghi chú |
| ------- | --------- | -------------- |
| API | `wokki_api` | `8386` — image `${DOCKER_USERNAME}/wokki-backend:latest` |
| Postgres | `wokki_postgres` | nội bộ — volume `wokki_postgres_data` |
| Redis | `wokki_redis` | nội bộ — volume `wokki_redis_data` |
| DbGate | `wokki_dbgate` | `127.0.0.1:5050` — optional, SSH tunnel |

### Manual (không qua Dokploy)

```bash
cp docker/.env.example docker/.env
task docker:prod
```

Build/push thủ công:

```bash
docker login -u "$DOCKER_USERNAME"
docker build -f docker/Dockerfile -t ${DOCKER_USERNAME}/wokki-backend:latest .
docker push ${DOCKER_USERNAME}/wokki-backend:latest
```

## Dev stack

```bash
task docker:build
```

| Service | Container | Image / volume |
| ------- | --------- | -------------- |
| API | `wokki_api_dev` | `wokki/wokki-backend:dev` (local build) |
| Postgres | `wokki_postgres_dev` | `wokki_postgres_data_dev` |
| Redis | `wokki_redis_dev` | `wokki_redis_data_dev` |
| pgweb | `wokki_pgweb_dev` | — |

API http://localhost:8386 · Scalar http://localhost:8386/scalar · pgweb http://localhost:8888

## Postgres + Redis (API local)

```bash
task docker:postgres
task run
```

Bedrock: `GET /api/v1/bedrock/health`
