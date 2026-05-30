# API Catalog

Base URL: `/api/v1` unless noted. Auth: **Bearer JWT** except `/health` and auth login/register.

Rate limits: **`Fixed`** (100/min) default; **`Clock`** (300/min) for attendance clock endpoints.

## Auth (`/api/v1/auth`)

| Method | Path               | Roles         | Description                                                                                                                              |
| ------ | ------------------ | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| POST   | `/login`           | Anonymous     | Issue tokens                                                                                                                             |
| POST   | `/register`        | Anonymous     | Self-serve org signup: `email`, `password`, `organizationName` → Org Admin + JWT with `organization_id`; org package starts NotActivated |
| POST   | `/refresh-token`   | Authenticated | Refresh JWT                                                                                                                              |
| GET    | `/me`              | Authenticated | Current user                                                                                                                             |
| POST   | `/logout`          | Authenticated | Logout                                                                                                                                   |
| POST   | `/reset-password`  | Authenticated | Change password while logged in: `{ currentPassword, newPassword, confirmNewPassword }`; clears `mustChangePassword`                   |
| POST   | `/forgot-password` | Anonymous     | Send 6-digit OTP email (1 min TTL). Blocked while live OTP exists (`AUTH_OTP_RESEND_TOO_SOON`, 429). Max 5 sends per email then 30 min lock (`AUTH_OTP_SEND_LOCKED`, 429) |
| POST   | `/forgot-password/verify-otp` | Anonymous | Verify OTP: `{ email, otpCode }`                                                                                          |
| POST   | `/forgot-password/complete`   | Anonymous | Set new password after verified OTP: `{ email, newPassword, confirmNewPassword }`                                         |

## Users (`/api/v1/users`) — Admin patterns

| Method | Path    | Roles | Description |
| ------ | ------- | ----- | ----------- |
| GET    | `/`     | Admin | Paged users |
| GET    | `/{id}` | Admin | User by id  |
| POST   | `/`     | Admin | Deprecated/blocked for org staff; use `POST /employees` so account + Employee profile are created together |

## Foundation

| Resource    | Base           | Manager                                       | Admin | Notes                                                                                                                                                                |
| ----------- | -------------- | --------------------------------------------- | ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Employees   | `/employees`   | Scoped list/read                              | Full  | Creates User + Employee together; same-org legacy User without Employee is linked. Manager sees employees with Active membership in assigned branches. Soft delete = terminate. List/detail includes employee self-service **payment profile** (bank + QR URL) for payroll transfer |
| Locations   | `/locations`   | Scoped read; `PUT /{id}` in assigned branches | Full  | Manager sees assigned locations only; may update branch metadata in scope (workspace drawer). `GET/PUT /locations/{id}/scheduling-policy` — policy write Admin only. |
| Departments | `/departments` | Scoped read; `PUT /{id}` in assigned branches | Full  | Manager sees departments in assigned locations only; may update department in scope.                                                                                 |

## Branch membership (`/api/v1/location-memberships`)

| Method | Path                          | Roles                              | Description                                                                                      |
| ------ | ----------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------------------ |
| GET    | `/my`                         | Authenticated (+ employee profile) | Current user's **Active** location membership (auto-provisioned when Org Admin creates employee) |
| GET    | `/locations/{id}/memberships` | Admin, Manager                     | List memberships for a branch; Manager must manage that branch                                   |

**Removed (2026-05-29):** self-serve join flow — `POST /request`, `GET /pending`, `PATCH /{id}/review`. Org Admin creates employees; branch membership is provisioned automatically from `DepartmentId`.

## Workspace transfer (`/api/v1/workspace`)

| Method | Path                   | Roles          | Description                                                                                                                                                                |
| ------ | ---------------------- | -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| POST   | `/location/transfer`   | Admin, Manager | Move an employee to another branch within caller scope.                                                                                                                    |
| POST   | `/department/transfer` | Admin, Manager | Move an employee to a department in the employee's current Active branch. Cross-branch department placement returns `WS_EMPLOYEE_WRONG_LOCATION`; transfer location first. |

## Scheduling

| Method              | Path                                         | Roles          | Description                                                            |
| ------------------- | -------------------------------------------- | -------------- | ---------------------------------------------------------------------- |
| GET/POST            | `/schedules`                                 | Admin, Manager | List / create (Draft)                                                  |
| GET/PUT/DELETE      | `/schedules/{id}`                            | Admin, Manager | Detail / update / delete Draft                                         |
| POST                | `/schedules/{id}/publish`                    | Admin, Manager | Draft → Published                                                      |
| POST                | `/schedules/{id}/unpublish`                  | Admin, Manager | Published → Draft                                                      |
| POST                | `/schedules/{id}/copy`                       | Admin, Manager | Copy week to new Draft                                                 |
| GET/POST            | `/schedules/{id}/assignments`                | Admin, Manager | List / add assignment                                                  |
| DELETE              | `/schedules/{id}/assignments/{assignmentId}` | Admin, Manager | Remove assignment                                                      |
| POST                | `/schedules/{id}/suggest`                    | Admin, Manager | Schedule suggestions (no write; Bedrock not used)                      |
| POST                | `/schedules/{id}/apply-suggestions`          | Admin, Manager | Apply suggestions (Draft only)                                         |
| POST                | `/schedules/{id}/insights/context`           | Admin, Manager | Generate/refresh JSON context snapshot for schedule insight            |
| GET                 | `/schedules/{id}/insights/context`           | Admin, Manager | Read latest context snapshot                                           |
| POST                | `/schedules/{id}/insights/chat`              | Admin, Manager | Ask optional Bedrock assistant about the context; no schedule mutation |
| GET/POST/PUT/DELETE | `/shifts`                                    | Admin, Manager | Shift definitions                                                      |
| POST                | `/shifts/copy`                               | Admin, Manager | Copy active shifts from source department to target departments (same location; skips duplicates by name+time) |

## Employee self-service (`/api/v1/self`) — User (+ employee profile)

Not the same as `GET /api/v1/auth/me` (login account). These routes require a linked Employee profile.

| Method | Path                  | Description                                  |
| ------ | --------------------- | -------------------------------------------- |
| GET    | `/self/schedule`      | Own upcoming published assignments (28 days) |
| GET    | `/self/swap-requests` | Swap requests sent/received                  |
| GET    | `/self/attendance`    | Own attendance history                       |
| GET    | `/self/profile`       | Own employee profile (name, phone, org context) |
| PUT    | `/self/profile`       | Update own profile: name, phone, bank account fields; optional `removePaymentQr` |
| POST   | `/self/profile/payment-qr` | Upload payment QR image (multipart `file`, Cloudinary, max 5MB) |

## Swap requests (`/api/v1/swap-requests`)

| Method | Path                     | Roles            | Description           |
| ------ | ------------------------ | ---------------- | --------------------- |
| POST   | `/`                      | User             | Create swap           |
| GET    | `/`                      | Admin, Manager   | List (filters)        |
| GET    | `/{id}`                  | Authenticated    | Detail (access rules) |
| POST   | `/{id}/accept`           | User (target)    | Accept + auto-apply   |
| POST   | `/{id}/decline`          | User (target)    | Decline               |
| POST   | `/{id}/cancel`           | User (requester) | Cancel                |
| POST   | `/{id}/override-approve` | Admin, Manager   | Manager override      |
| POST   | `/{id}/override-reject`  | Admin, Manager   | Manager override      |

## Attendance (`/api/v1/attendance`)

| Method | Path           | Roles          | Rate  | Description                |
| ------ | -------------- | -------------- | ----- | -------------------------- |
| POST   | `/clock-in`    | User           | Clock | Start record               |
| POST   | `/clock-out`   | User           | Clock | End + minutes              |
| GET    | `/`            | Admin, Manager | Fixed | List/filter                |
| PUT    | `/{id}/adjust` | Admin, Manager | Fixed | Manual adjust + audit note |

## Payroll (`/api/v1/payroll`)

| Method | Path                    | Roles          | Description                   |
| ------ | ----------------------- | -------------- | ----------------------------- |
| GET    | `/summary`              | Admin, Manager | Department pay period summary; each line includes employee **payment profile** (bank + QR URL) |
| GET    | `/summary/{employeeId}` | Admin, Manager | Employee breakdown + payment profile |
| POST   | `/summary/export`       | Admin          | CSV download (includes bank columns + QR URL) |

Query/body: `departmentId`, `startDate`, `endDate` (`PayrollPeriodRequest`).

## Chat (`/api/v1/channels`)

| Method | Path                     | Roles                      | Description                    |
| ------ | ------------------------ | -------------------------- | ------------------------------ |
| GET    | `/`                      | Authenticated              | Channels for caller's employee |
| POST   | `/`                      | Admin, Manager             | Create Direct/Group            |
| GET    | `/{id}/messages`         | Member (Admin bypass read) | Cursor: `?before=&limit=`      |
| POST   | `/{id}/messages`         | Member                     | Send + SignalR push            |
| DELETE | `/{id}/messages/{msgId}` | Sender or Admin            | Soft delete                    |

## Stats

| Method | Path              | Roles            | Description                                                   |
| ------ | ----------------- | ---------------- | ------------------------------------------------------------- |
| GET    | `/platform/stats` | PlatformOperator | Aggregate platform counts (orgs, users, locations, employees) |
| GET    | `/org/stats`      | Admin, Manager   | Org-scoped operational counts for current tenant              |

## Platform admin (`/api/v1/platform`) — PlatformOperator

| Method | Path                                                   | Description                                               |
| ------ | ------------------------------------------------------ | --------------------------------------------------------- |
| GET    | `/users?page=&pageSize=&organizationId=&role=&search=` | Paged platform user list, including org name when present |
| GET    | `/organizations?page=&pageSize=&search=`               | Paged org list with package status and counts             |
| PUT    | `/organizations/{id}/subscription`                     | Enable/disable or renew org package                       |

`PUT /platform/organizations/{id}/subscription` body:

```json
{
  "enabled": true,
  "durationDays": 90
}
```

`durationDays` is optional on the API (`1..3650`). Platform FE should send the value the Wokki admin chooses in settings (no fixed 30-day default in UI). When omitted, BE reuses the org’s stored `subscriptionDurationDays`. Enabling sets `subscriptionExpiresAt = now + durationDays`. Disabling makes the org unusable without deleting data.

Package gate codes for org users:

| HTTP | Code                        | Meaning                                              |
| ---- | --------------------------- | ---------------------------------------------------- |
| 403  | `ORG_PACKAGE_NOT_ACTIVATED` | Org package has never been activated or was disabled |
| 402  | `ORG_PACKAGE_EXPIRED`       | Org package expired; Wokki admin must renew          |

## Real-time

| Transport | Path                          | Auth      | Events                                                   |
| --------- | ----------------------------- | --------- | -------------------------------------------------------- |
| SignalR   | `/ws/chat?access_token={jwt}` | JWT query | `JoinChannel`, `LeaveChannel`, server → `ReceiveMessage` |

## Health

| Method | Path      | Auth      |
| ------ | --------- | --------- |
| GET    | `/health` | Anonymous |

## Response envelope

```json
{
  "success": true,
  "data": {},
  "message": {"code": "...", "text": "...", "statusCode": 200},
  "errors": null
}
```

Message codes live in `Wokki.Common.Utils.AppMessages`.
