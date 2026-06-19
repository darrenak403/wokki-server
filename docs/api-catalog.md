# API Catalog

Base URL: `/api/v1` unless noted. Auth: **Bearer JWT** except `/health` and auth login/register.

Rate limits: **`Fixed`** (100/min) default; **`Clock`** (300/min) for attendance clock endpoints.

## Auth (`/api/v1/auth`)

| Method | Path               | Roles         | Description                                                                                                                              |
| ------ | ------------------ | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| POST   | `/login`           | Anonymous     | Issue tokens                                                                                                                             |
| POST   | `/register`        | Anonymous     | Self-serve org signup: `email`, `password`, `organizationName` → Org Admin + JWT with `organization_id`; org package starts NotActivated |
| POST   | `/register-employee` | Anonymous   | Self-serve employee signup: `email`, `password`, `firstName`, `lastName`, optional `phone` → `User` with **no** `organization_id`; JWT for org-less shell |
| POST   | `/refresh-token`   | Authenticated | Refresh JWT (org-less `User` allowed)                                                                                                    |
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
| Employees   | `/employees`   | Scoped list/read                              | Full  | Creates User + Employee together; same-org legacy User without Employee is linked. Manager sees employees with Active membership in assigned branches. Soft delete = terminate. `GET /` supports `search` (name, email, phone, position). List/detail includes employee self-service **payment profile** (bank + QR URL) for payroll transfer |
| Locations   | `/locations`   | Scoped read; `PUT /{id}` in assigned branches | Full  | Manager sees assigned locations only; may update branch metadata in scope (workspace drawer). |
| Scheduling  | `/scheduling`  | Authenticated org users | — | `GET /scheduling/rule-catalog` — platform rule catalog (enforced + categories). |
| Organization | `/org`        | Admin + Manager (read policy) | Full | `GET/PUT /org/scheduling-policy` — org-wide scheduling rules (PUT Admin only). |
| Departments | `/departments` | Scoped read; `PUT /{id}` in assigned branches | Full  | Manager sees departments in assigned locations only; may update department in scope.                                                                                 |

## Branch membership (`/api/v1/location-memberships`)

| Method | Path                          | Roles                              | Description                                                                                      |
| ------ | ----------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------------------ |
| GET    | `/my`                         | Authenticated (+ employee profile) | Current user's **Active** location membership (auto-provisioned when Org Admin creates employee) |
| GET    | `/locations/{id}/memberships` | Admin, Manager                     | List memberships for a branch; Manager must manage that branch                                   |

**Removed (2026-05-29):** location-level self-serve join — `POST /location-memberships/request`, `GET /pending`, `PATCH /{id}/review`. Replaced by **org-level** join requests below (2026-06-01).

## Organizations — directory (`/api/v1/organizations`)

| Method | Path         | Roles                         | Description                                                                 |
| ------ | ------------ | ----------------------------- | --------------------------------------------------------------------------- |
| GET    | `/directory` | `User` without `organization_id` | Paged list of orgs with **active package** (`id`, `name` only); query `search` |

## Org join requests (`/api/v1/org-join-requests`)

Parallel to `POST /employees` for admin-driven onboarding. **User** role only; one global **Pending** request per user.

| Method | Path                  | Roles   | Description                                                                 |
| ------ | --------------------- | ------- | --------------------------------------------------------------------------- |
| POST   | `/`                   | Org-less `User` | Submit `{ organizationId }` → 409 `ORG_JOIN_PENDING_EXISTS` if pending exists |
| GET    | `/me`                 | Org-less `User` | Latest request (Pending / Rejected / Expired / Cancelled) + org name        |
| DELETE | `/me`                 | Org-less `User` | Cancel own Pending request                                                  |
| GET    | `/pending`            | Admin   | Pending requests for caller's org (lazy-expire if org lost package)         |
| PATCH  | `/{id}/approve`       | Admin   | `{ departmentId, hourlyRate, phone? }` → sets `User.OrganizationId`, creates `Employee` + Active `LocationMembership` |
| PATCH  | `/{id}/reject`        | Admin   | `{ note? }` (max 500 chars)                                                 |

## Workspace transfer (`/api/v1/workspace`)

| Method | Path                   | Roles          | Description                                                                                                                                                                |
| ------ | ---------------------- | -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| POST   | `/location/transfer`   | Admin, Manager | Move an employee to another branch within caller scope.                                                                                                                    |
| POST   | `/department/transfer` | Admin, Manager | Move an employee to a department in the employee's current Active branch. Cross-branch department placement returns `WS_EMPLOYEE_WRONG_LOCATION`; transfer location first. |

## Scheduling

| Method              | Path                                         | Roles          | Description                                                            |
| ------------------- | -------------------------------------------- | -------------- | ---------------------------------------------------------------------- |
| GET/POST            | `/schedules`                                 | Admin, Manager | List / create (Draft)                                                  |
| GET/PUT/DELETE      | `/schedules/{id}`                            | Admin, Manager | Detail (includes `rebalanceHints`: conflicts, pending leave, preference changes) / update / delete Draft |
| POST                | `/schedules/{id}/publish`                    | Admin, Manager | Draft → Published                                                      |
| POST                | `/schedules/{id}/unpublish`                  | Admin, Manager | Published → Draft                                                      |
| POST                | `/schedules/{id}/copy`                       | Admin, Manager | Copy week to new Draft                                                 |
| GET/POST            | `/schedules/{id}/assignments`                | Admin, Manager | List / add assignment                                                  |
| DELETE              | `/schedules/{id}/assignments/{assignmentId}` | Admin, Manager | Remove assignment                                                      |
| POST                | `/schedules/{id}/suggest`                    | Admin, Manager | CP-SAT suggestions (read-only; `useAi` ignored; auto-refreshes insight context snapshot; Bedrock not used) |
| POST                | `/schedules/{id}/apply-suggestions`          | Admin, Manager | Apply suggestions (Draft only; upsert by `(shiftDefinitionId, employeeId, date)`) |
| GET                 | `/schedules/{id}/preference-board`           | Admin, Manager | Read-only grid: all dept employees × shifts × days with preference cells; includes `submittedCount` / `employeeCount` |
| POST                | `/schedules/{id}/insights/context`           | Admin, Manager | Generate/refresh JSON context snapshot for schedule insight            |
| GET                 | `/schedules/{id}/insights/context`           | Admin, Manager | Read latest context snapshot                                           |
| POST                | `/schedules/{id}/insights/chat`              | Admin, Manager | Ask optional Bedrock assistant about the context; no schedule mutation |
| GET/POST/PUT/DELETE | `/shifts`                                    | Admin, Manager | Shift definitions                                                      |
| POST                | `/shifts/copy`                               | Admin, Manager | Copy active shifts from source department to target departments (same location; skips duplicates by name+time) |

## Schedule leave requests (`/api/v1/leave-requests`) — Admin, Manager

Draft schedules only. Approve upserts preference Unavailable and removes conflicting assignment.

| Method | Path | Description |
| ------ | ---- | ----------- |
| GET | `/leave-requests?scheduleId=&status=` | List leave requests for a schedule |
| POST | `/leave-requests/{id}/approve` | Approve leave request |
| POST | `/leave-requests/{id}/reject` | Reject leave request |

## Employee self-service (`/api/v1/self`) — User (+ employee profile)

Not the same as `GET /api/v1/auth/me` (login account). These routes require a linked Employee profile.

| Method | Path                  | Description                                  |
| ------ | --------------------- | -------------------------------------------- |
| GET    | `/self/schedule`      | Own upcoming published assignments (28 days) |
| GET    | `/self/schedule-preferences/week/{weekStartDate}` | Draft schedule + shifts for employee's dept/week (null if no schedule) |
| GET    | `/self/schedule-preferences/{scheduleId}` | Own preference submission (Draft/Submitted) |
| PUT    | `/self/schedule-preferences/{scheduleId}` | Save preference lines (Draft schedule only) |
| POST   | `/self/schedule-preferences/{scheduleId}/submit` | Submit preferences to Admin board (Draft schedule only) |
| POST   | `/self/leave-requests` | Submit draft-week leave request (shift + date + reason) |
| GET    | `/self/leave-requests` | List own leave requests (`?scheduleId=` optional) |
| DELETE | `/self/leave-requests/{id}` | Cancel pending leave request |
| GET    | `/self/schedule/draft/{weekStartDate}/assignments` | Own Draft-week assignments (swap create/accept picker) |
| GET    | `/self/swap-posts/feed` | Swap marketplace feed (Draft schedule; `?scheduleId=`) |
| GET    | `/self/swap-posts/mine` | Own swap posts (`?scheduleId=`, `?status=`) |
| GET    | `/self/attendance`    | Own attendance history                       |
| GET    | `/self/profile`       | Own employee profile (name, phone, org context) |
| PUT    | `/self/profile`       | Update own profile: name, phone, bank account fields; optional `removePaymentQr` |
| POST   | `/self/profile/payment-qr` | Upload payment QR image (multipart `file`, Cloudinary, max 5MB) |

## Swap marketplace (`/api/v1/swap-posts`)

| Method | Path | Roles | Description |
| ------ | ---- | ----- | ----------- |
| GET | `/feed` | User | Pending posts for department (`?scheduleId=`) |
| GET | `/mine` | User | Own posts |
| POST | `/` | User | Create Cover or CrossSwap post |
| GET | `/{id}` | Authenticated | Detail |
| POST | `/{id}/accept` | User | FCFS accept (CrossSwap: body `acceptorAssignmentId`) |
| POST | `/{id}/accept/preview` | User | Dry-run policy validation |
| POST | `/{id}/cancel` | User (author) | Cancel Pending post |
| GET | `/audit` | Admin, Manager | Completed swap log |

## Attendance (`/api/v1/attendance`)

| Method | Path           | Roles          | Rate  | Description                |
| ------ | -------------- | -------------- | ----- | -------------------------- |
| POST   | `/clock-in`    | User           | Clock | Start record               |
| POST   | `/clock-out`   | User           | Clock | End + minutes              |
| GET    | `/`            | Admin, Manager | Fixed | List/filter                |
| PUT    | `/{id}/adjust` | Admin, Manager | Fixed | Manual adjust + audit note |
| GET    | `/summary`     | Admin, Manager | Fixed | Daily count summary by location + date (clockedIn, total assigned) |

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
| GET    | `/`                      | Authenticated (Employee)   | Org channel + Direct DMs only  |
| GET    | `/unread-count`          | Authenticated (Employee)   | Unread message count per channel (self only) |
| GET    | `/org/members`           | Authenticated (Employee)   | Active org employees for DM    |
| POST   | `/`                      | Authenticated (Employee)   | Create **Direct** only (403 Group) |
| GET    | `/{id}/messages`         | Member (Admin bypass read) | Cursor: `?before=&limit=`      |
| POST   | `/{id}/messages`         | Member                     | Send + SignalR push            |
| DELETE | `/{id}/messages/{msgId}` | Sender or Admin            | Soft delete                    |

## Stats

| Method | Path                    | Roles            | Description                                                   |
| ------ | ----------------------- | ---------------- | ------------------------------------------------------------- |
| GET    | `/platform/stats`       | PlatformOperator | Aggregate platform counts (orgs, users, locations, employees) |
| GET    | `/org/stats`            | Admin, Manager   | Org-scoped operational counts for current tenant              |
| GET    | `/org/usage-analytics`  | Admin            | Activity trends by event type (login, schedule, attendance, chat) over configurable window |

## Platform admin (`/api/v1/platform`) — PlatformOperator

| Method | Path                                                                                                          | Description                                               |
| ------ | ------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------- |
| GET    | `/stats`                                                                                                      | Aggregate platform counts                                 |
| GET    | `/health`                                                                                                     | Platform-only API, Bedrock, and email diagnostics         |
| GET    | `/usage-analytics?windowDays=&organizationId=`                                                               | Active org metrics and usage signals                      |
| GET    | `/users?page=&pageSize=&organizationId=&role=&search=`                                                        | Paged platform user list, including org name when present |
| GET    | `/organizations?page=&pageSize=&search=&status=&sortBy=&sortDirection=&expiringWithinDays=`                  | Paged org registry with package status, filters, counts   |
| PUT    | `/organizations/{id}/subscription`                                                                            | Enable/disable or renew org package                       |
| GET    | `/subscription-ledger?page=&pageSize=&organizationId=&action=&from=&to=`                                      | Global immutable subscription history                     |
| GET    | `/organizations/{id}/subscription-ledger?page=&pageSize=&action=&from=&to=`                                  | Immutable subscription history for one org                |
| GET    | `/support/search?query=&page=&pageSize=`                                                                     | Search by org id, org name, or user email                 |
| GET    | `/support/organizations/{id}/context`                                                                         | Read-only org support context                             |

`GET /platform/organizations` query notes:

| Query | Values |
| ----- | ------ |
| `status` | `NotActivated`, `Active`, `Expired`, `Disabled` |
| `sortBy` | `createdAt`, `name`, `expiryDate` |
| `sortDirection` | `asc`, `desc` |
| `expiringWithinDays` | Defaults to `7`; controls `isExpiringSoon` |

Org registry response items include `daysUntilExpiry`, `isExpiringSoon`, `userCount`, `locationCount`, and `employeeCount`.

`PUT /platform/organizations/{id}/subscription` body:

```json
{
  "enabled": true,
  "durationDays": 90
}
```

`durationDays` is optional on the API (`1..3650`). Platform FE should send the value the Wokki admin chooses in settings (no fixed 30-day default in UI). When omitted, BE reuses the org’s stored `subscriptionDurationDays`. Enabling sets `subscriptionExpiresAt = now + durationDays`. Disabling makes the org unusable without deleting data.

Every subscription update writes an immutable subscription ledger entry and an `AuditLog` entry in the same transaction. Ledger entries include `organizationId`, `action`, previous/new status, previous/new duration days, previous/new expiry, `changedByUserId`, and `changedAt`. Ledger history does not include revenue amount, invoices, or payment data.

Support Console responses are read-only operational metadata. They may include linked org/user metadata, package status, counts, latest operational timestamps, and latest subscription ledger entry; they must not expose tenant business row contents or enable impersonation.

Usage analytics defines an active org as one with at least one tracked activity event in the window: successful login, schedule publish, schedule suggest/apply, attendance clock-in/out, or chat message. `windowDays` defaults to 7 and supports 30-day queries.

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
