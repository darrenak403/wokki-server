# API Catalog

Base URL: `/api/v1` unless noted. Auth: **Bearer JWT** except `/health` and auth login/register.

Rate limits: **`Fixed`** (100/min) default; **`Clock`** (300/min) for attendance clock endpoints.

## Auth (`/api/v1/auth`)

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| POST | `/login` | Anonymous | Issue tokens |
| POST | `/register` | Anonymous | Self-signup (always `User` role; admin creates users via `POST /users`) |
| POST | `/refresh-token` | Anonymous | Refresh JWT |
| GET | `/me` | Authenticated | Current user |
| POST | `/logout` | Authenticated | Logout |
| PUT | `/change-password` | Authenticated | Change password |
| POST | `/forgot-password` | Anonymous | Forgot password |
| POST | `/reset-password` | Anonymous | Reset password |

## Users (`/api/v1/users`) — Admin patterns

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| GET | `/` | Admin | Paged users |
| GET | `/{id}` | Admin | User by id |
| POST | `/` | Admin | Create user |

## Foundation

| Resource | Base | Manager | Admin | Notes |
|----------|------|---------|-------|-------|
| Employees | `/employees` | R/W list | Full | Soft delete = terminate |
| Locations | `/locations` | R/W | Full | |
| Departments | `/departments` | R/W | Full | |

## Scheduling

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| GET/POST | `/schedules` | Admin, Manager | List / create (Draft) |
| GET/PUT/DELETE | `/schedules/{id}` | Admin, Manager | Detail / update / delete Draft |
| POST | `/schedules/{id}/publish` | Admin, Manager | Draft → Published |
| POST | `/schedules/{id}/unpublish` | Admin, Manager | Published → Draft |
| POST | `/schedules/{id}/copy` | Admin, Manager | Copy week to new Draft |
| GET/POST | `/schedules/{id}/assignments` | Admin, Manager | List / add assignment |
| DELETE | `/schedules/{id}/assignments/{assignmentId}` | Admin, Manager | Remove assignment |
| POST | `/schedules/{id}/suggest` | Admin, Manager | Heuristic suggestions (no write) |
| POST | `/schedules/{id}/apply-suggestions` | Admin, Manager | Apply suggestions (Draft only) |
| GET/POST/PUT/DELETE | `/shifts` | Admin, Manager | Shift definitions |

## Employee self-service (`/api/v1/self`) — User (+ employee profile)

Not the same as `GET /api/v1/auth/me` (login account). These routes require a linked Employee profile.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/self/schedule` | Own upcoming published assignments (28 days) |
| GET | `/self/swap-requests` | Swap requests sent/received |
| GET | `/self/attendance` | Own attendance history |

## Swap requests (`/api/v1/swap-requests`)

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| POST | `/` | User | Create swap |
| GET | `/` | Admin, Manager | List (filters) |
| GET | `/{id}` | Authenticated | Detail (access rules) |
| POST | `/{id}/accept` | User (target) | Accept + auto-apply |
| POST | `/{id}/decline` | User (target) | Decline |
| POST | `/{id}/cancel` | User (requester) | Cancel |
| POST | `/{id}/override-approve` | Admin, Manager | Manager override |
| POST | `/{id}/override-reject` | Admin, Manager | Manager override |

## Attendance (`/api/v1/attendance`)

| Method | Path | Roles | Rate | Description |
|--------|------|-------|------|-------------|
| POST | `/clock-in` | User | Clock | Start record |
| POST | `/clock-out` | User | Clock | End + minutes |
| GET | `/` | Admin, Manager | Fixed | List/filter |
| PUT | `/{id}/adjust` | Admin, Manager | Fixed | Manual adjust + audit note |

## Payroll (`/api/v1/payroll`)

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| GET | `/summary` | Admin, Manager | Department pay period summary |
| GET | `/summary/{employeeId}` | Admin, Manager | Employee breakdown |
| POST | `/summary/export` | Admin | CSV file download |

Query/body: `departmentId`, `startDate`, `endDate` (`PayrollPeriodRequest`).

## Chat (`/api/v1/channels`)

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| GET | `/` | Authenticated | Channels for caller's employee |
| POST | `/` | Admin, Manager | Create Direct/Group |
| GET | `/{id}/messages` | Member (Admin bypass read) | Cursor: `?before=&limit=` |
| POST | `/{id}/messages` | Member | Send + SignalR push |
| DELETE | `/{id}/messages/{msgId}` | Sender or Admin | Soft delete |

## Real-time

| Transport | Path | Auth | Events |
|-----------|------|------|--------|
| SignalR | `/ws/chat?access_token={jwt}` | JWT query | `JoinChannel`, `LeaveChannel`, server → `ReceiveMessage` |

## Health

| Method | Path | Auth |
|--------|------|------|
| GET | `/health` | Anonymous |

## Response envelope

```json
{
  "success": true,
  "data": { },
  "message": { "code": "...", "text": "...", "statusCode": 200 },
  "errors": null
}
```

Message codes live in `Wokki.Common.Utils.AppMessages`.
