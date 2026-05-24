---
description: Load Wokki product context — BR-xxx, codebase map, and doc index for this session
argument-hint: [optional topic keyword, e.g. schedule | swap | bedrock]
---

Load Wokki Shift Ops context for this session.

**Argument:** `$ARGUMENTS` (optional — focus area)

## Steps

1. Read [CLAUDE.md](../../../CLAUDE.md) (repo root)
2. Read [.claude/contexts/wokki.md](../../contexts/wokki.md)
3. Skim [docs/business-rules.md](../../../docs/business-rules.md) — all `BR-xxx` are binding
4. If argument provided, grep `docs/` and `src/` for that topic and read the matching service + endpoints
5. Reply with a **short** summary (≤15 bullets):
   - Product scope in one sentence
   - Relevant `BR-xxx` for the topic
   - Key files to edit
   - Commands to verify (`task build`)
   - Whether FE (`../wokki-client`) must change

Do not start implementation until the user confirms or asks to proceed.
