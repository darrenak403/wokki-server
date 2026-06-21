#!/usr/bin/env python3
"""
PreToolUse Read|Write|Edit|Grep|Bash hook — block access to sensitive files.

Detection logic lives in hooks/lib/privacy_checker.py.
Allow-list: add filenames/paths to privacyBlock.allowList in .ck.json to bypass.

Grep is only checked when tool_input.path resolves to an existing file —
Grep's path is commonly a directory or omitted entirely (defaults to cwd),
and checking those would false-positive block legitimate searches inside
directories that merely contain a sensitive-sounding name. Glob is
intentionally not handled: its tool_input.pattern is a glob expression
(e.g. "**/*secret*.ts"), not a concrete file path, so running it through
check_file() would block legitimate feature names that happen to contain
sensitive-sounding words.
"""

import json
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "lib"))
from ck_config_utils import find_project_root, is_enabled
from hook_logger import HookLogger
from privacy_checker import check_bash, check_file, load_allow_list


def main() -> None:
    try:
        data = json.load(sys.stdin)
    except Exception:
        sys.exit(0)

    cwd = data.get("cwd") or os.getcwd()
    root = find_project_root(cwd)

    if not is_enabled("privacyBlock", root=root):
        sys.exit(0)

    log = HookLogger("privacy-block")
    allow_list = load_allow_list(cwd)
    tool_name = data.get("tool_name", "")
    tool_input = data.get("tool_input", {})

    if tool_name in ("Read", "Write", "Edit"):
        file_path = tool_input.get("file_path", "")
        match = check_file(file_path, allow_list)
        if match:
            sys.stderr.write(
                f"[privacy-block] Blocked: {Path(file_path).name!r} matches pattern '{match}'.\n"
                f"To allow, add the filename to privacyBlock.allowList in .ck.json "
                f"or ask the user for explicit permission."
            )
            sys.exit(2)

    elif tool_name == "Grep":
        path = tool_input.get("path", "")
        if path and Path(path).is_file():
            match = check_file(path, allow_list)
            if match:
                sys.stderr.write(
                    f"[privacy-block] Blocked: {Path(path).name!r} matches pattern '{match}'.\n"
                    f"To allow, add the filename to privacyBlock.allowList in .ck.json "
                    f"or ask the user for explicit permission."
                )
                sys.exit(2)

    elif tool_name == "Bash":
        cmd = tool_input.get("command", "")
        match = check_bash(cmd, allow_list)
        if match:
            sys.stderr.write(
                f"[privacy-block] Blocked: Bash command references sensitive file ({match!r}).\n"
                f"To allow, add the filename to privacyBlock.allowList in .ck.json "
                f"or ask the user for explicit permission."
            )
            sys.exit(2)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        sys.exit(0)
