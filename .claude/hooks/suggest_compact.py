#!/usr/bin/env python3
"""
UserPromptSubmit hook — reads the tool-call counter (written by tool_counter.py)
and suggests /compact at thresholds via stderr, so it only surfaces when the
user sends a new message, never mid-pipeline between tool calls.

Environment:
  CLAUDE_SESSION_ID   Session identifier (used to isolate counter per session)
  COMPACT_THRESHOLD   Tool call count at which to first suggest compact (default 50)
"""

import json
import os
import sys
from pathlib import Path


def main() -> None:
    if os.environ.get("CLAUDE_PARENT_SESSION_ID"):
        return

    session_id = os.environ.get("CLAUDE_SESSION_ID") or os.environ.get("PPID", "default")
    threshold = int(os.environ.get("COMPACT_THRESHOLD", "50"))

    tmp_dir = Path(os.environ.get("TEMP", os.environ.get("TMPDIR", "/tmp")))
    counter_file = tmp_dir / f"claude-tool-count-{session_id}"

    try:
        count = int(counter_file.read_text().strip())
    except Exception:
        return

    if count != threshold and not (count > threshold and count % 25 == 0):
        return

    phase = None
    context_file = Path(".claude/session-data/session-context.json")
    try:
        ctx = json.loads(context_file.read_text(encoding="utf-8"))
        phase = ctx.get("phase")
    except Exception:
        pass

    phase_suffix = f" (phase: {phase})" if phase else ""

    if count == threshold:
        print(
            f"[StrategicCompact] {threshold} tool calls reached{phase_suffix} — "
            f"consider /compact if transitioning phases",
            file=sys.stderr,
        )
    else:
        print(
            f"[StrategicCompact] {count} tool calls{phase_suffix} — "
            f"good checkpoint for /compact if context is stale",
            file=sys.stderr,
        )


if __name__ == "__main__":
    try:
        main()
    except Exception:
        sys.exit(0)
