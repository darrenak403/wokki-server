# Wokki Server — Claude Code

> Claude loads this file automatically. Project kit lives in **`.claude/`** (rules, contexts, skills, commands, hooks).

@.claude/contexts/wokki-bootstrap.md

@AGENTS.md

---

## Claude project map

| Path | Purpose |
|------|---------|
| [.claude/README.md](./.claude/README.md) | Index of `.claude/` |
| [.claude/contexts/wokki.md](./.claude/contexts/wokki.md) | **Full** product + codebase map |
| [.claude/rules/wokki-backend.md](./.claude/rules/wokki-backend.md) | Rules on `src/**/*.cs` |
| [.claude/rules/wokki-business.md](./.claude/rules/wokki-business.md) | Doc sync + `BR-xxx` |
| [.claude/skills/wokki/SKILL.md](./.claude/skills/wokki/SKILL.md) | Skill: load Wokki context |
| [.claude/commands/ck/wokki.md](./.claude/commands/ck/wokki.md) | Slash: `/ck:wokki` |

**Cursor mirror:** `.cursor/` (same kit + `rules/wokki-backend.mdc`).

**Frontend:** `../wokki-client` · **Entry there:** `../wokki-client/CLAUDE.md`

---

## Documentation (read before behavior changes)

| # | Document |
|---|----------|
| 1 | [docs/README.md](./docs/README.md) |
| 2 | [docs/brd.md](./docs/brd.md) |
| 3 | [docs/business-rules.md](./docs/business-rules.md) — **`BR-xxx` locked** |
| 4 | [docs/process-flows.md](./docs/process-flows.md) |
| 5 | [docs/api-catalog.md](./docs/api-catalog.md) |
| 6 | [docs/architecture.md](./docs/architecture.md) · [docs/minimal-api.md](./docs/minimal-api.md) |

**Tiếng Việt:** [docs/vi/README.md](./docs/vi/README.md) · [docs/vi/api-catalog.md](./docs/vi/api-catalog.md) · [docs/vi/fe-integration-guide.md](./docs/vi/fe-integration-guide.md)

When business behavior changes: update locked docs + `AGENTS.md` + `../wokki-client/AGENTS.md` + `.claude/contexts/wokki.md` in the same task.

---

## Business essentials (`BR-xxx` detail in docs)

- **Roles:** `Admin`, `Manager`, `User` — JWT + `RequireRole`
- **Schedule:** `Draft` → `Published`; assignments only in **Draft**; Monday week start
- **Preferences** advisory vs official **assignments**
- **`/auth/me`** ≠ **`/self/*`** (requires `Employee`)
- **Swaps** on published schedules only; location timezone cutoff
- **Auto-schedule:** branch `LocationSchedulingPolicy` (`location-scheduling-policy.v3`) first; department membership for eligibility
- **Bedrock:** insight/chat on context snapshot only — never create/apply/publish assignments (`BR-077`–`BR-079`)

---

## Solution layout

```text
src/Wokki.Api/           → Apis/{Feature}/{Feature}Endpoints.cs (register in MapEndpoints)
src/Wokki.Application/   → Services/{Feature}/Interfaces|Implementations, Dtos, Validators
src/Wokki.Domain/        → Entities, IUnitOfWork, RoleConstants
src/Wokki.Infrastructure/→ EF, JWT
src/Wokki.Common/        → ApiResponse<T>, AppMessages
```

### API modules

| Module | Base |
|--------|------|
| Auth, Users, Employees, Locations, Departments, Shifts | `/api/v1/...` |
| Schedules (+ suggest, apply, insights) | `/api/v1/schedules` |
| Self | `/api/v1/self` |
| Swap, Attendance, Payroll, Channels, Bedrock | `/api/v1/...` |
| Health | `/health` |

### Services

`Auth`, `User`, `Employee`, `Location`, `Department`, `Shift`, `Schedule` (+ preferences, dept config, insights), `SwapRequest`, `Attendance`, `Payroll`, `Chat`, `Bedrock`.

---

## Commands

```bash
task build              # required before done
task run                # :8386
task docker:postgres
task migration:add -- <Name>
task migration:update
```

Scalar: http://localhost:8386/scalar · Seed: `admin@gmail.com` / `manager@gmail.com` / `user@gmail.com` — `12345@Abc`

---

## Claude workflows

| Command / skill | Use |
|-----------------|-----|
| `/ck:wokki` | Load product + `BR-xxx` for session |
| `/ck:cook` | Implement from plan |
| `/ck:plan` | Research + phased plan |
| `/ck:fix` | Debug with scout evidence |
| Skill `wokki` | Deep business + file map |

Hooks (`.claude/settings.json`): build check after edits, session bootstrap, privacy block.

---

## Definition of done

1. `task build` passes  
2. No `BR-xxx` violations  
3. Endpoints registered with minimal-api pattern  
4. Docs + agent context updated if behavior changed  
