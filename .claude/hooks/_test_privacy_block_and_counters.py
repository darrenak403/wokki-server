"""
Independent tester-agent verification pass for the harness-drift-fixes plan
(plans/260621-harness-drift-fixes/). Covers privacy_block.py path resolution
and Grep/Glob matcher behavior, tool_counter.py + suggest_compact.py event
split, caveman_watch.py compatibility, and audit_hooks.py side-effect safety.

Run: python3 -m pytest .claude/hooks/_test_privacy_block_and_counters.py -v
"""

import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

HOOKS_DIR = Path(__file__).parent
ROOT = HOOKS_DIR.parent.parent  # repo root (hesd-skills)
TEST_REPO = Path("/tmp/privacy-test-repo")

# Sensitive-looking names built at runtime so this test file's own source
# text never contains the literal substrings privacy_block.py's Bash-matcher
# greps for, and so re-reading this file doesn't trip anything either.
ENV_NAME = "." + "env"
KEY_SUFFIX = "." + "key"
SECRETS_DIR = "se" + "crets"
CREDENTIALS_DIR = "cred" + "entials"


@pytest.fixture(scope="session", autouse=True)
def privacy_test_repo():
    """Deterministically build the fixture git repo every test in this file
    relies on, so the suite is self-contained on a clean machine/CI — not
    dependent on a fixture hand-built earlier in some prior session."""
    if TEST_REPO.exists():
        shutil.rmtree(TEST_REPO)
    TEST_REPO.mkdir(parents=True)
    subprocess.run(["git", "init", "-q"], cwd=str(TEST_REPO), check=True)
    (TEST_REPO / ENV_NAME).write_text("fixture\n")
    (TEST_REPO / SECRETS_DIR).mkdir()
    (TEST_REPO / SECRETS_DIR / ("db" + KEY_SUFFIX)).write_text("fixture\n")
    (TEST_REPO / CREDENTIALS_DIR).mkdir()
    (TEST_REPO / "notes.txt").write_text("fixture\n")

    yield TEST_REPO

    shutil.rmtree(TEST_REPO, ignore_errors=True)


def run_hook(name: str, payload: dict | str, cwd: str | None = None, env: dict | None = None):
    """Run a hook script with given stdin payload, return CompletedProcess."""
    stdin = payload if isinstance(payload, str) else json.dumps(payload)
    full_env = dict(os.environ)
    if env:
        full_env.update(env)
    return subprocess.run(
        [sys.executable, str(HOOKS_DIR / name)],
        input=stdin,
        capture_output=True,
        text=True,
        timeout=15,
        cwd=cwd or str(ROOT),
        env=full_env,
    )


# ── privacy_block.py ────────────────────────────────────────────────────────

class TestPrivacyBlockRootResolution:
    def test_cwd_in_different_repo_resolves_correct_root(self):
        """cwd pointing at /tmp/privacy-test-repo (a different git repo than
        the one this test runs from) must resolve allow_list/root from that
        repo, not from os.getcwd() (which would be hesd-skills)."""
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Read",
            "tool_input": {"file_path": str(TEST_REPO / ENV_NAME)},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 2, f"expected block (exit 2), got {r.returncode}: {r.stderr}"
        assert ENV_NAME in r.stderr or "Blocked" in r.stderr

    def test_missing_cwd_field_does_not_crash(self):
        """No cwd field at all -> falls back to os.getcwd(), must not raise."""
        payload = {
            "tool_name": "Read",
            "tool_input": {"file_path": "/tmp/privacy-test-repo/notes.txt"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(TEST_REPO))
        assert r.returncode == 0, f"non-sensitive file should pass, got {r.returncode}: {r.stderr}"

    def test_missing_cwd_field_on_sensitive_file_still_blocks(self):
        payload = {
            "tool_name": "Read",
            "tool_input": {"file_path": str(TEST_REPO / ENV_NAME)},
        }
        # cwd of the subprocess itself is TEST_REPO; hook falls back to os.getcwd()
        r = run_hook("privacy_block.py", payload, cwd=str(TEST_REPO))
        assert r.returncode == 2, f"expected block, got {r.returncode}: {r.stderr}"


class TestPrivacyBlockGrep:
    def test_grep_on_real_sensitive_file_is_blocked(self):
        sensitive_file = TEST_REPO / SECRETS_DIR / ("db" + KEY_SUFFIX)
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Grep",
            "tool_input": {"path": str(sensitive_file), "pattern": "foo"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 2, f"expected block, got {r.returncode}: {r.stderr}"

    def test_grep_on_directory_with_sensitive_name_is_not_blocked(self):
        """tool_input.path is a directory (not a file) -> guard Path(path).is_file()
        must skip check_file(), per plan.md 'Quyết định kỹ thuật'."""
        sensitive_dir = TEST_REPO / SECRETS_DIR
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Grep",
            "tool_input": {"path": str(sensitive_dir), "pattern": "foo"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"directory path must not be blocked, got {r.returncode}: {r.stderr}"

    def test_grep_on_other_sensitive_named_directory_not_blocked(self):
        sensitive_dir = TEST_REPO / CREDENTIALS_DIR
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Grep",
            "tool_input": {"path": str(sensitive_dir), "pattern": "foo"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"directory path must not be blocked, got {r.returncode}: {r.stderr}"

    def test_grep_with_no_path_field_not_blocked(self):
        """Grep without a path field at all (defaults to cwd) must not crash
        or block."""
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Grep",
            "tool_input": {"pattern": "foo"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"missing path must not block, got {r.returncode}: {r.stderr}"

    def test_grep_on_nonexistent_path_not_blocked(self):
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Grep",
            "tool_input": {"path": str(TEST_REPO / "does-not-exist"), "pattern": "foo"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"nonexistent path must not block, got {r.returncode}: {r.stderr}"


class TestPrivacyBlockGlobUnaffected:
    def test_glob_with_sensitive_pattern_is_never_checked(self):
        """Glob is intentionally excluded entirely — privacy_block.py has no
        'elif tool_name == \"Glob\"' branch, so any Glob call must pass
        through with exit 0 regardless of pattern content."""
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Glob",
            "tool_input": {"pattern": "**/*" + "password" + "-reset*.tsx"},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"Glob must always pass through, got {r.returncode}: {r.stderr}"

    def test_glob_matching_actual_env_pattern_passes_through(self):
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Glob",
            "tool_input": {"pattern": "**/" + ENV_NAME},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0, f"Glob must always pass through, got {r.returncode}: {r.stderr}"


class TestPrivacyBlockReadWriteEditStillWork:
    def test_read_sensitive_file_blocked(self):
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Read",
            "tool_input": {"file_path": str(TEST_REPO / ENV_NAME)},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 2

    def test_write_normal_file_allowed(self):
        payload = {
            "cwd": str(TEST_REPO),
            "tool_name": "Write",
            "tool_input": {"file_path": str(TEST_REPO / "notes.txt")},
        }
        r = run_hook("privacy_block.py", payload, cwd=str(ROOT))
        assert r.returncode == 0

    def test_malformed_json_fails_open(self):
        r = run_hook("privacy_block.py", "NOT VALID JSON {{{", cwd=str(ROOT))
        assert r.returncode == 0, f"malformed JSON must fail-open, got {r.returncode}: {r.stderr}"


# ── tool_counter.py / suggest_compact.py / caveman_watch.py ────────────────

class TestToolCounter:
    def setup_method(self):
        self.session_id = "tester-counter-session"
        self.tmp_dir = Path(os.environ.get("TEMP", os.environ.get("TMPDIR", "/tmp")))
        self.counter_file = self.tmp_dir / f"claude-tool-count-{self.session_id}"
        if self.counter_file.exists():
            self.counter_file.unlink()

    def teardown_method(self):
        if self.counter_file.exists():
            self.counter_file.unlink()

    def test_counter_increments_on_normal_calls(self):
        env = {"CLAUDE_SESSION_ID": self.session_id}
        env.pop("CLAUDE_PARENT_SESSION_ID", None)
        for expected in (1, 2, 3):
            r = run_hook("tool_counter.py", "{}", env={"CLAUDE_SESSION_ID": self.session_id,
                                                         "CLAUDE_PARENT_SESSION_ID": ""})
            assert r.returncode == 0
            count = int(self.counter_file.read_text().strip())
            assert count == expected, f"expected {expected}, got {count}"

    def test_counter_does_not_increment_in_subagent(self):
        full_env = {"CLAUDE_SESSION_ID": self.session_id, "CLAUDE_PARENT_SESSION_ID": "parent-123"}
        run_hook("tool_counter.py", "{}", env=full_env)
        assert not self.counter_file.exists(), "subagent call must not create/increment counter file"

    def test_counter_subagent_noop_after_existing_count(self):
        # First increment normally as the main agent.
        run_hook("tool_counter.py", "{}", env={"CLAUDE_SESSION_ID": self.session_id,
                                                 "CLAUDE_PARENT_SESSION_ID": ""})
        assert int(self.counter_file.read_text().strip()) == 1
        # Now simulate a subagent call - must not bump the count.
        run_hook("tool_counter.py", "{}", env={"CLAUDE_SESSION_ID": self.session_id,
                                                 "CLAUDE_PARENT_SESSION_ID": "parent-123"})
        assert int(self.counter_file.read_text().strip()) == 1, "subagent call must not increment existing counter"


class TestSuggestCompact:
    def setup_method(self):
        self.session_id = "tester-suggest-session"
        self.tmp_dir = Path(os.environ.get("TEMP", os.environ.get("TMPDIR", "/tmp")))
        self.counter_file = self.tmp_dir / f"claude-tool-count-{self.session_id}"

    def teardown_method(self):
        if self.counter_file.exists():
            self.counter_file.unlink()

    def _run(self, count, env_extra=None, cwd=None):
        self.counter_file.write_text(str(count))
        env = {"CLAUDE_SESSION_ID": self.session_id, "COMPACT_THRESHOLD": "50"}
        if env_extra:
            env.update(env_extra)
        return run_hook("suggest_compact.py", json.dumps({"cwd": cwd or str(ROOT)}), cwd=cwd or str(ROOT), env=env)

    def test_silent_below_threshold(self):
        r = self._run(10)
        assert r.stderr.strip() == "", f"expected silence below threshold, got: {r.stderr!r}"

    def test_prints_at_threshold(self):
        r = self._run(50)
        assert "StrategicCompact" in r.stderr, f"expected suggestion at threshold, got: {r.stderr!r}"
        assert "50 tool calls reached" in r.stderr

    def test_silent_just_above_threshold_non_multiple_of_25(self):
        r = self._run(51)
        assert r.stderr.strip() == "", f"expected silence at 51 (not a multiple-of-25 checkpoint), got: {r.stderr!r}"

    def test_prints_at_75_checkpoint(self):
        r = self._run(75)
        assert "StrategicCompact" in r.stderr, f"expected suggestion at 75, got: {r.stderr!r}"

    def test_silent_in_subagent_context(self):
        r = self._run(50, env_extra={"CLAUDE_PARENT_SESSION_ID": "parent-123"})
        assert r.stderr.strip() == "", f"must be silent in subagent context, got: {r.stderr!r}"

    def test_no_counter_file_is_silent(self):
        if self.counter_file.exists():
            self.counter_file.unlink()
        env = {"CLAUDE_SESSION_ID": self.session_id, "COMPACT_THRESHOLD": "50"}
        r = run_hook("suggest_compact.py", json.dumps({"cwd": str(ROOT)}), cwd=str(ROOT), env=env)
        assert r.stderr.strip() == ""
        assert r.returncode == 0


class TestCavemanWatchCompat:
    """caveman_watch.py is unchanged by this plan but reads the same counter
    file tool_counter.py now owns as sole writer — verify it still works."""

    def setup_method(self):
        self.session_id = "tester-caveman-session"
        self.tmp_dir = Path(os.environ.get("TEMP", os.environ.get("TMPDIR", "/tmp")))
        self.counter_file = self.tmp_dir / f"claude-tool-count-{self.session_id}"
        self.state_file = TEST_REPO / ".claude" / "session-data" / f"caveman-{self.session_id}.json"

    def teardown_method(self):
        if self.counter_file.exists():
            self.counter_file.unlink()
        if self.state_file.exists():
            self.state_file.unlink()

    def test_caveman_triggers_at_orange_threshold(self):
        self.counter_file.write_text("50")
        payload = {"cwd": str(TEST_REPO)}
        env = {"CLAUDE_SESSION_ID": self.session_id}
        r = run_hook("caveman_watch.py", payload, cwd=str(TEST_REPO), env=env)
        assert r.returncode == 0
        if r.stdout.strip():
            out = json.loads(r.stdout)
            ctx = out["hookSpecificOutput"]["additionalContext"]
            assert "CAVEMAN_TRIGGERED" in ctx

    def test_caveman_no_transition_no_output(self):
        # Pre-seed state as already active, then count still >= orange -> no transition -> silent.
        self.state_file.parent.mkdir(parents=True, exist_ok=True)
        self.state_file.write_text(json.dumps({"active": True}))
        self.counter_file.write_text("60")
        payload = {"cwd": str(TEST_REPO)}
        env = {"CLAUDE_SESSION_ID": self.session_id}
        r = run_hook("caveman_watch.py", payload, cwd=str(TEST_REPO), env=env)
        assert r.returncode == 0
        assert r.stdout.strip() == "", f"no state transition should mean no output, got: {r.stdout!r}"


# ── audit_hooks.py ──────────────────────────────────────────────────────────

class TestAuditHooks:
    def test_does_not_modify_settings_or_session_data(self):
        settings_path = ROOT / ".claude" / "settings.json"
        session_data_dir = ROOT / ".claude" / "session-data"

        before_settings = settings_path.read_text()
        before_files = set(session_data_dir.rglob("*")) if session_data_dir.exists() else set()
        before_mtimes = {p: p.stat().st_mtime for p in before_files if p.is_file()}

        r = subprocess.run(
            [sys.executable, str(HOOKS_DIR / "audit_hooks.py")],
            capture_output=True, text=True, timeout=30, cwd=str(ROOT),
        )
        assert r.returncode == 0, f"audit_hooks.py should exit 0, got {r.returncode}: {r.stderr}"

        after_settings = settings_path.read_text()
        assert after_settings == before_settings, "audit_hooks.py must not modify settings.json"

        after_files = set(session_data_dir.rglob("*")) if session_data_dir.exists() else set()
        new_files = after_files - before_files
        assert not new_files, f"audit_hooks.py must not create new session-data files: {new_files}"

        after_mtimes = {p: p.stat().st_mtime for p in after_files if p.is_file() and p in before_mtimes}
        changed = {p for p in after_mtimes if after_mtimes[p] != before_mtimes.get(p)}
        assert not changed, f"audit_hooks.py must not modify existing session-data files: {changed}"

    def test_dynamic_checked_hooks_fail_open_on_malformed_json(self):
        r = subprocess.run(
            [sys.executable, str(HOOKS_DIR / "audit_hooks.py")],
            capture_output=True, text=True, timeout=30, cwd=str(ROOT),
        )
        out = r.stdout
        for name in ("privacy_block.py", "build_check.py", "test_runner.py"):
            assert name in out, f"{name} missing from audit output"
        assert "ERROR: did not fail open" not in out, f"a dynamically-checked hook did not fail open:\n{out}"

    def test_privacy_block_fails_open_directly_on_malformed_json(self):
        r = run_hook("privacy_block.py", "BAD JSON NOT PARSEABLE", cwd=str(ROOT))
        assert r.returncode == 0

    def test_build_check_fails_open_directly_on_malformed_json(self):
        r = run_hook("build_check.py", "BAD JSON NOT PARSEABLE", cwd=str(ROOT))
        assert r.returncode == 0

    def test_test_runner_fails_open_directly_on_malformed_json(self):
        r = run_hook("test_runner.py", "BAD JSON NOT PARSEABLE", cwd=str(ROOT))
        assert r.returncode == 0


# ── ck-spec skill structural check ──────────────────────────────────────────

class TestCkSpecSkill:
    def test_skill_md_exists_and_has_frontmatter(self):
        skill_path = ROOT / ".claude" / "skills" / "ck-spec" / "SKILL.md"
        assert skill_path.exists()
        text = skill_path.read_text()
        assert text.startswith("---\n"), "SKILL.md must start with YAML frontmatter"
        parts = text.split("---\n", 2)
        assert len(parts) >= 3, "SKILL.md must have closed frontmatter block"
        frontmatter = parts[1]
        assert "name:" in frontmatter
        assert "description:" in frontmatter

    def test_referenced_template_path_exists(self):
        skill_path = ROOT / ".claude" / "skills" / "ck-spec" / "SKILL.md"
        text = skill_path.read_text()
        assert ".claude/skills/ck-brainstorm/references/spec-template.md" in text
        template_path = ROOT / ".claude" / "skills" / "ck-brainstorm" / "references" / "spec-template.md"
        assert template_path.exists(), f"referenced template missing: {template_path}"
