#!/usr/bin/env python3
"""
PreToolUse Write|Edit|Bash|Agent hook — increments the per-session tool-call counter.

Single writer for {TEMP}/claude-tool-count-{session_id} — suggest_compact.py and
caveman_watch.py only read it. Exits immediately in subagents (CLAUDE_PARENT_SESSION_ID
set) so subagent tool calls don't inflate the parent session's count.
"""

import os
import sys
from pathlib import Path

session_id = os.environ.get("CLAUDE_SESSION_ID") or os.environ.get("PPID", "default")

tmp_dir = Path(os.environ.get("TEMP", os.environ.get("TMPDIR", "/tmp")))
counter_file = tmp_dir / f"claude-tool-count-{session_id}"


def main() -> None:
    if os.environ.get("CLAUDE_PARENT_SESSION_ID"):
        return

    try:
        count = int(counter_file.read_text().strip()) + 1 if counter_file.exists() else 1
        counter_file.write_text(str(count))
    except Exception:
        pass


if __name__ == "__main__":
    try:
        main()
    except Exception:
        sys.exit(0)
