# FE Handoff: Platform Admin Control Center

> Branch: `dev`  
> Date: 2026-06-02

## 1) Endpoint map

- `GET /api/v1/platform/users` — Wokki admin xem user toàn hệ thống.
- `GET /api/v1/platform/organizations` — Wokki admin xem org + trạng thái gói + filter/sort/cảnh báo sắp hết hạn.
- `PUT /api/v1/platform/organizations/{id}/subscription` — bật/tắt hoặc gia hạn gói org.
- `GET /api/v1/platform/subscription-ledger` — lịch sử thay đổi gói toàn platform.
- `GET /api/v1/platform/organizations/{id}/subscription-ledger` — lịch sử thay đổi gói theo org.
- `GET /api/v1/platform/support/search` — search support theo org id, tên org, email user.
- `GET /api/v1/platform/support/organizations/{id}/context` — context support read-only của org.
- `GET /api/v1/platform/health` — diagnostics API/Bedrock/email cho PlatformOperator.
- `GET /api/v1/platform/usage-analytics` — active orgs + event counts + weekly trend.
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
| `status` | string | null | `NotActivated`, `Active`, `Expired`, `Disabled` |
| `sortBy` | string | `createdAt` | `createdAt`, `name`, `expiryDate` |
| `sortDirection` | string | `desc` | `asc`, `desc` |
| `expiringWithinDays` | number | 7 | controls `isExpiringSoon` |

Response `data.items[]`: `id`, `name`, `isActive`, `subscriptionStatus`, `subscriptionEnabled`, `subscriptionDurationDays`, `subscriptionActivatedAt`, `subscriptionExpiresAt`, `subscriptionUpdatedAt`, `createdAt`, `daysUntilExpiry`, `isExpiringSoon`, `userCount`, `locationCount`, `employeeCount`.

`subscriptionStatus`: `NotActivated` | `Active` | `Expired` | `Disabled`.

### Update org subscription

`PUT /api/v1/platform/organizations/{id}/subscription`

**Auth:** `PlatformOperator`

```json
{
  "enabled": true,
  "durationDays": 90
}
```

Validation: `durationDays` optional, `1..3650`. Enabling sets expiry to `now + durationDays`; disabling blocks org usage without deleting data. If `durationDays` is omitted on renew/enable, BE reuses stored `subscriptionDurationDays`; if none exists, BE rejects.

Every successful update creates an immutable ledger entry and an audit log in the same transaction.

### Subscription ledger

`GET /api/v1/platform/subscription-ledger`

| Query | Type | Default | Note |
|-------|------|---------|------|
| `page` | number | 1 | min 1 |
| `pageSize` | number | 10 | BE clamp 1..100 |
| `organizationId` | uuid | null | optional on global endpoint |
| `action` | string | null | optional |
| `from` | ISO datetime | null | optional |
| `to` | ISO datetime | null | optional |

Response item: `id`, `organizationId`, `action`, `previousStatus`, `newStatus`, `previousDurationDays`, `newDurationDays`, `previousExpiresAt`, `newExpiresAt`, `changedByUserId`, `changedAt`.

### Support console

`GET /api/v1/platform/support/search?query=&page=&pageSize=`

Search supports org id (exact GUID), org name contains, and user email contains. Response item: `matchType`, org metadata, optional matched user metadata, counts, `latestOperationalActivityAt`.

`GET /api/v1/platform/support/organizations/{id}/context`

Returns package state, org counts, latest schedule created/published, latest attendance clock-in, latest chat message, latest operational timestamp, and latest subscription ledger entry. It is read-only: no impersonation, no tenant business data edits, no tenant row contents.

### Health and usage analytics

`GET /api/v1/platform/health` returns overall status plus component statuses for API, Bedrock, and email. Do not wire this to public anonymous health UI; it requires PlatformOperator.

`GET /api/v1/platform/usage-analytics?windowDays=7|30&organizationId=...` returns `activeOrganizationCount`, `activeOrganizations`, `countsByEventType`, `weeklyActiveOrganizations`, `topOrganizations`.

Active org = at least one tracked event in the window: login, schedule publish/suggest/apply, attendance clock-in/out, or chat message.

## 3) Error codes

| HTTP | code | message |
|------|------|---------|
| 403 | `ORG_PACKAGE_NOT_ACTIVATED` | Organization package is not activated. |
| 402 | `ORG_PACKAGE_EXPIRED` | Organization package has expired. |
| 404 | `PLATFORM_ORG_NOT_FOUND` | Organization not found. |
| 400 | `VALIDATION_FAILED` | durationDays out of range |

## 4) FE notes

- PlatformOperator shell nên có bảng Users và Organizations; action gói nằm trên org row.
- Add tabs/modules: Org Registry, Subscription Ledger, Support Console, System Health, Usage Analytics.
- Org mới sau register sẽ NotActivated. Hiển thị màn chờ kích hoạt, không vào onboarding cho tới khi login lại thành công.
- Global API/auth handler: với `ORG_PACKAGE_NOT_ACTIVATED` hiển thị "Bạn chưa có gói sử dụng hệ thống."; với `ORG_PACKAGE_EXPIRED` hiển thị "Bạn phải gia hạn để tiếp tục dùng hệ thống."
- Paged responses dùng `data.items`, `data.page`, `data.pageSize`, `data.totalCount`, `data.totalPages`.
- UI must not offer impersonation or tenant business-data edit actions in Support Console phase 1.
