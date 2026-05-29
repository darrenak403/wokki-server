# FE Handoff: Self-serve org + package gate + tenant URL

> Branch: `dev`  
> Date: 2026-05-29  
> **Client guide:** [wokki-client/docs/fe-implementation-guide.md](../../../wokki-client/docs/fe-implementation-guide.md)

## 1) Endpoint map

- `POST /api/v1/auth/register` — org + Org Admin (org **chưa** có gói)
- `POST /api/v1/auth/login` — chặn org user nếu gói chưa bật / hết hạn
- `POST /api/v1/auth/refresh-token` — cùng gate gói
- `GET /api/v1/org/stats` — probe gói (Admin/Manager)
- `GET /api/v1/platform/stats` — PlatformOperator dashboard
- `GET /api/v1/platform/organizations` — list org + `subscriptionStatus`
- `PUT /api/v1/platform/organizations/{id}/subscription` — Wokki admin bật/gia hạn **N ngày**
- Mọi `/api/v1/*` org business — middleware chặn nếu gói invalid

## 2) Contracts

### Bật / gia hạn gói org

`PUT /api/v1/platform/organizations/{organizationId}/subscription`

**Auth:** PlatformOperator

**Body:**

```json
{
  "enabled": true,
  "durationDays": 50
}
```

Validation:

- `durationDays` **bắt buộc** khi `enabled: true` (`1..3650`)
- Không hardcode 30 — FE lấy từ input admin
- BE set `subscriptionExpiresAt = UtcNow + durationDays` — sau đó **mọi** user thuộc org không login/API được

Tắt gói:

```json
{ "enabled": false }
```

**Response `data`:** `PlatformOrganizationResponse` (có `subscriptionStatus`, `subscriptionExpiresAt`, …)

### Login (org user, gói hết hạn)

**Response failure:**

```json
{
  "success": false,
  "message": {
    "code": "ORG_PACKAGE_EXPIRED",
    "text": "Organization package has expired.",
    "statusCode": 402
  }
}
```

## 3) Error codes

| HTTP | code | FE copy |
|------|------|---------|
| 403 | `ORG_PACKAGE_NOT_ACTIVATED` | Chưa có gói — liên hệ Wokki |
| 402 | `ORG_PACKAGE_EXPIRED` | Gói hết hạn — cần gia hạn org |
| 400 | `SUBSCRIPTION_DURATION_REQUIRED` | Thiếu số ngày khi bật gói |

## 4) FE notes

- Implement package UI: `app/(auth)/org-package`, `OrgPackageGuard`, platform org table (see client guide §3).
- Tenant URLs: `buildTenantNav`, `useTenantNavigation` — không link `/admin/...` thuần.
- Register success → `/org-package?reason=not-activated`, không onboarding trước khi có gói.
