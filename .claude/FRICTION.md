# Friction Backlog

This file is **append-only**. Never overwrite or reorder entries.

It captures moments where the harness slowed Claude down: a re-read to understand intent (not to verify after a write), a backtrack, or a mid-task pause caused by unclear instructions. Each entry is a candidate for a harness improvement task.

Trigger: `/ck:journal` surfaces entries added since the last journal date and asks whether any should become a task.

Every field must name a concrete file, command, or rule — not a general impression. Vague entries can't become tasks.

Vague (rejected): "Docs weren't clear about the plan format."
Concrete (required): "plan.md template in planner.md:42 omits the Risk: line required by ck-plan Step 0.5 — read planner.md twice to confirm it was actually missing."

Entry format:

```
## [{YYYY-MM-DD}] {brief context naming the specific file/command/rule}
Signal type: pause | backtrack | re-read
What happened: {one sentence, naming the concrete file/step/rule involved}
Suggested fix: {one sentence}
Predicted impact: {one sentence — what fixing this is expected to change}
Actual outcome: {filled in later, once the fix has been tried — or "(pending)"}
```

---

<!-- Entries go below this line, newest last -->
