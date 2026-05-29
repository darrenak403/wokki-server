# Glossary

| Term | Definition |
|------|------------|
| **Admin** | System administrator: master data, users, payroll export, override capabilities. |
| **Manager** | Operations lead: schedules, assignments, swap overrides, attendance adjust, channel creation. |
| **User** | Frontline employee: self-service (`/self/*`), clock in/out, swap peer actions. |
| **Location** | Physical site with timezone (`TimeZone` IANA id). Parent of departments. |
| **Department** | Org unit under a location; schedules and pay periods are department-scoped. |
| **Employee** | Workforce profile linked 1:1 to a `User`; `Position` mirrors primary department name (solver/scheduling); `HourlyRate`, `DepartmentId`. |
| **Shift definition** | Reusable shift template: name, start/end time, `RequiredRole`, location/department scope. |
| **Schedule** | Weekly plan for one department; `WeekStartDate` must be Monday. |
| **Shift assignment** | One employee on one shift definition on one calendar date within a schedule. |
| **Publish** | Transition schedule `Draft` → `Published`; employees can see shifts and request swaps. |
| **Swap request** | Peer-to-peer request to exchange two published assignments. |
| **Attendance record** | Clock-in / clock-out pair with computed `WorkedMinutes`. |
| **Pay period** | Date range per department; `Open` or `Locked`. |
| **Payroll line** | Snapshot row per employee per locked period (minutes, rate, gross pay). |
| **Channel** | Chat room: `Direct` (2 members) or `Group`. |
| **Schedule suggestion** | Transient DTO from heuristic engine; not stored until applied. |
| **Heuristic engine** | Rule-based suggestion service (`HeuristicScheduleSuggestionService`); not an external LLM. |
| **Single-tenant** | One enterprise per deployment/database; new customer = new instance. |

## Status enums (code reference)

| Enum | Values | Notes |
|------|--------|-------|
| `ScheduleStatus` | `Draft`, `Published`, `Locked` | MVP uses **Draft/Published** only; `Locked` reserved. |
| `SwapStatus` | `Pending`, `PeerAccepted`, `PeerDeclined`, `ManagerApproved`, `ManagerRejected`, `Cancelled` | See [process-flows.md](./process-flows.md). |
| `PayPeriodStatus` | `Open`, `Locked` | Locked blocks attendance adjustment for dates in period. |
| `ChannelType` | `Direct`, `Group` | |
