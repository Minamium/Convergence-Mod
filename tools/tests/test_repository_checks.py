"""Contract tests for repository policy checks that guard the project direction documents."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]

_spec = importlib.util.spec_from_file_location("repository_checks", ROOT / "tools/repository_checks.py")
checks = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(checks)

BRIEF = """---
doc_id: project.brief
---

# Project Brief

## Encounter roster

| Encounter | Spec |
|---|---|
| One | [spec](encounters/one/ENCOUNTER_SPEC.md) |

## Setting and presentation

See [two](encounters/two/ENCOUNTER_SPEC.md), which is outside the roster.
"""


class EncounterRosterTests(unittest.TestCase):
    def test_current_brief_lists_every_encounter_folder(self):
        errors = []
        checks.check_encounter_roster(errors)
        self.assertEqual(errors, [])

    def test_linked_encounter_passes(self):
        self.assertEqual(checks.encounter_roster_errors(BRIEF, ["one"]), [])

    def test_encounter_linked_only_outside_the_roster_is_reported(self):
        errors = checks.encounter_roster_errors(BRIEF, ["one", "two"])
        self.assertEqual(len(errors), 1)
        self.assertIn("docs/encounters/two/", errors[0])

    def test_missing_roster_section_is_reported(self):
        errors = checks.encounter_roster_errors("# Project Brief\n\n## Other\n", ["one"])
        self.assertEqual(errors, ["docs/PROJECT_BRIEF.md has no '## Encounter roster' section"])

    def test_direction_documents_are_required(self):
        self.assertIn("docs/PROJECT_BRIEF.md", checks.REQUIRED_PATHS)
        self.assertIn("docs/MILESTONES.md", checks.REQUIRED_PATHS)


if __name__ == "__main__":
    unittest.main()
