---
paths:
  - ".claude/commands/**/*.md"
---

# Command Design Rules

- **Commands orchestrate only.** No review checklists, fix logic, or business rules — those belong in agents or skills.
- **Readable top-to-bottom in under a minute.** If it isn't, extract to agents.
- **Shared multi-step logic goes in an agent, not copy-pasted.** If a new command is a flag variation, make it a `--flag` on the existing one.
- **Every command ends with a verifiable outcome** — a report, a file, a test result, a commit hash. "Done" is not verifiable.
- **Challenge before adding.** Does this duplicate an existing command? Could it be a flag instead?

## Scope Control

- **WIP=1.** One user request is active at a time. A "task" is one user request — it may write multiple files if all are required to satisfy that single request. Do not start a second independent task before the first is verified complete.
- **If a second task surfaces mid-execution**, record it as a bullet under `## Next Steps` (or via `/ck:handoff`) and continue with the current task.
- **Scope creep** is any action not directly required to satisfy the command's stated outcome: fixing unrelated bugs noticed in passing, refactoring adjacent code, adding unrequested features, or "improving things while we're here."
- **Call it out.** When scope creep is detected, say so in one sentence and stop: `"Scope: [X] is out of scope for this task — noting for next session."` Do not silently absorb extra work.
