# FE Handoff: Branch Workspace Scope

> Branch: `main`
> Date: 2026-05-29

## 1) Endpoint map

- `GET /api/v1/locations` — list locations; Admin gets all, Manager gets assigned branches only.
- `GET /api/v1/locations/available` — active branches for `/join`; any authenticated user.
- `GET /api/v1/departments?locationId=` — list departments; Manager is server-scoped to assigned branches.
- `GET /api/v1/employees?locationId=&departmentId=` — list employees; Manager sees only employees with Active branch membership in assigned branches.
- `GET /api/v1/schedules?departmentId=&weekStartDate=` — list schedules; Manager is server-scoped.
- `GET /api/v1/attendance?employeeId=&fromDate=&toDate=` — list attendance; Manager is server-scoped.
- `GET /api/v1/swap-requests?status=&departmentId=&weekStartDate=` — list swap requests; Manager is server-scoped.
- `GET /api/v1/overtime-requests?departmentId=&month=&year=` — list OT requests; Manager is server-scoped.
- `GET /api/v1/overtime-requests/pending?departmentId=` — pending OT requests; Manager is server-scoped.
- `POST /api/v1/channels` — create chat channel; Manager may include only employees in managed branches.
- `GET /api/v1/location-memberships/my` — current employee branch state for route guard.
- `POST /api/v1/location-memberships/request` — user requests to join a branch.
- `GET /api/v1/location-memberships/pending` — pending join requests; Admin all, Manager assigned branches only.
- `PATCH /api/v1/location-memberships/{id}/review` — approve/reject join request.
- `GET /api/v1/locations/{id}/memberships?status=` — memberships for one branch.
- `POST /api/v1/locations/{id}/managers` — Admin assigns Manager to a branch.
- `DELETE /api/v1/locations/{id}/managers/{userId}` — Admin removes Manager from a branch.
- `GET /api/v1/locations/{id}/managers` — Admin lists Managers of a branch.
- `GET /api/v1/managers/me/locations` — Manager lists own assigned branches.
- `POST /api/v1/location-memberships/transfer` — move employee to another branch; Admin or scoped Manager.
- `POST /api/v1/department-memberships/transfer` — place employee into a department; Admin or scoped Manager.

## 2) Contracts

All responses use the existing envelope:

```json
{
  "success": true,
  "data": {},
  "message": { "code": "CODE", "text": "Message", "statusCode": 200 },
  "errors": null
}
```

### Route guard membership

`GET /api/v1/location-memberships/my`

**Auth:** any authenticated user

**Response `data`:** `LocationMembershipResponse | null`

- `null` means the user has an Employee profile but has not requested/received branch membership yet.
- `404 LM_NO_EMPLOYEE` means the account has no linked Employee profile yet.

`LocationMembershipResponse`:

- `id` — string
- `locationId` — string
- `locationName` — string
- `employeeId` — string
- `employeeFirstName` / `employeeLastName` — string
- `status` — `Pending | Active | Rejected | Left | Transferred` or numeric enum `0..4`
- `requestedAt` — ISO datetime
- `reviewedById` — string/null
- `reviewedAt` — ISO datetime/null
- `note` — string/null

FE route decision:

| Case | FE route |
|------|----------|
| `status === Active` | render protected app |
| `data === null` | `/join` |
| `Pending`, `Rejected`, `Left`, `Transferred` | `/pending` |
| `404 LM_NO_EMPLOYEE` | `/pending` setup-required state |

### Join request

`GET /api/v1/locations/available`

**Auth:** any authenticated user

**Response `data`:** `LocationResponse[]`

- `id`, `name`, `address`, `timeZone`, `isActive`, `createdAt`

`POST /api/v1/location-memberships/request`

**Auth:** User with linked Employee profile

**Body:**

```json
{ "locationId": "guid" }
```

Validation:

- `locationId` required.
- Existing Pending/Active membership for the same branch returns `409 LM_DUPLICATE`.

**Response `data`:** `LocationMembershipResponse`

FE flow: submit from `/join`, then redirect to `/pending`.

### Review join requests

`GET /api/v1/location-memberships/pending`

**Auth:** Admin, Manager

**Response `data`:** `LocationMembershipResponse[]`

- Admin receives every pending request.
- Manager receives only pending requests for assigned branches.

`GET /api/v1/locations/{id}/memberships?status=Pending`

**Auth:** Admin, Manager of `{id}`

**Query params:**

| Param | Type | Default | Note |
|-------|------|---------|------|
| `status` | enum string | none | `Pending`, `Active`, `Rejected`, `Left`, `Transferred` |

`PATCH /api/v1/location-memberships/{id}/review`

**Auth:** Admin, Manager of the target branch

**Body:**

```json
{ "status": "Active", "note": "optional note" }
```

Validation:

- `status` must be `Active` or `Rejected`.
- `note` max 500 chars.
- Only Pending memberships can be reviewed.
- Approving fails if employee already has another Active branch membership.

### Manager assignment

`GET /api/v1/managers/me/locations`

**Auth:** Manager

**Response `data`:** `LocationResponse[]`

Use this for Manager workspace selector and React Flow scope.

`GET /api/v1/locations/{id}/managers`

**Auth:** Admin

**Response `data`:** `LocationManagerResponse[]`

- `id`
- `locationId`
- `locationName`
- `userId`
- `userEmail`
- `assignedById`
- `assignedAt`

`POST /api/v1/locations/{id}/managers`

**Auth:** Admin

**Body:**

```json
{ "userId": "guid" }
```

Validation:

- `userId` required.
- Duplicate assignment returns `409 LMG_ALREADY_ASSIGNED`.

`DELETE /api/v1/locations/{id}/managers/{userId}`

**Auth:** Admin

**Response `data`:** empty object/null.

### Scoped foundation lists

`GET /api/v1/locations`

**Auth:** Admin, Manager

**Response `data`:** `LocationResponse[]`

Server behavior changed: Manager receives assigned branches only. Do not assume this endpoint returns all branches for Manager.

`GET /api/v1/departments?locationId={id}`

**Auth:** Admin, Manager

**Response `data`:** `DepartmentResponse[]`

- `id`, `locationId`, `name`, `isActive`, `createdAt`

If `locationId` is omitted, Manager still receives only departments under assigned branches.

`GET /api/v1/employees?page=1&pageSize=20&locationId=&departmentId=&includeTerminated=false`

**Auth:** Admin, Manager

**Response `data`:** paged `EmployeeResponse[]`

- `id`, `userId`, `email`, `role`
- `firstName`, `lastName`, `phone`, `position`, `hourlyRate`
- `departmentId`, `departmentName`
- `locationId`, `locationName`
- `employedAt`, `terminatedAt`, `createdAt`

Server behavior changed: Manager list uses Active `LocationMembership`, not the Manager's own department membership.

### Workspace transfer / department placement

`POST /api/v1/location-memberships/transfer`

**Auth:** Admin, Manager with scope over the employee and target branch

**Body:**

```json
{ "employeeId": "guid", "toLocationId": "guid" }
```

**Response `data`:** `LocationMembershipResponse`

`POST /api/v1/department-memberships/transfer`

**Auth:** Admin, Manager with scope over the employee and target department

**Body:**

```json
{ "employeeId": "guid", "toDepartmentId": "guid" }
```

**Response `data`:**

```json
{ "employeeId": "guid", "toDepartmentId": "guid" }
```

FE should use this after a join request is approved to place the employee under the correct department.

### Scoped operational lists

The following list endpoints are now server-scoped for Manager even when FE omits location filters:

| Endpoint | Query params |
|----------|--------------|
| `GET /api/v1/schedules` | `page`, `pageSize`, `departmentId`, `weekStartDate` |
| `GET /api/v1/attendance` | `page`, `pageSize`, `employeeId`, `fromDate`, `toDate` |
| `GET /api/v1/swap-requests` | `page`, `pageSize`, `status`, `departmentId`, `weekStartDate` |
| `GET /api/v1/overtime-requests` | `departmentId`, `month`, `year`, `page`, `pageSize` |
| `GET /api/v1/overtime-requests/pending` | `departmentId`, `page`, `pageSize` |

FE can still send `locationId`/`departmentId` for UX filtering, but backend is the source of truth for Manager scope.

### Schedule / CP-SAT eligibility

Manual assignments and `apply-suggestions` now require:

- schedule is Draft
- employee has Active `LocationMembership` for the schedule location
- employee has active department membership for the schedule department
- no duplicate assignment
- no overlap inside existing DB rows or the apply batch

New error code for assignment/apply:

- `400 SCHEDULE_EMPLOYEE_WRONG_LOCATION` — employee does not have Active membership in the schedule location.

## 3) Error codes

| HTTP | code | message |
|------|------|---------|
| 401 | `AUTH_UNAUTHORIZED` | Unauthorized |
| 403 | `AUTH_FORBIDDEN` | Forbidden |
| 400 | `VALIDATION_FAILED` | Validation failed. |
| 400 | `VALIDATION_INVALID_PAGE` | page must be greater than or equal to 1. |
| 400 | `VALIDATION_INVALID_PAGE_SIZE` | pageSize must be between 1 and 100. |
| 404 | `LM_NO_EMPLOYEE` | No employee profile linked to this account. |
| 404 | `LM_LOCATION_NOT_FOUND` | Location not found. |
| 404 | `LM_NOT_FOUND` | Membership not found. |
| 403 | `LM_FORBIDDEN` | You are not authorized to manage memberships for this location. |
| 409 | `LM_DUPLICATE` | A pending or active membership for this location already exists. |
| 409 | `LM_ACTIVE_CONFLICT` | Employee already has an active membership at another location. |
| 409 | `LM_INVALID_STATUS` | Membership is not in Pending status and cannot be reviewed. |
| 404 | `LMG_LOCATION_NOT_FOUND` | Location not found. |
| 404 | `LMG_USER_NOT_FOUND` | User not found. |
| 404 | `LMG_NOT_FOUND` | Manager assignment not found. |
| 409 | `LMG_ALREADY_ASSIGNED` | User is already a manager of this location. |
| 403 | `WS_CANNOT_MODIFY_ADMIN` | Admin accounts cannot be modified. |
| 403 | `WS_TRANSFER_FORBIDDEN` | You are not authorized to manage this employee. |
| 409 | `WS_ALREADY_AT_LOCATION` | Employee already has an active membership at this location. |
| 409 | `WS_ALREADY_IN_DEPT` | Employee is already in this department. |
| 400 | `WS_EMPLOYEE_WRONG_LOCATION` | Employee must have an active membership in the target department's location before department transfer. |
| 400 | `SCHEDULE_EMPLOYEE_WRONG_LOCATION` | Employee does not have an active membership in this schedule location. |
| 400 | `SCHEDULE_EMPLOYEE_WRONG_DEPT` | Employee does not belong to this schedule department. |

## 4) FE notes

- Business hierarchy for visualization:

```text
Company / Workspace
└── Location
    ├── Manager assigned by LocationManager
    └── Department
        └── Employee
```

- Manager is branch-scoped, not department-scoped. There is no `DepartmentManager` model yet.
- Employee visibility and schedule eligibility are branch-first: Active `LocationMembership` first, then department membership.
- Recommended graph library for FE: install `@xyflow/react` for rendering and `dagre` for auto-layout.

```bash
npm install @xyflow/react dagre
```

- Import React Flow CSS once in the graph component or app CSS:

```ts
import "@xyflow/react/dist/style.css";
```

- Suggested React Flow node types:

| Type | Source | ID shape |
|------|--------|----------|
| `location` | `GET /locations` or `GET /managers/me/locations` | `location:{id}` |
| `manager` | `GET /locations/{id}/managers` | `manager:{locationId}:{userId}` |
| `department` | `GET /departments?locationId=` | `department:{id}` |
| `employee` | `GET /employees?locationId=&departmentId=` | `employee:{id}` |
| `pendingMembership` | `GET /location-memberships/pending` | `pending:{membershipId}` |

- Suggested graph edges:

```text
location:{locationId} -> manager:{locationId}:{userId}
location:{locationId} -> department:{departmentId}
department:{departmentId} -> employee:{employeeId}
location:{locationId} -> pending:{membershipId}
```

- Current workspace UI is branch-scoped: `/{orgId}/{locationId}/{role}/workspace` must render only the selected branch, its departments, employees, and managers. `/{orgId}/{role}/workspace` is a redirect/branch selection fallback. A true all-branch Admin graph must be a separate explicit org-level view.
- Operational modules (`schedule`, `payroll`, `employees`, `shifts`, `attendance`, `swap`, `chat`) must save/query data through the selected branch context. Admin has full scope inside the org; Manager scope is only the branches assigned by Org Admin through `LocationManager`.
- Manager graph should use server-scoped `GET /locations` or `GET /managers/me/locations`; do not use `/locations/available` for Manager workspace data because it is for join flow and returns all active branches.
- After approving a pending membership, show a second action: "Phân vào phòng ban" using `POST /department-memberships/transfer`.
- If FE needs a real "trưởng ban theo phòng ban", backend does not have that entity yet. Do not represent branch Managers as department heads unless product accepts that approximation.
- Membership enum may arrive as string or numeric enum. Normalize both:

```ts
const statusMap = ["Pending", "Active", "Rejected", "Left", "Transferred"] as const;
function normalizeStatus(status: unknown) {
  return typeof status === "number" ? statusMap[status] : status;
}
```

- CP-SAT and manual apply may now reject employees outside the schedule location. Map `SCHEDULE_EMPLOYEE_WRONG_LOCATION` to copy like: "Nhân viên chưa thuộc chi nhánh của lịch này."
