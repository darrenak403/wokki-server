---
description: Capture decisions, blockers, and next steps into .last-handoff.md before ending a session.
---

Before writing the handoff, check `feature_list.json` at project root (same directory as `.ck.json`) if it exists:

- Collect entries where `status == "active"` and `evidence` is absent or empty string — these are in-progress features with no verification record.
- Collect entries where `status == "passing-unverified"` — these passed in fast mode without test evidence.
- Collect entries where `status == "blocked"` **and** `blocked_by` is non-empty — these have explicit dependency blockers. (`blocked_by` alone without `status: "blocked"` does not trigger this; a feature may list dependencies without being currently blocked.)
- If none of these lists have entries, or if `feature_list.json` is absent, skip this check entirely.

If unverified active features exist, output this block before the handoff questions (then continue writing the file regardless):

```
## Unverified Active Features
- {id}: {title} — active, no evidence recorded
```

If passing-unverified features exist, output:

```
## Fast-Mode Features (Unverified)
- {id}: {title} — passing-unverified (fast mode, no tests run)
```

If blocked features (status == "blocked" and blocked_by non-empty) exist, output:

```
## Blocked Features
- {id}: {title} — blocked by: {blocked_by ids, comma-separated}
```

---

Now reflect on what happened this session and write a structured handoff so the next session can resume without re-discovering context.

Answer these questions honestly:

1. **Decisions** — what non-obvious decisions were made and why? (e.g., "chose X over Y because Z", "skipped A because B")
2. **Blockers** — what is currently blocked, incomplete, or waiting on external input?
3. **Next Steps** — what should happen first in the next session, in priority order?
4. **Module Quality** — for each meaningful module or subsystem touched this session, assign an honest grade. Grade scale: A = no known debt, B = working but has documented debt, C = needs attention before next feature.

Write the answers to `.claude/session-data/.last-handoff.md` using exactly this structure (replace the entire file if it already exists):

```
# Handoff — {YYYY-MM-DD HH:MM}
## Decisions
- {decision and rationale}
## Blockers
- {blocker description, or "(none)"}
## Next Steps
- {first action for next session}
## Module Quality
| Module | Grade | Notes |
|--------|-------|-------|
| {module name} | {A/B/C} | {one-line note} |
```

If unverified, passing-unverified, or blocked features were found above, also add them as sections in the written file (same content as the warning blocks output above).

Rules:
- Use bullet points, not prose paragraphs (except Module Quality table)
- Each bullet must be specific enough to act on without re-reading the conversation
- If nothing is blocked, write `- (none)` under Blockers — do not omit the section
- Module Quality table must always be present — minimum one header row even if no modules were touched

After writing `.last-handoff.md`, append a JSONL trace entry (best-effort — trace failure must not block or alter the handoff):

1. Determine month suffix: `YYYY-MM` from current timestamp.
2. Target path: `.claude/traces/trace-log-{YYYY-MM}.jsonl`. Create `.claude/traces/` directory if absent.
3. Judge the risk tier from `feature_list.json` at project root, using the same blocked condition as the `## Blocked Features` check above (status == "blocked" **and** `blocked_by` non-empty — a "blocked" status alone does not count):
   - Any feature matching that blocked condition → **high** risk
   - Else any feature `status == "active"` with no `evidence` → **normal** risk
   - Else (all passing, empty features array, or file absent) → **low** risk
4. Derive `outcome` using the same blocked condition, priority order:
   - Any feature matching the blocked condition → `"blocked"`
   - Else any feature `status == "active"` → `"active"`
   - Else → `"passing"`
5. Append exactly one JSON line, shaped by the risk tier:
   - **Low**: `{"ts": "<ISO-8601 timestamp>", "outcome": "<see above>"}` — no other fields.
   - **Normal**: `{"ts": "<ISO-8601 timestamp>", "task": "<first bullet under ## Next Steps>", "decision": "<first bullet under ## Decisions>", "outcome": "<see above>", "feature_id": "<optional>"}` — today's existing shape, unchanged.
   - **High**: the normal shape plus `"blocked_by": "<comma-separated blocked_by ids of the matching feature>"`.
6. `feature_id` (normal and high tiers): id of the first `active` feature if one exists, else first feature matching the blocked condition, else omit the field entirely.
7. If the write fails for any reason (permissions, malformed state), skip silently — the handoff file is already written, regardless of which tier was chosen.

Known limitation: old `.jsonl` files accumulate in `.claude/traces/` with no automatic cleanup — delete manually if storage is a concern.

Confirm with:
- Trace succeeded: `Handoff saved → .claude/session-data/.last-handoff.md ({timestamp}) | Trace → traces/trace-log-{YYYY-MM}.jsonl | Features verified: {N passing}/{total tracked}`
- Trace failed: `Handoff saved → .claude/session-data/.last-handoff.md ({timestamp}) | Trace → FAILED (best-effort) | Features verified: {N passing}/{total tracked}`

## Living Files vs. Historical Snapshot

When resuming a session, start with the living files — they reflect current state:
- `.claude/session-data/.last-handoff.md` — decisions, blockers, next steps from the last session
- `feature_list.json` — current status of every tracked feature
- `plan.md` (and its phase files) — what's done, what's in progress

`plans/{slug}/spec.md` is not one of these — once journaled (see `/ck:journal`), it is a historical record of what was originally explored, not a source of current state. Do not re-read it to figure out what to do next.
