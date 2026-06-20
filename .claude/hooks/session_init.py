#!/usr/bin/env python3
"""
SessionStart hook — initialize session context.

Fires on startup, resume, clear, and compact. Loads coding level and previous
session state. Skips heavy loading for subagents (detected via
CLAUDE_PARENT_SESSION_ID); subagent_init.py handles those.

Writes .claude/session-data/session-context.json for subagent_init to read.
"""
import json
import os
import re
import subprocess
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "lib"))
from ck_config_utils import (
    find_project_root as get_project_root,
    get_claude_dir, get_project_name, get_sessions_dir, load_ck_config,
)
from hook_logger import HookLogger, strip_ansi
from project_detector import detect_project_type, get_package_manager
from session_utils import ensure_dir
from config_counter import get_summary as get_config_summary

DEV_RULES = "YAGNI · KISS · DRY · Brutal honesty · Challenge assumptions"
_log = HookLogger("session-init")


# ── coding level ──────────────────────────────────────────────────────────────

_LEVEL_NAMES = {-1: "Default", 0: "ELI5", 1: "Junior", 2: "Mid-level", 3: "Senior", 4: "Tech Lead", 5: "God Mode"}
_LEVEL_FILES = {0: "0-eli5.md", 1: "1-junior.md", 2: "2-midlevel.md", 3: "3-senior.md", 4: "4-techlead.md", 5: "5-godmode.md"}
_CODING_LEVEL_RE = re.compile(r"coding.?level|codingLevel", re.IGNORECASE)


def read_language_setting(root: Path) -> str | None:
    """Return a language instruction string from .ck.json language config."""
    ck_path = root / ".ck.json"
    if not ck_path.exists():
        return None
    try:
        cfg = json.loads(ck_path.read_text(encoding="utf-8"))
        lang = cfg.get("language", {})
        if not lang:
            return None
        parts = []
        if conv := lang.get("conversation"):
            parts.append(f"Respond to the user in: {conv}")
        if files := lang.get("files"):
            parts.append(f"Write all file content (code, docs, comments) in: {files}")
        return "\n".join(parts) if parts else None
    except Exception as err:
        _log.warn(f"failed to read language setting: {err}")
        return None


def read_coding_level() -> str | None:
    root = get_project_root()
    if not root:
        return None
    ck_path = root / ".ck.json"
    if not ck_path.exists():
        return None
    try:
        cfg = json.loads(ck_path.read_text(encoding="utf-8"))
        level = int(cfg.get("codingLevel", 5))
        if level not in _LEVEL_NAMES:
            return None
        _log.info(f"Coding level: {level} ({_LEVEL_NAMES[level]})")
        if level == -1:
            return None
        style_file = root / ".claude" / "coding-levels" / _LEVEL_FILES[level]
        if not style_file.exists():
            style_file = get_claude_dir() / "coding-levels" / _LEVEL_FILES[level]
        try:
            return style_file.read_text(encoding="utf-8").strip()
        except FileNotFoundError:
            raise FileNotFoundError(f"Level file not found: {style_file}") from None
    except Exception as err:
        _log.warn(f"failed to read .ck.json: {err}")
        return None


def _strip_coding_level_lines(text: str) -> str:
    return "\n".join(ln for ln in text.splitlines() if not _CODING_LEVEL_RE.search(ln))




# ── session aliases ────────────────────────────────────────────────────────────

def list_aliases(limit: int = 5) -> list[dict]:
    try:
        data = json.loads((get_claude_dir() / "session-aliases.json").read_text(encoding="utf-8"))
        aliases = [{"name": n, **info} for n, info in (data.get("aliases") or {}).items()]
        aliases.sort(key=lambda a: a.get("updatedAt") or a.get("createdAt") or "", reverse=True)
        return aliases[:limit]
    except Exception:
        return []


# ── session context for subagents ─────────────────────────────────────────────

def _get_current_branch() -> str:
    try:
        r = subprocess.run(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            capture_output=True, text=True, timeout=3,
        )
        return r.stdout.strip() if r.returncode == 0 else ""
    except Exception:
        return ""


def write_session_context(sessions_dir: Path) -> None:
    try:
        project = get_project_name() or ""
        branch = _get_current_branch()
        context = {
            "project": project,
            "branch": branch,
            "rules": DEV_RULES,
            "updated_at": datetime.now().isoformat(),
        }
        (sessions_dir / "session-context.json").write_text(
            json.dumps(context, indent=2), encoding="utf-8"
        )
        _log.info(f"Session context written (project={project}, branch={branch})")
    except Exception as e:
        _log.warn(f"could not write session context: {e}")


# ── main ───────────────────────────────────────────────────────────────────────

def main() -> None:
    logger = HookLogger("session-init")

    # Skip for subagents — subagent_init.py provides lighter context for those
    if os.environ.get("CLAUDE_PARENT_SESSION_ID"):
        logger.info("Subagent detected — skipping (handled by subagent-init)")
        sys.exit(0)

    sessions_dir = get_sessions_dir()
    context_parts: list[str] = []

    ensure_dir(sessions_dir)

    # Language setting
    root = get_project_root()
    if root:
        lang_setting = read_language_setting(root)
        if lang_setting:
            context_parts.append(lang_setting)

    # Coding level
    coding_level = read_coding_level()
    if coding_level:
        context_parts.append(coding_level)

    # Previous session state
    if os.environ.get("BENCHMARK_MODE") == "1":
        logger.info("BENCHMARK_MODE — skipping session state")
    else:
        state_file = sessions_dir / ".last-state.md"
        if state_file.exists():
            try:
                state_content = strip_ansi(state_file.read_text(encoding="utf-8").strip())
                if state_content:
                    state_content = _strip_coding_level_lines(state_content)
                    context_parts.append(f"Previous session state:\n{state_content}")
                    logger.info("Previous session state loaded")
            except Exception as e:
                logger.warn(f"could not read session state: {e}")
        else:
            logger.info("No previous session state found")

        # Deliberate handoff written by /ck:handoff — survives across sessions
        handoff_file = sessions_dir / ".last-handoff.md"
        if handoff_file.exists():
            try:
                handoff_content = strip_ansi(handoff_file.read_text(encoding="utf-8").strip())
                if handoff_content:
                    context_parts.append(f"Handoff from previous session:\n{handoff_content}")
                    logger.info("Handoff context loaded")
            except Exception as e:
                logger.warn(f"could not read handoff: {e}")

        # Feature checklist — inject startup gate when feature_list.json exists at project root
        if root:
            feature_list_path = root / "feature_list.json"
            if feature_list_path.exists():
                try:
                    fl = json.loads(feature_list_path.read_text(encoding="utf-8"))
                    features = fl.get("features", [])
                    active = [f for f in features if f.get("status") == "active"]
                    blocked = [f for f in features if f.get("status") == "blocked" and f.get("blocked_by")]
                    total = len(features)
                    if active:
                        def _fmt_active(f):
                            prefix = f"{f['priority']}: " if f.get("priority") else ""
                            return f"  - {prefix}{f.get('id', '?')}: {f.get('title', '')} — verify before starting new work"
                        lines = [_fmt_active(f) for f in active]
                        blocked_lines = [f"  - {f.get('id', '?')}: {f.get('title', '')} (blocked by: {', '.join(f.get('blocked_by', []))})" for f in blocked] if blocked else []
                        checklist = (
                            f"Feature baseline checklist ({total} features tracked):\n"
                            f"  - Read feature_list.json to confirm current feature state\n"
                            f"  - Active feature(s) requiring verification:\n"
                            + "\n".join(lines)
                            + ("\n  - Blocked features:\n" + "\n".join(blocked_lines) if blocked_lines else "")
                            + "\n  - Do not mark any feature passing without running its verification_command"
                        )
                        context_parts.append(checklist)
                        logger.info(f"Feature checklist injected: {len(active)} active, {len(blocked)} blocked")
                    else:
                        blocked_note = f" {len(blocked)} blocked." if blocked else ""
                        context_parts.append(
                            f"Feature baseline: {total} feature(s) tracked, none active.{blocked_note} "
                            f"No verification required before starting work."
                        )
                        logger.info(f"Feature checklist: no active features ({total} tracked, {len(blocked)} blocked)")
                except json.JSONDecodeError as e:
                    logger.warn(f"feature_list.json is malformed — skipping checklist: {e}")
                except Exception as e:
                    logger.warn(f"could not read feature_list.json: {e}")

    # Session aliases (log only)
    aliases = list_aliases(limit=5)
    if aliases:
        logger.info(f"{len(aliases)} session alias(es): {', '.join(a['name'] for a in aliases)}")

    # Package manager and project type (uses lib modules)
    pm = get_package_manager()
    logger.info(f"Package manager: {pm['name']} ({pm['source']})")

    project_info = detect_project_type()
    parts = []
    if project_info["languages"]:
        parts.append(f"languages: {', '.join(project_info['languages'])}")
    if project_info["frameworks"]:
        parts.append(f"frameworks: {', '.join(project_info['frameworks'])}")
    if parts:
        logger.info(f"Project: {'; '.join(parts)}")
        context_parts.append(f"Project type: {json.dumps(project_info)}")
    else:
        logger.info("No specific project type detected")

    # Config summary
    root = get_project_root()
    if root:
        counts = get_config_summary(root / ".claude")
        logger.info(f"Config: {counts['hooks']} hooks, {counts['skills']} skills, {counts['agents']} agents")

    # Write compact session context for subagent_init
    write_session_context(sessions_dir)
    logger.perf()

    payload = json.dumps({
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": "\n\n".join(context_parts),
        }
    })
    sys.stdout.write(payload)
    sys.stdout.flush()


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        _log.error(str(e))
        sys.exit(0)
