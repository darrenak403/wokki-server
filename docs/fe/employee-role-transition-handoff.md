# FE handoff: Employee ↔ Manager role transition

**API:** `POST /api/v1/employees/{employeeId}/role-transition`  
**Auth:** Org Admin (`RoleConstants.Admin`) only.

## Request

```json
{
  "targetRole": "Manager",
  "locationId": "optional-guid",
  "departmentId": "optional-guid",
  "hourlyRate": 35000
}
```

| Field | Promote (`Manager`) | Demote (`User`) |
|-------|---------------------|-----------------|
| `targetRole` | `"Manager"` | `"User"` |
| `locationId` | Optional; default = branch of employee's current `departmentId` | — |
| `departmentId` | — | **Required** |
| `hourlyRate` | — | Optional; omit to keep existing rate |

## Response

`200` → `ApiResponse<EmployeeResponse>` (same shape as `GET /employees/{id}`).

## Error codes

| Code | HTTP | Meaning |
|------|------|---------|
| `EMPLOYEE_NOT_FOUND` | 404 | Unknown / wrong org / terminated |
| `EMPLOYEE_INVALID_ROLE_TRANSITION` | 400 | Wrong current role for intent |
| `EMPLOYEE_DEPARTMENT_OR_LOCATION_REQUIRED` | 400 | Promote without department or `locationId` |
| `EMPLOYEE_DEPARTMENT_REQUIRED` | 400 | Demote without `departmentId` |
| `EMPLOYEE_DEPARTMENT_NOT_FOUND` | 404 | Invalid department |
| `EMPLOYEE_MANAGER_LOCATION_NOT_FOUND` | 404 | Invalid `locationId` |
| `EMPLOYEE_ALREADY_MANAGER_AT_LOCATION` | 409 | Already `LocationManager` at branch |
| `EMPLOYEE_ALREADY_USER_IN_DEPARTMENT` | 409 | Already User in that department |
| `WS_ROLE_TRANSITION_USE_EMPLOYEE_ENDPOINT` | 400 | Do not use `PATCH /users/{id}/role` |

After success, user must **re-login** (refresh token revoked).

## UI mapping (org graph)

| Gesture | Body |
|---------|------|
| Drag **employee** (User) onto **location** node | `{ "targetRole": "Manager", "locationId": "<location node id>" }` |
| Drag **manager** node onto **department** node | Opens demote confirm dialog (chi nhánh + phòng ban, type `yes`) → `{ "targetRole": "User", "departmentId": "<dept id>" }`; use `LocationManagerResponse.employeeId` |
| Profile tab **Vai trò & phạm vi** (demote) | Same demote dialog — chọn chi nhánh + phòng ban thủ công trước khi xác nhận |
| Drag **employee** (User) onto **department** (same branch) | Keep `POST /department-memberships/transfer` |
| Employee form / profile | Same `role-transition` API |

## Manual QA matrix

| # | Steps | Expected |
|---|--------|----------|
| 1 | User in dept A → promote (no `locationId`) | Manager + `LocationManager` at A's branch; no `departmentId` |
| 2 | User → promote with explicit other `locationId` | Manager at target branch |
| 3 | Manager → demote with `departmentId` | User + dept membership; no `LocationManager` |
| 4 | Manager token calls transition | 403 |
| 5 | `PATCH /users/{id}/role` | 400 `WS_ROLE_TRANSITION_USE_EMPLOYEE_ENDPOINT` |
| 6 | After transition, refresh with old token | 401 |
