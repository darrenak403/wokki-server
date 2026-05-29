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
| **AWS (Bedrock)** | `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `BEDROCK_MODEL_ID` |
| **Brevo (SMTP)** | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USE_SSL`, `SMTP_FROM`, `SMTP_USERNAME`, `SMTP_PASSWORD` |
| **Cloudinary** | `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET` |
| **Redis** | `REDIS_PORT` (compose dùng `redis:6379` nội bộ) |

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

## Dev stack

```bash
task docker:build
```

API http://localhost:8386 · Scalar http://localhost:8386/scalar · pgweb http://localhost:8888

## Postgres + Redis (API local)

```bash
task docker:postgres
task run
```

Bedrock: `GET /api/v1/bedrock/health`
