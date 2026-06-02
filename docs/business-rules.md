# Business Rules (Locked)

Rules are numbered **`BR-xxx`**. Services and APIs must enforce them. When a rule conflicts with a convenience shortcut, **the rule wins**.

Cross-reference: [process-flows.md](./process-flows.md), [api-catalog.md](./api-catalog.md).

---

## Identity & access

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-001 | Roles are fixed: `Admin`, `Manager`, `User`, and platform-only `PlatformOperator`. No dynamic permission matrix in MVP. | JWT claims + endpoint `RequireRole` |
| BR-002 | `User` may not access manager schedule APIs (`/api/v1/schedules/*` except via published data indirectly). Own schedule only via `/api/v1/self/schedule`. | Route authorization |
| BR-003 | `Admin` may manage users, payroll export, and soft-delete any chat message. | `ChannelService`, `PayrollEndpoints` |
| BR-004 | `Manager` may manage schedules, assignments, swap audit, attendance adjust, and open **direct messages** with org members (same rules as other roles). | Route authorization |
| BR-005 | Every employee-facing action requires an `Employee` row linked to the authenticated `User`. | Services return `*_NO_EMPLOYEE` / 404 |
| BR-006 | `Admin` has full branch scope **within their organization** and may list/manage all `Location` workspaces in that org. | `LocationScopeService`, `IOrganizationScopeService` |
| BR-007 | `Manager` scope is only the locations assigned through `LocationManager`. Do not infer Manager access from role alone, user global role, or the Manager's own employee/department memberships. | `LocationScopeService`, scoped list queries |
| BR-008 | Org Admin creates staff through `POST /api/v1/employees`; that single workflow creates both `User` account and `Employee` profile. **`User` role requires `DepartmentId`** (current branch + department) and auto-provisions **Active** `LocationMembership` at that department's location. **`Manager` role requires `LocationIds`** (at least one branch) — the system creates `LocationManager` rows at create time; Manager does not need a department. On login, **User** and **Manager** see only branches they have access to (`LocationMembership` / `LocationManager`); unassigned branches are hidden. If a same-org legacy `User` exists without `Employee`, this workflow links it into an employee profile. Org staff accounts must not be created as standalone `/users` rows without `Employee`. After onboarding, employees log in and enter `/app` directly. | `EmployeeService.CreateAsync`, `EmployeeService.AssignManagerLocationsAsync`, `EmployeeService.EnsureActiveLocationMembershipAsync` |
| BR-020 | **Employee self-onboard (parallel to BR-008):** Prospective staff use `POST /auth/register-employee` → `User` with `Role=User`, `OrganizationId=null`, profile on `users` (firstName, lastName, optional phone). They browse `GET /organizations/directory` (orgs with active package only) and submit **one** global Pending `OrgJoinRequest` via `POST /org-join-requests`. Org Admin approves with `departmentId` + `hourlyRate` (same provisioning as User create) or rejects. **Manager** cannot self-onboard. No concurrent pending requests; cancel via `DELETE /org-join-requests/me`. Request auto-**Expired** if org loses active package before review. Re-request immediately after Reject. Org-less users may only call auth, directory, and org-join APIs until approved. | `AuthService.RegisterEmployeeAsync`, `OrgJoinRequestService`, `IEmployeeProvisioner`, `OrganizationContextMiddleware` |
| BR-009 | Branch changes for an existing employee use Admin/Manager workspace transfer (`POST /api/v1/workspace/location/transfer`), not employee join requests. Department placement uses `POST /api/v1/workspace/department/transfer` and must target a department in the employee's active branch; cross-branch placement requires location transfer first. | `WorkspaceService.TransferLocationAsync`, `WorkspaceService.TransferDepartmentAsync` |

---

## Organization & master data

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-010 | A `Department` belongs to exactly one `Location`. | EF FK |
| BR-011 | Employees must have Active `LocationMembership` for the schedule's location and Active department membership for the schedule department before assignment. `Employee.DepartmentId` is the current primary department. Department transfers are branch-local: the target department's `LocationId` must match the employee's Active `LocationMembership`. Transfers close the active row (`Status=Transferred`, `LeftAt` set) and append a new row when the employee returns to a department — history rows are never overwritten. Tenure is read from `JoinedAt`/`LeftAt` per `employee_department_memberships` row. | `TryPrepareAssignmentAsync`, `WorkspaceService.TransferDepartmentAsync`, `GET /api/v1/employees/{id}/department-memberships` |
| BR-012 | Terminated employees (`TerminatedAt` set) cannot receive new assignments or be swap targets. | `EmployeeService`, assignment validators |
| BR-013 | `ShiftDefinition` must match schedule scope: same `LocationId`; if `DepartmentId` set, must equal schedule department. | `TryPrepareAssignmentAsync` |
| BR-014 | Tenant root is `Organization`. Business data carries `OrganizationId`; org users must have JWT claim `organization_id`. Never accept `organizationId` from request body for authorization. | `OrganizationContextMiddleware`, `IOrganizationScopeService` |
| BR-015 | `PlatformOperator` (`admin@gmail.com` seed) has `OrganizationId = null`, may use platform routes (`/api/v1/platform/*`) only — not org business routes. | `StatsService`, `PlatformAdminService`, route auth |
| BR-016 | `POST /register` atomically creates `Organization` + Org Admin (`Admin` role) + **linked `Employee` profile** (org channel member) + JWT. Org Admin **`Employee` has no `DepartmentId`** — org-wide scope over all branches; chat/UI show **Quản trị viên · Toàn chi nhánh**, not a branch department. **Never downgrade org creator / Admin via `POST /employees`**; chat member list auto-repairs demoted org creator (role → Admin, clears department). One email = one org; duplicate email → 409. New orgs start without an activated package until Wokki admin enables them. Legacy org Admins without Employee are auto-provisioned on first chat/self-profile access. | `AuthService.RegisterAsync`, `OrgAdminEmployeeProvisioner`, `ChannelService.ListOrgMembersAsync` |
| BR-017 | Branch transfer validates `location.OrganizationId == employee.organizationId`. Cross-tenant location access returns 404. | `WorkspaceService`, `EmployeeService` |
| BR-018 | Org stats (`GET /api/v1/org/stats`) for `Admin` + `Manager` only; platform stats for `PlatformOperator` only. | `StatsService`, endpoint auth |
| BR-019 | Wokki admin (`PlatformOperator`) may list platform users/orgs and enable, disable, or renew an org package. Package length is set via `durationDays` in the platform console (admin-chosen; FE must not assume a fixed default such as 30 days). On renew, omitted `durationDays` reuses the org’s stored `subscriptionDurationDays`. Org users cannot log in, refresh, or call org APIs when the package is not activated (`ORG_PACKAGE_NOT_ACTIVATED`) or expired (`ORG_PACKAGE_EXPIRED`). | `PlatformAdminService`, `OrganizationSubscriptionService`, `OrganizationContextMiddleware`, `AuthService` |
| BR-090 | Every platform subscription update must create an immutable `OrganizationSubscriptionLedgerEntry` and an `AuditLog` entry in the same transaction as the org package mutation. Ledger writes are mandatory; if the ledger/audit write fails, the subscription update fails. Ledger stores package history only (state, duration, expiry, operator, timestamp), not revenue amount, invoice, or payment data. | `PlatformAdminService`, `OrganizationSubscriptionLedgerRepository`, `AuditLog` |
| BR-091 | Platform Support Console phase 1 is read-only for tenant business data. It may search org id, org name, or user email and return package status, counts, latest operational timestamps, and latest subscription ledger entry, but it must not impersonate users, expose tenant row contents, edit schedules/payroll/attendance/chat, or perform destructive tenant actions. Existing platform-scope subscription actions remain the only support write path. | `PlatformAdminService`, `OrganizationRepository.SearchPlatformSupportAsync`, `GetPlatformSupportContextAsync` |
| BR-092 | Platform usage analytics defines an active org as an organization with at least one tracked `PlatformActivityEvent` in the selected window: successful login, schedule publish, schedule suggest, schedule apply-suggestions, attendance clock-in, attendance clock-out, or chat message. Activity recording is best-effort, stores no request/body payloads, and must not roll back the core org workflow. | `PlatformActivityRecorder`, `PlatformUsageAnalyticsService` |
| BR-093 | Platform diagnostics are `PlatformOperator`-only and separate from public `/health`. Platform health may expose component status for API, Bedrock, and email configuration/failure state, but must not expose SMTP recipients, credentials, tokens, or tenant data. Dependency failures degrade the platform health response instead of taking down the whole endpoint. | `PlatformDiagnosticsService`, `PlatformDiagnosticState`, `EmailDiagnosticService` |

---

## Scheduling

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-020 | `WeekStartDate` must be a **Monday** (`ScheduleRules.IsMonday`). | Create/update schedule |
| BR-021 | At most one schedule per `(DepartmentId, WeekStartDate)`. | Unique constraint + service guard |
| BR-022 | Schedule lifecycle in MVP: **`Draft` → `Published`** (unpublish reverts to `Draft`). Enum `Locked` exists but is **not** used by publish APIs yet. | `ScheduleService` |
| BR-023 | Assignments may be created/updated/deleted only when schedule is **`Draft`**. | `CreateAssignmentAsync`, delete assignment |
| BR-024 | One employee cannot have **overlapping** shift times on the same date within one schedule. | `HasTimeOverlapAsync` |
| BR-025 | Duplicate tuple `(schedule, shiftDefinition, employee, date)` is rejected. | `ExistsAsync` |
| BR-026 | On publish, assigned employees receive notification `schedule.published` (non-blocking). Email subject and body are **Vietnamese**: week range in subject; body lists each assigned day as **Thứ …, dd/MM/yyyy** with shift name and time (not raw JSON). | `ScheduleService.PublishAsync`, `NotificationEmailComposer` |
| BR-027 | `GET /api/v1/self/schedule` returns only the caller's assignments for the next **28 days** on **published** schedules. | `GetMyScheduleAsync` |
| BR-028 | Schedule preferences are **advisory only**. They are separate from official `ShiftAssignment` rows; Admin/Manager may change Draft assignments after preferences are copied/submitted, and the published assignments are the final work schedule. | `SchedulePreferenceService`, `ScheduleService` |
| BR-029 | Employees may save/update their own schedule preferences while the schedule is **Draft**. After the schedule is **Published**, preferences remain viewable but are read-only. | `SaveMineAsync`, `SubmitMineAsync`, self preference APIs |
| BR-080 | **Main flow (F&B SMB):** Admin creates Draft schedule → employees submit preferences (`/self/schedule-preferences/*`) → Admin views `GET /schedules/{id}/preference-board` → suggest/assign → publish. Preferences never auto-create assignments. Web clients must normalize `ScheduleStatus` from API (BE may serialize enum as string `"Draft"`/`"Published"`). | `SchedulePanel`, `SchedulePreferenceService`, FE `normalizeScheduleStatus` |

---

## Shift swap marketplace (Draft only)

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-030 | Swap marketplace operates only while the schedule is **`Draft`**. Published schedules lock all swap activity. | `SwapPostService`, `ScheduleService.PublishAsync` |
| BR-031 | On **publish**, all `Pending` swap posts become **`Hidden`** (retained in DB, not shown on feed). | `HidePendingByScheduleAsync` in publish transaction |
| BR-032 | Feed scope: same **location** + **department**; employees outside scope cannot view or accept. | `ListFeedAsync`, scope validators |
| BR-033 | Post types: **`Cover`** (author gives up a shift; accepter takes it only if they have an **open schedule slot** — no acceptor assignment) and **`CrossSwap`** (accepter selects **their** assignment to exchange with the author's). | `SwapPostType`, `AcceptAsync` |
| BR-034 | **FCFS** open accept — first valid accept wins; no admin approval. | Row lock + `SWAP_POST_ALREADY_TAKEN` |
| BR-035 | Accept validates enabled org policy: role match, overlap, rest, max shifts/day/week. | `SwapPostPolicyValidator` |
| BR-036 | **Admin/Manager** read audit log only; they do not create or accept marketplace posts. | Role guards on endpoints |
| BR-037 | Notifications (`swap_post.completed`, `schedule.published`) must **not** roll back the core transaction if delivery fails. | try/catch around `INotificationService` |

### Swap post status (marketplace)

| Status | Meaning |
|--------|---------|
| `Pending` | Visible on feed, awaiting accept |
| `Completed` | Accept applied to Draft assignments |
| `Hidden` | Schedule published while still pending |
| `Cancelled` | Author cancelled before accept |
| `Expired` | Author no longer owns offered assignment |

Legacy table `swap_requests` is retained read-only for historical rows; API `/api/v1/swap-requests` removed.

---

## Attendance

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-040 | Clock-in only if employee has **no** open record (`ClockOut IS NULL`). | Partial unique index + service |
| BR-041 | Clock-in requires a **published assignment** for today. | `AttendanceService` |
| BR-042 | Clock-out requires an open record; sets `WorkedMinutes` (rounded minute). | `AttendanceService` |
| BR-043 | Manual adjust: `Admin`/`Manager` only; **adjustment note required**. | `AdjustAsync` |
| BR-044 | Adjust blocked if a **locked** pay period contains the record's clock-in date. | `IsPayPeriodLockedForRecordAsync` |
| BR-045 | Clock endpoints use higher rate limit policy (`Clock`, 300/min). | `AttendanceEndpoints` |

---

## Payroll

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-050 | Summary aggregates eligible `WorkedMinutes` in period × `HourlyRate`; OT flat same rate. | `PayrollCalculationService` |
| BR-051 | Pay period auto-created on first summary request if no overlap. | `PayPeriodRepository` |
| BR-052 | When period is **`Locked`** and snapshot lines exist, summary reads **`PayrollLine`** not live attendance. | `PayrollService` |
| BR-053 | `HourlyRate` on `PayrollLine` is a **snapshot** at lock time. | `LockPeriodAsync` |
| BR-054 | CSV export max **500** rows; Admin only. | `ExportCsvAsync` |
| BR-055 | Only assignment attendance with `ClockOut` counts toward payroll. | `SumWorkedMinutesByEmployeeAsync` |
| BR-056 | OT pay = approved OT minutes × same `HourlyRate` (flat). | `PayrollCalculationService` |
| BR-057 | Lock creates immutable `PayrollLine` snapshot per employee. | `POST /payroll/periods/{id}/lock` |
| BR-058 | Primary pay period UX = **calendar month** per department. | FE + `PayrollPeriodRequest` |
| BR-059 | Paid flag on locked `PayrollLine`; Admin only. | `SetLinePaidAsync` |

---

## Chat

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-060 | Only **channel members** may list/send messages; non-member → **403**. | `ChannelService` |
| BR-061 | `Admin` may read any channel history without membership. | `ResolveMemberAccessAsync` |
| BR-062 | Direct channel: exactly **two** members; reuse existing DM if present. | `CreateAsync` + `FindDirectChannelAsync` |
| BR-063 | Custom **Group** channel creation is **disabled** (`403`); legacy Group rows may remain in DB but are **hidden** from `ListMineAsync`. | `ChannelService.CreateAsync`, `ListMineAsync` |
| BR-064 | Message body: plain text, max **4000** chars; HTML tags stripped. | `SanitizeBody` |
| BR-065 | Soft-delete sets `DeletedAt` and body placeholder `[Message deleted]` (thread preserved). | `DeleteMessageAsync` |
| BR-066 | Persist message first; SignalR `ReceiveMessage` is best-effort (no rollback on push failure). | `SendMessageAsync` |
| BR-067 | WebSocket `/ws/chat` requires JWT (`access_token` query); logs must **redact** token. | JwtBearer + Serilog enricher |
| BR-088 | Each org has exactly one **Organization** channel (`ChannelType.Organization`); created at **`POST /register`** (org bootstrap) and ensured on chat list for legacy orgs; display name = org name or `"Toàn công ty"`. **All active employees** are auto-added (`SyncAllActiveMembers`); new employees on create/provision join immediately; **terminate** removes membership. | `AuthService.RegisterAsync`, `OrgChannelService`, `EmployeeService` |
| BR-089 | `ListMineAsync` returns **Organization + Direct** only. `GET /api/v1/channels/org/members` lists active org employees (max 200) for starting DMs. `POST /channels` accepts **Direct only** for any authenticated user with an `Employee` profile (same org; reuses existing DM). | `ChannelService`, `ChannelEndpoints` |

---

## Schedule suggestions

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-070 | `POST .../suggest` does not write official `ShiftAssignment` rows and does **not** call AWS Bedrock. After successful suggestions, it may refresh the advisory insight context snapshot in DB. | `ScheduleService`, `ScheduleSuggestionOrchestrator` |
| BR-071 | Suggest/apply only on **`Draft`** schedules. | Services |
| BR-072 | Auto-scheduling is scoped to one department/week. **Org policy** (`org-scheduling-policy.v1.1`) is shared across branches; catalog `GET /api/v1/scheduling/rule-catalog`; Admin `GET/PUT /api/v1/org/scheduling-policy`, `POST .../scheduling-policy/wizard-draft` (Manager read-only). **All enforced catalog rules default `enabled: false`** — solver applies only rules the org Admin enables. Zero enabled rules → **pure preference** suggest (may over-staff a slot when many employees mark Preferred). Enforced keys include preference preflight/block, **min/max staff per shift**, full coverage, rest between shifts, **max shifts per employee day/week**, role match — values org-chosen when enabled. `SchedulingSolverDefaults` holds technical weights/timeouts only. PUT policy runs feasibility heuristic → **422** `ORG_SCHEDULING_POLICY_INFEASIBLE` when min > max staff or other conflicts. Suggest returns structured `reason` (`partial_coverage`, `infeasible`, …). Apply gợi ý always explicit. | `CpSatScheduleSuggestionService`, `OrganizationSchedulingSolverPolicy`, `OrganizationSchedulingPolicyFeasibilityValidator`, `OrganizationSchedulingPolicyWizard`, `SchedulingRuleCatalog` |
| BR-073 | Suggestions must not double-book (in-memory overlap check for the target week). | `HasOverlapInPlan` |
| BR-074 | Auto-scheduling employee eligibility uses Active branch membership plus department membership. `Employee.DepartmentId` is only the primary/backward-compatible department; assignment guards must accept any active department membership for the schedule department. | `LocationMembership`, `EmployeeDepartmentMembership`, `TryPrepareAssignmentAsync` |
| BR-075 | `apply-suggestions` validates **all** rows then commits in **one transaction** (all or none). | `ApplySuggestionsAsync` |
| BR-076 | Schedule insight context is a DB-stored JSON snapshot for explanation only, keyed by schedule and carrying `LocationId`, `DepartmentId`, `WeekStartDate`, and `ExpiresAt`; it is not a source of truth and never replaces `ShiftAssignment`. | `ScheduleInsightService` |
| BR-077 | Copying shift definitions is scoped to **one location**: source and all targets must be departments in the same branch. Only **active** shifts bound to the source `DepartmentId` are copied; duplicates in a target (same name + start + end, case-insensitive name) are skipped. Copy creates **new** `ShiftDefinition` rows — existing schedules/assignments are unchanged. | `ShiftDefinitionService.CopyAsync` |
| BR-077 | Bedrock schedule insight chat is advisory only. It may summarize, explain, and suggest manager review actions, but it must not create, update, apply, or publish assignments. | `ScheduleInsightService.ChatAsync` |
| BR-078 | Bedrock failures, throttling, empty output, or timeout must not affect `suggest` or `apply-suggestions`; insight chat fails independently with a service-unavailable response. | `ScheduleInsightService`, `IBedrockService` |
| BR-079 | Generate/refresh schedule insight context does not call Bedrock; it only serializes current schedule, rules, preferences, assignments, suggestions, and summary metadata. | `GenerateContextAsync` |
| BR-086 | CP-SAT suggest inputs are loaded by `ScheduleSuggestionContextLoader`. Employees with a **Submitted** preference board: only **Preferred/Available** lines are assignable; **Trống** (no line) and **Unavailable** are hard blocks. Existing draft assignments are locked per employee/slot: when an employee updates their submitted preferences after the last assignment/apply, only that employee's draft assignments are unlocked for re-suggest; other employees stay locked unless their own preferences changed or their assignment conflicts with Unavailable. All slots locked → `fully_assigned`. Apply upserts exact assignment tuples `(shiftDefinitionId, employeeId, date)` and sets `Schedule.SuggestionsAppliedAt`; multiple employees may remain on the same `(shift, date)` when staffing policy allows, so one employee's suggestion must not overwrite another employee's assignment. When `ClearOrphanAssignments=true`, only omitted tuples for affected employees are removed; otherwise omitted assignments are preserved. FE suggest sheet compares current draft vs new suggestions before apply. | `SchedulingAssignmentRules`, `SchedulingAssignmentLockPolicy`, `ScheduleRebalanceBaseline`, `CpSatScheduleSuggestionService`, `ApplySuggestionsAsync` |
| BR-087 | **Draft-only leave request:** Employee submits via `/self/leave-requests`; Manager/Admin approves via `/leave-requests/{id}/approve`. On approve: upsert preference **Unavailable**, submit if needed, delete conflicting `ShiftAssignment`. **No auto CP-SAT.** Blocked when schedule is Published (`SCHEDULE_ALREADY_PUBLISHED`). Real absence after publish = no check-in / attendance workflow. | `ScheduleLeaveRequestService` |

---

## Authentication & password

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-083 | Org Admin creates employee with generated temp password → `User.MustChangePassword = true`. Login/refresh responses expose `mustChangePassword`. FE shows a **persistent advisory banner** (driven by this flag) until password change succeeds — does not block app use. API clears the flag via `POST /auth/reset-password`. | `EmployeeService`, `AuthService.ResetPasswordAsync` |
| BR-084 | Forgot-password OTP flow is anonymous and three-step: `POST /auth/forgot-password` (send OTP) → `POST /auth/forgot-password/verify-otp` → `POST /auth/forgot-password/complete`. OTP expires in **1 minute**. While a live OTP exists, resend is blocked (`AUTH_OTP_RESEND_TOO_SOON`, 429). OTP state lives in **Redis** (TTL 1 min); consumed OTP is deleted on successful password reset. | `AuthService`, `IAuthOtpStore`, `RedisAuthOtpStore`, `AuthOtpHelper` |
| BR-085 | Forgot-password send is rate-limited per email in Redis: at most **5** OTP sends; the 6th attempt locks the email for **30 minutes** (`AUTH_OTP_SEND_LOCKED`, 429). Counter resets after successful password reset. Response is always generic success on send (no email enumeration). | `IAuthOtpStore`, `AuthService.ForgotPasswordAsync` |

---

## Notifications & cross-cutting

| ID | Rule | Enforcement |
|----|------|-------------|
| BR-080 | SMTP email when `Smtp:Host` + `Smtp:From` configured; else no-op logger. | Infrastructure DI |
| BR-081 | API responses use envelope `ApiResponse<T>` with `AppMessage` codes. | Application-wide |
| BR-082 | Business logic only in **Application** services; API is HTTP mapping only. | Architecture guardrail |
