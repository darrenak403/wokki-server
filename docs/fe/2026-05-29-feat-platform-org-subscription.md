# FE Handoff: Platform Org Subscription

> Branch: `dev`  
> Date: 2026-05-29

## 1) Endpoint map

- `GET /api/v1/platform/users` — Wokki admin xem user toàn hệ thống.
- `GET /api/v1/platform/organizations` — Wokki admin xem org + trạng thái gói.
- `PUT /api/v1/platform/organizations/{id}/subscription` — bật/tắt hoặc gia hạn gói org.
- `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh-token` — giờ chặn org chưa kích hoạt/hết hạn.

## 2) Contracts

### List platform users

`GET /api/v1/platform/users`

**Auth:** `PlatformOperator`

| Query | Type | Default | Note |
|-------|------|---------|------|
| `page` | number | 1 | min 1 |
| `pageSize` | number | 10 | BE clamp 1..100 |
| `organizationId` | uuid | null | optional |
| `role` | string | null | `PlatformOperator`, `Admin`, `Manager`, `User` |
| `search` | string | null | email hoặc org name |

Response `data.items[]`: `id`, `email`, `role`, `organizationId`, `organizationName`, `createdAt`.

### List platform organizations

`GET /api/v1/platform/organizations`

**Auth:** `PlatformOperator`

| Query | Type | Default | Note |
|-------|------|---------|------|
| `page` | number | 1 | min 1 |
| `pageSize` | number | 10 | BE clamp 1..100 |
| `search` | string | null | org name |

Response `data.items[]`: `id`, `name`, `isActive`, `subscriptionStatus`, `subscriptionEnabled`, `subscriptionDurationDays`, `subscriptionActivatedAt`, `subscriptionExpiresAt`, `subscriptionUpdatedAt`, `createdAt`, `userCount`, `locationCount`, `employeeCount`.

`subscriptionStatus`: `NotActivated` | `Active` | `Expired` | `Disabled`.

### Update org subscription

`PUT /api/v1/platform/organizations/{id}/subscription`

**Auth:** `PlatformOperator`

```json
{
  "enabled": true,
  "durationDays": 30
}
```

Validation: `durationDays` optional, `1..3650`. Enabling sets expiry to `now + durationDays`; disabling blocks org usage without deleting data.

## 3) Error codes

| HTTP | code | message |
|------|------|---------|
| 403 | `ORG_PACKAGE_NOT_ACTIVATED` | Organization package is not activated. |
| 402 | `ORG_PACKAGE_EXPIRED` | Organization package has expired. |
| 404 | `PLATFORM_ORG_NOT_FOUND` | Organization not found. |
| 400 | `VALIDATION_FAILED` | durationDays out of range |

## 4) FE notes

- PlatformOperator shell nên có bảng Users và Organizations; action gói nằm trên org row.
- Org mới sau register sẽ NotActivated. Hiển thị màn chờ kích hoạt, không vào onboarding cho tới khi login lại thành công.
- Global API/auth handler: với `ORG_PACKAGE_NOT_ACTIVATED` hiển thị "Bạn chưa có gói sử dụng hệ thống."; với `ORG_PACKAGE_EXPIRED` hiển thị "Bạn phải gia hạn để tiếp tục dùng hệ thống."
- Paged responses dùng `data.items`, `data.page`, `data.pageSize`, `data.totalCount`, `data.totalPages`.
