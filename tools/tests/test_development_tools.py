"""Contract tests for the verifier command plan and source manifest, with no builds/network."""
import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


verify = module("verify_repo", ROOT / ".agents/skills/develop-convergence-raids/scripts/verify_repo.py")
dev = module("dev", ROOT / "tools/dev.py")


class VerifierTests(unittest.TestCase):
    def invoke(self, *flags, result=None):
        with patch.object(sys, "argv", ["verify_repo", str(ROOT), *flags]), \
                patch.object(verify, "run", side_effect=result, return_value=0) as run, \
                contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            code = verify.main()
        return code, [entry.args[0] for entry in run.call_args_list]

    def test_audit_excludes_every_mutating_option(self):
        for flag in ("--write-catalog", "--with-domain", "--with-dotnet", "--with-codec"):
            with self.subTest(flag=flag), self.assertRaises(SystemExit) as error:
                self.invoke("--audit-only", flag)
            self.assertEqual(2, error.exception.code)

    def test_aggregation_runs_each_selected_step_once(self):
        code, calls = self.invoke("--write-catalog", "--with-domain", "--with-dotnet", "--with-codec")
        self.assertEqual(0, code)
        self.assertEqual(7, len(calls))
        self.assertEqual(["--write", "--check"], [calls[0][-1], calls[1][-1]])
        self.assertEqual("run", calls[4][1])
        self.assertEqual(["dotnet", "build", "ConvergenceMod.csproj"], calls[5])
        self.assertEqual("bin/Debug/net8.0/Convergence.dll", calls[6][-1])

    def test_codec_reuses_domain_build(self):
        _, calls = self.invoke("--with-domain", "--with-codec")
        self.assertEqual(5, len(calls))
        self.assertEqual("run", calls[3][1])
        self.assertTrue(calls[-1][-1].endswith("Convergence.DomainTests.dll"))

    def test_codec_alone_compiles_linked_sources_without_running_domain_twice(self):
        _, calls = self.invoke("--with-codec")
        self.assertEqual(5, len(calls))
        self.assertEqual("build", calls[3][1])
        self.assertEqual("pwsh", calls[4][0])

    def test_failed_stage_stops_and_preserves_exit_code(self):
        for index in range(7):
            with self.subTest(stage=index):
                code, calls = self.invoke("--write-catalog", "--with-domain", "--with-dotnet", "--with-codec",
                                          result=[0] * index + [23])
                self.assertEqual(23, code)
                self.assertEqual(index + 1, len(calls))

    def test_missing_executable_is_explicit(self):
        code, calls = self.invoke(result=FileNotFoundError(2, "not found", "python"))
        self.assertEqual(127, code)
        self.assertEqual(1, len(calls))

    def test_missing_repository_is_not_success(self):
        with tempfile.TemporaryDirectory() as folder, patch.object(sys, "argv", ["verify_repo", folder]), \
                patch.object(verify, "run") as run, contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(2, verify.main())
            run.assert_not_called()


class SourceIdentityTests(unittest.TestCase):
    def test_manifest_covers_content_changes_and_tracked_deletion(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            asset = root / "sample.txt"
            asset.write_text("before", encoding="utf-8")
            def git(*command):
                if "ls-files" in command: return "sample.txt\0deleted.txt\0"
                if "rev-parse" in command: return "a" * 40
                if "branch" in command: return "test"
                return " M sample.txt\n D deleted.txt"
            with patch.object(dev, "ROOT", root), patch.object(dev, "output", side_effect=git):
                before = dev.source_record()
                self.assertEqual(before, dev.source_record())
                self.assertIsNone(before["files"]["deleted.txt"])
                asset.write_text("after", encoding="utf-8")
                self.assertNotEqual(before["source_sha256"], dev.source_record()["source_sha256"])


if __name__ == "__main__":
    unittest.main()
