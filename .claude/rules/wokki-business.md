---
paths:
  - "**/*"
---

# Wokki — shared business context (BE)

When changing workflow, permissions, statuses, API meaning, or user-facing copy:

1. Read [docs/business-rules.md](../../docs/business-rules.md) (`BR-xxx`) and [docs/process-flows.md](../../docs/process-flows.md)
2. Update locked docs (`docs/brd.md`, `docs/api-catalog.md`, `docs/vi/*` as needed) in the same task
3. Mirror durable rules in [AGENTS.md](../../AGENTS.md) and `../wokki-client/AGENTS.md` when FE is affected
4. Update [.claude/contexts/wokki.md](../contexts/wokki.md) if the codebase map changes

Key distinctions:

- **Preferences** ≠ **assignments**
- **Department membership** for multi-department employees (not only `Employee.DepartmentId`)
- **Branch** `LocationSchedulingPolicy` before auto-suggest
- **Bedrock** advisory only — heuristic suggest/apply must stand alone
