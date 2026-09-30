"""Contract tests for the Claude Code adapters around the canonical agent guidance."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("repository_checks", ROOT / "tools/repository_checks.py")
checks = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checks)

DESCRIPTION = "Review demo behavior. Use for demos; skip wording."


def skill(name, description=DESCRIPTION, body=""):
    return f"---\nname: {name}\ndescription: {description}\n---\n\n{body}\n"


def valid_layout():
    return {
        "CLAUDE.md": "@AGENTS.md\n\n# Claude Code\n",
        ".agents/skills/demo/SKILL.md": skill("demo", body="Canonical instructions."),
        ".claude/skills/demo/SKILL.md": skill("demo", body="Read `.agents/skills/demo/SKILL.md`."),
    }


class ClaudeCodeAdapterTests(unittest.TestCase):
    def errors_for(self, files):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for name, text in files.items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(text, encoding="utf-8", newline="\n")
            errors = []
            with patch.object(checks, "ROOT", root):
                checks.check_claude_code_adapters(errors)
            return errors

    def assert_single_error(self, files, fragment):
        errors = self.errors_for(files)
        self.assertEqual(1, len(errors), errors)
        self.assertIn(fragment, errors[0])

    def test_repository_adapters_match_canonical_skills(self):
        errors = []
        checks.check_claude_code_adapters(errors)
        self.assertEqual([], errors)

    def test_matching_adapter_passes(self):
        self.assertEqual([], self.errors_for(valid_layout()))

    def test_claude_md_must_import_agents_md(self):
        files = valid_layout()
        files["CLAUDE.md"] = "Read `@AGENTS.md` first.\n"
        self.assert_single_error(files, "'@AGENTS.md'")

    def test_description_drift_is_rejected(self):
        files = valid_layout()
        files[".agents/skills/demo/SKILL.md"] = skill("demo", description="Changed canonical description.")
        self.assert_single_error(files, "description must match")

    def test_adapter_name_must_match_directory(self):
        files = valid_layout()
        files[".claude/skills/demo/SKILL.md"] = skill("other", body="Read `.agents/skills/demo/SKILL.md`.")
        self.assert_single_error(files, "name must match")

    def test_adapter_must_point_to_its_canonical_skill(self):
        files = valid_layout()
        files[".claude/skills/demo/SKILL.md"] = skill("demo", body="Follow the repository rules.")
        self.assert_single_error(files, "must point to .agents/skills/demo/SKILL.md")

    def test_missing_and_orphan_adapters_are_rejected(self):
        files = valid_layout()
        files[".agents/skills/extra/SKILL.md"] = skill("extra")
        files[".claude/skills/stray/SKILL.md"] = skill("stray", body="Read `.agents/skills/stray/SKILL.md`.")
        errors = self.errors_for(files)
        self.assertEqual(2, len(errors), errors)
        self.assertTrue(any("no Claude Code adapter: .claude/skills/extra" in error for error in errors))
        self.assertTrue(any("no canonical Skill: .claude/skills/stray" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
