---
name: wokki
description: Load Wokki Shift Ops business rules, API map, and codebase conventions. Use when working on scheduling, swaps, attendance, payroll, chat, Bedrock insight, or any Wokki feature in wokki-server or coordinating with wokki-client.
---

# Wokki — business & codebase (server)

## When to use

- Any task touching schedules, assignments, preferences, swaps, attendance, payroll, chat, or scheduling policy
- Before claiming a feature is complete
- When unsure which service or `BR-xxx` applies

## Read (in order)

1. [CLAUDE.md](../../CLAUDE.md) at repo root
2. [contexts/wokki.md](../contexts/wokki.md) — flows + endpoint ↔ service map
3. [docs/business-rules.md](../../../docs/business-rules.md) — locked `BR-xxx`
4. [docs/process-flows.md](../../../docs/process-flows.md) — if changing state machines
5. [docs/api-catalog.md](../../../docs/api-catalog.md) — if adding/changing endpoints

**Tiếng Việt:** [docs/vi/README.md](../../../docs/vi/README.md) · [docs/vi/fe-integration-guide.md](../../../docs/vi/fe-integration-guide.md)

## Implementation checklist

- [ ] Application service returns `ApiResponse<T>` only
- [ ] New route in `Apis/{Feature}/{Feature}Endpoints.cs` + `MapEndpoints()`
- [ ] Validator + `AppMessages` code
- [ ] `task build` passes
- [ ] Docs + `AGENTS.md` / `wokki-client/AGENTS.md` if behavior changed

## Common mistakes

| Mistake | Correct |
|---------|---------|
| Logic in `Wokki.Api` | Application service |
| `DbContext` in Application | `IUnitOfWork` |
| Bedrock applies assignments | Heuristic suggest/apply only |
| Ignore department membership | `EmployeeDepartmentMembership` |
| Preferences = official schedule | Published `ShiftAssignment` |
