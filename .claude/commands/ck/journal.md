---
description: Archive the current brainstorm spec to plans/{slug}/spec.md and append a dated log entry to .claude/session-data/journal.md.
---

Archive the most recent brainstorm session into a permanent log entry.

## Steps

1. **Read `.claude/session-data/.current-brainstorm.md`.**
   - If this file does not exist, stop and tell the user:
     `"No brainstorm found. Run /ck:brainstorm first, then /ck:journal to archive it."`
   - Parse: `slug`, `spec`, `report`, `date` from the file.

2. **Determine the slug.**
   - If `$ARGUMENTS` is non-empty, use it as the slug (overrides the pointer file).
   - Otherwise use the `slug` value from the pointer file.
   - If still empty, ask: `"Short slug for this brainstorm (e.g. auth-redesign)?"`

3. **Confirm the spec file exists** at the path in the pointer file (`plans/{slug}/spec.md`).
   - If missing, tell the user and stop — do not create an empty spec.

4. **Append a dated entry to `.claude/session-data/journal.md`** (create if it doesn't exist):
   ```
   ## {YYYY-MM-DD} — {slug}

   {2–4 sentences: what was explored, which option was selected or deferred, and why.}

   Spec: plans/{slug}/spec.md
   Report: plans/reports/{YYMMDD}-{slug}-brainstorm.md
   ```

5. **Confirm** with:
   ```
   Journal updated → .claude/session-data/journal.md
   Spec archived at → plans/{slug}/spec.md
   ```

6. **Surface friction signals as structured proposals.**
   - Read `.claude/session-data/journal.md` and collect all `## {YYYY-MM-DD}` headings in order. Use the **second-to-last** date (the entry that existed before this run's Step 4 append). If fewer than two parseable dates exist (journal was empty or had only one entry before this run), default to 7 days ago.
   - If `.claude/FRICTION.md` does not exist, skip this step silently.
   - Scan `.claude/FRICTION.md` for entries (headings `## [{YYYY-MM-DD}]`) dated **after** the journal date found above.
   - If none exist, skip silently — do not mention FRICTION.md.
   - If any entries exist, output one numbered proposal per entry, in the same order they were scanned:
     ```
     ## Friction Signals

     ### Proposal {N}: {brief context from the entry's heading}
     Affected component: {the concrete file/command/rule named in the entry}
     Predicted impact: {the entry's "Predicted impact" field}
     Risk: {tiny | normal | high-risk — one clause why}
     Suggested action: {the entry's "Suggested fix" field, phrased as a concrete next step}
     ```
   - Then ask: `"Any of these should become a harness improvement task? [list proposal numbers or 'none']"`
   - For each proposal number the user selects, append a `Promoted: {YYYY-MM-DD}` line to that entry in `.claude/FRICTION.md`, directly after its `Predicted impact:` line — this marks the entry as having become a task, distinct from entries that were only surfaced but never acted on. Leave `Actual outcome` untouched — it is filled in manually once the resulting task closes.

## Rules

- Append to journal, never overwrite — multiple entries accumulate over time.
- Do not modify the spec file — it was already written by `/ck:brainstorm`.
- After journaling, the pointer file (`.current-brainstorm.md`) remains — it is overwritten by the next brainstorm run.
- Once archived, `spec.md` is a historical snapshot, not a living document — do not keep re-reading it for current project state. After this point, current truth lives in `plan.md`, `feature_list.json`, and `.last-handoff.md` — consult those instead.
