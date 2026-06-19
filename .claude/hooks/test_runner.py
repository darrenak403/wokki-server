#!/usr/bin/env python3
"""
Hook: PostToolUse Write|Edit
Runs the adjacent test suite after a file is modified. Opt-in via .ck.json:
  { "testRunner": true }

Design constraints (from plan review):
- shell=False + hardcoded arg lists only — never executes values from package.json scripts
- Bounded test-file detection: immediate directory + one level up, no deeper
- Framework detection only fires when test files are found in bounded scope
- Manifest-only match (no adjacent test files) = silent exit
- All subprocess calls use shell=False
"""
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "lib"))
from ck_config_utils import load_ck_config
from hook_logger import HookLogger

_log = HookLogger("test-runner")

# Bounded: how many parent levels to search for test files
BOUNDED_LEVELS = 1

# Test file patterns per language
TEST_PATTERNS = {
    "ts": ["*.test.ts", "*.spec.ts"],
    "tsx": ["*.test.tsx", "*.spec.tsx"],
    "js": ["*.test.js", "*.spec.js"],
    "py": ["test_*.py", "*_test.py"],
    "go": ["*_test.go"],
}

# Directories that count as test presence when found in bounded scope
TEST_DIRS = ["__tests__", "tests", "test"]


def _find_test_files(file_path: Path) -> bool:
    """Return True if test files exist in bounded scope (immediate dir + BOUNDED_LEVELS up)."""
    search_dirs = [file_path.parent]
    parent = file_path.parent
    for _ in range(BOUNDED_LEVELS):
        parent = parent.parent
        if parent == parent.parent:  # reached filesystem root
            break
        search_dirs.append(parent)

    suffix = file_path.suffix.lstrip(".")
    patterns = TEST_PATTERNS.get(suffix, [])

    for d in search_dirs:
        if not d.is_dir():
            continue
        # Check test directory presence
        for td in TEST_DIRS:
            if (d / td).is_dir():
                return True
        # Check test file patterns
        for pat in patterns:
            if any(d.glob(pat)):
                return True
    return False


def _detect_framework(file_path: Path, test_files_found: bool) -> list[str] | None:
    """
    Walk up to find nearest project manifest.
    Returns hardcoded command list or None.
    manifest-only match without test files = None (silent exit).
    """
    if not test_files_found:
        return None

    current = file_path.parent
    while True:
        # package.json — detect vitest vs jest
        pkg = current / "package.json"
        if pkg.exists():
            try:
                data = json.loads(pkg.read_text(encoding="utf-8"))
                deps = {}
                deps.update(data.get("dependencies", {}))
                deps.update(data.get("devDependencies", {}))
                if "vitest" in deps:
                    return ["npx", "vitest", "run"]
                # Use npm test with --watchAll=false for jest (never reads "test" script value)
                return ["npm", "test", "--", "--watchAll=false"]
            except Exception:
                pass

        # Python
        for marker in ["pytest.ini", "pyproject.toml", "setup.py", "setup.cfg"]:
            if (current / marker).exists():
                return [sys.executable, "-m", "pytest", "-x", "-q"]

        # Go
        if (current / "go.mod").exists():
            return ["go", "test", "./..."]

        # Rust
        if (current / "Cargo.toml").exists():
            return ["cargo", "test", "-q"]

        parent = current.parent
        if parent == current:
            break
        current = parent

    return None


def main() -> None:
    try:
        data = json.load(sys.stdin)
    except Exception:
        return

    # Config gate — opt-in only
    config = load_ck_config()
    if not config.get("testRunner", False):
        return

    raw_path = (data.get("tool_input") or {}).get("file_path", "")
    if not raw_path:
        return

    file_path = Path(raw_path)
    if not file_path.exists():
        return

    # Bounded test-file detection
    test_files_found = _find_test_files(file_path)

    # Framework detection (requires test files in bounded scope)
    cmd = _detect_framework(file_path, test_files_found)
    if not cmd:
        return

    _log.info(f"Running: {' '.join(cmd)}")

    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30,
            shell=False,
            cwd=str(file_path.parent),
        )
    except subprocess.TimeoutExpired:
        _log.warn("Test run timed out after 30s")
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": f"Test runner timed out after 30s ({' '.join(cmd)}).\nFix: ensure tests complete within 30 seconds.",
            }
        }))
        return
    except FileNotFoundError:
        _log.warn(f"Command not found: {cmd[0]}")
        return

    if result.returncode != 0:
        output = (result.stdout + result.stderr).strip()
        lines = output.splitlines()[:20]
        failure_text = "\n".join(lines)
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": (
                    f"Tests failed ({' '.join(cmd)}):\n{failure_text}\n\n"
                    "Fix: make the failing tests pass before proceeding."
                ),
            }
        }))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        _log.error(str(e))
        sys.exit(0)
