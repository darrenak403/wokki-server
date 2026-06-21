#!/usr/bin/env python3
"""
Hook audit — run manually, DO NOT wire into settings.json.

Lists which hooks are wired in settings.json and at which event, the enabled
status from .ck.json, and which hooks exist on disk but aren't wired anywhere.

Also runs a DYNAMIC health check (not just file existence): for each blocking
hook, sends malformed input and verifies the REAL exit code — catches a
"silent bypass" hook (exit 0 due to a bug, instead of failing open as designed).

simplify_gate.py is excluded from the dynamic check: it writes
.claude/session-data/simplify-tracker-{session_id}.json as a side effect of a
normal run, and auditing it could pollute real session state. Malformed JSON
input alone happens to return before that write today, but excluding it keeps
this script safe even if that early-return path ever changes.

    python .claude/hooks/audit_hooks.py
"""

import json
import subprocess
import sys
from pathlib import Path

HOOKS_DIR = Path(__file__).parent
ROOT = HOOKS_DIR.parent.parent

# Hooks that can deny a tool call — checked for fail-open behavior on bad input.
# simplify_gate.py intentionally excluded — see module docstring.
DYNAMIC_CHECK_HOOKS = ("privacy_block.py", "build_check.py", "test_runner.py")


def _load_json(p: Path) -> dict:
    try:
        return json.loads(p.read_text(encoding="utf-8-sig"))
    except Exception:
        return {}


def _wired_hooks(settings: dict) -> dict[str, list[str]]:
    """Map each hook name to the events it is wired to."""
    out: dict[str, list[str]] = {}
    for event, groups in settings.get("hooks", {}).items():
        for g in groups:
            for h in g.get("hooks", []):
                cmd = h.get("command", "")
                name = Path(cmd.split()[-1]).name if cmd else ""
                if name:
                    out.setdefault(name, []).append(event)
    return out


def _run_hook(name: str, stdin: str) -> int:
    """Run hook with dummy stdin, return real exit code."""
    try:
        r = subprocess.run(
            [sys.executable, str(HOOKS_DIR / name)],
            input=stdin, capture_output=True, text=True, timeout=15,
            cwd=str(ROOT),
        )
        return r.returncode
    except Exception as e:
        print(f"    health check failed: {e}")
        return -1


def main() -> None:
    settings = _load_json(ROOT / ".claude" / "settings.json")
    ck = _load_json(ROOT / ".ck.json")
    wired = _wired_hooks(settings)

    print("=== Hook Audit ===\n")

    print("-- Already wired in settings.json --")
    for name, events in sorted(wired.items()):
        print(f"  {name:32s} -> {', '.join(events)}")

    print("\n-- Exist in hooks/ but NOT wired --")
    on_disk = {p.name for p in HOOKS_DIR.glob("*.py") if p.name != Path(__file__).name}
    unwired = sorted(on_disk - set(wired))
    for name in unwired:
        print(f"  {name}")
    if not unwired:
        print("  (none)")

    print("\n-- Enabled status (.ck.json) --")
    for key in ("privacyBlock", "simplify", "testRunner"):
        section = ck.get(key, {})
        if key == "simplify":
            enabled = section.get("gate", {}).get("enabled", "(default false)")
        elif key == "testRunner":
            enabled = ck.get("testRunner", "(default false)")
        else:
            enabled = section.get("enabled", "(default true)")
        print(f"  {key:16s} enabled = {enabled}")

    print("\n-- Health check DYNAMIC (real exit code) --")
    print("  (simplify_gate.py excluded — writes session-data state, see module docstring)")
    # fail-open: malformed JSON must make every hook exit 0
    for name in DYNAMIC_CHECK_HOOKS:
        if (HOOKS_DIR / name).exists():
            rc = _run_hook(name, "BAD JSON NOT PARSEABLE")
            print(f"  {name} (malformed JSON -> fail-open): exit={rc} "
                  f"[{'OK' if rc == 0 else 'ERROR: did not fail open'}]")


if __name__ == "__main__":
    main()
