#!/usr/bin/env python3
"""
Hook: PostToolUse Write|Edit
Detects project type from the edited file and runs the appropriate build/type-check.
Injects compiler errors as additionalContext. Silent on success or unsupported file types.

Supported:
  .cs / .csproj  → dotnet build --no-restore -q
  .ts / .tsx     → tsc --noEmit (finds tsconfig.json walking up)
  .py            → python -m py_compile <file>
  .go            → go build ./...  (from module root)
  .rs            → cargo check -q  (from Cargo.toml root)
"""

import sys
import json
import re
import subprocess
from pathlib import Path


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def walk_up(file_path: Path, filename: str) -> Path | None:
    for parent in file_path.parents:
        candidate = parent / filename
        if candidate.exists():
            return candidate
    return None


def walk_up_glob(file_path: Path, pattern: str) -> Path | None:
    for parent in file_path.parents:
        matches = list(parent.glob(pattern))
        if matches:
            return matches[0]
    return None


def run(cmd: list[str], cwd: str) -> str:
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, cwd=cwd, timeout=30)
        return result.stdout + result.stderr
    except FileNotFoundError:
        # Underlying tool binary not installed — caller's outer try/except is a
        # second safety net; this makes the skip explicit at the source too.
        return ""


# ---------------------------------------------------------------------------
# Per-language commands (capability name → command template)
# ---------------------------------------------------------------------------

COMMANDS: dict[str, list[str]] = {
    "dotnet_build": ["dotnet", "build", "{target}", "--no-restore", "-q"],
    "typescript_check": ["{tsc_cmd}", "--noEmit", "--pretty", "false"],
    "python_compile": ["{python}", "-m", "py_compile", "{file}"],
    "go_build": ["go", "build", "./..."],
    "rust_check": ["cargo", "check", "-q", "--message-format=short"],
}


def build_command(name: str, **placeholders: str) -> list[str]:
    return [part.format(**placeholders) if "{" in part else part for part in COMMANDS[name]]


# ---------------------------------------------------------------------------
# Language-specific checkers
# ---------------------------------------------------------------------------

def check_dotnet(file_path: Path) -> str | None:
    csproj = walk_up_glob(file_path, "*.csproj")
    if not csproj:
        return None
    sln_root = walk_up_glob(file_path, "*.sln")
    cwd = str(sln_root.parent if sln_root else csproj.parent)
    output = run(build_command("dotnet_build", target=str(csproj)), cwd)
    errors = [l for l in output.splitlines() if re.search(r": error CS\d+:", l)]
    if errors:
        return f"dotnet build errors in {csproj.name}:\n" + "\n".join(errors[:5])
    return None


def check_typescript(file_path: Path) -> str | None:
    tsconfig = walk_up(file_path, "tsconfig.json")
    if not tsconfig:
        return None
    cwd = tsconfig.parent
    # prefer local tsc binary; fall back to global
    local_tsc = cwd / "node_modules" / ".bin" / "tsc"
    tsc_cmd = str(local_tsc) if local_tsc.exists() else "tsc"
    output = run(build_command("typescript_check", tsc_cmd=tsc_cmd), str(cwd))
    errors = [l for l in output.splitlines() if re.search(r"error TS\d+:", l)]
    if errors:
        return "tsc errors:\n" + "\n".join(errors[:5])
    return None


def check_python(file_path: Path) -> str | None:
    cmd = build_command("python_compile", python=sys.executable, file=str(file_path))
    result = subprocess.run(cmd, capture_output=True, text=True)
    output = (result.stdout + result.stderr).strip()
    if result.returncode != 0 and output:
        return f"Python syntax error:\n{output}"
    return None


def check_go(file_path: Path) -> str | None:
    go_mod = walk_up(file_path, "go.mod")
    if not go_mod:
        return None
    output = run(build_command("go_build"), str(go_mod.parent))
    errors = [l for l in output.splitlines() if l.strip()]
    if errors:
        return f"go build errors:\n" + "\n".join(errors[:5])
    return None


def check_rust(file_path: Path) -> str | None:
    cargo_toml = walk_up(file_path, "Cargo.toml")
    if not cargo_toml:
        return None
    output = run(build_command("rust_check"), str(cargo_toml.parent))
    errors = [l for l in output.splitlines() if "error" in l.lower()]
    if errors:
        return f"cargo check errors:\n" + "\n".join(errors[:5])
    return None


# ---------------------------------------------------------------------------
# Dispatch
# ---------------------------------------------------------------------------

CHECKERS: dict[str, callable] = {
    ".cs": check_dotnet,
    ".ts": check_typescript,
    ".tsx": check_typescript,
    ".py": check_python,
    ".go": check_go,
    ".rs": check_rust,
}

HINTS: dict[str, str] = {
    ".cs": "Fix: resolve the build error above before proceeding.",
    ".ts": "Fix: resolve the TypeScript type error before proceeding.",
    ".tsx": "Fix: resolve the TypeScript type error before proceeding.",
    ".py": "Fix: correct the syntax error shown above before proceeding.",
    ".go": "Fix: ensure the package compiles cleanly before continuing.",
    ".rs": "Fix: resolve the Rust compiler error before continuing.",
}


def main() -> None:
    try:
        data = json.load(sys.stdin)
    except Exception:
        return

    raw_path = data.get("tool_input", {}).get("file_path", "")
    if not raw_path:
        return

    file_path = Path(raw_path)
    suffix = file_path.suffix.lower()
    checker = CHECKERS.get(suffix)
    if not checker:
        return

    try:
        error_msg = checker(file_path)
    except (FileNotFoundError, subprocess.TimeoutExpired):
        return

    if error_msg:
        hint = HINTS.get(suffix, "Fix: resolve the error above before proceeding.")
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": f"{error_msg}\n\n{hint}",
            }
        }))


if __name__ == "__main__":
    main()
