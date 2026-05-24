# Claude Code — Wokki Server

Open this repo as the workspace root. Claude auto-loads **[../CLAUDE.md](../CLAUDE.md)**.

## Start here

| Priority | File |
|----------|------|
| 1 | [../CLAUDE.md](../CLAUDE.md) — entry (`@` imports bootstrap + AGENTS) |
| 2 | [contexts/wokki.md](./contexts/wokki.md) — full product & code map |
| 3 | [../docs/business-rules.md](../docs/business-rules.md) — `BR-xxx` locked |

**Session hook** injects [contexts/wokki-bootstrap.md](./contexts/wokki-bootstrap.md) on every `SessionStart`.

## Slash commands

| Command | File |
|---------|------|
| `/ck:wokki` | [commands/ck/wokki.md](./commands/ck/wokki.md) |
| `/ck:cook` | [commands/ck/cook.md](./commands/ck/cook.md) |
| `/ck:plan` | [commands/ck/plan.md](./commands/ck/plan.md) |
| `/ck:fix` | [commands/ck/fix.md](./commands/ck/fix.md) |
| `/ck:docs-fe` | [commands/ck/docs-fe.md](./commands/ck/docs-fe.md) |

## Skills

| Skill | Path |
|-------|------|
| **wokki** | [skills/wokki/SKILL.md](./skills/wokki/SKILL.md) |
| code-review | [skills/code-review/SKILL.md](./skills/code-review/SKILL.md) |
| ck-cook / ck-plan / ck-fix | `skills/ck-*/SKILL.md` |

## Rules (path-scoped)

| Rule | Scope |
|------|-------|
| [wokki-backend.md](./rules/wokki-backend.md) | `src/**/*.cs`, `docs/**` |
| [wokki-business.md](./rules/wokki-business.md) | All files — doc + `BR-xxx` sync |
| [agents.md](./rules/agents.md) | `.claude/agents/**` |
| [skills.md](./rules/skills.md) | `.claude/skills/**` |
| [commands.md](./rules/commands.md) | `.claude/commands/**` |

## Agents

`planner`, `debugger`, `tester`, `code-reviewer`, `docs-manager`, `scout`, finalize trio — see [agents/](./agents/).

## Hooks

Configured in [settings.json](./settings.json): session bootstrap, build check (`task build`), privacy block.

**Cursor mirror:** `.cursor/` — same layout; use when working in Cursor IDE.

**Frontend repo:** `../wokki-client` → [../wokki-client/.claude/README.md](../wokki-client/.claude/README.md)
