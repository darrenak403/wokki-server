# Wokki Server — session bootstrap (Claude)

**Product:** Wokki Shift Ops MVP — single-tenant workforce API (.NET 10, `/api/v1`).

**Read before coding:** `CLAUDE.md` → `docs/business-rules.md` (`BR-xxx`) → `.claude/contexts/wokki.md` (code map).

**Never violate:**
- Roles: `Admin` | `Manager` | `User` only
- Schedule: `Draft` → `Published`; assignments only in **Draft**; Monday `WeekStartDate`
- Preferences ≠ official `ShiftAssignment` (advisory)
- `/auth/me` ≠ `/self/*` (needs `Employee`)
- Swaps only on **Published** schedules
- Workspace/sidebar actions are selected-branch scoped; department transfer must target the employee's Active branch
- Branch `LocationSchedulingPolicy` before suggest; membership not only `Employee.DepartmentId`
- Bedrock = insight only; `suggest` / `apply-suggestions` never depend on Bedrock

**Code:** `Wokki.Api` HTTP only · `Application` services + `ApiResponse<T>` · `task build` before done · no `Features/*` folder.

**Sibling FE:** `../wokki-client` · **Docs VI:** `docs/vi/README.md`
