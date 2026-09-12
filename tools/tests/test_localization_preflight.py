"""The local build must not overwrite a package after a localization failure."""
import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("convergence_dev", Path(__file__).parents[1] / "dev.py")
dev = importlib.util.module_from_spec(spec)
spec.loader.exec_module(dev)


class LocalizationPreflight(unittest.TestCase):
    def test_installed_parser_receives_the_selected_install_without_shell(self):
        with patch.object(dev.subprocess, "run") as run:
            dev.validate_localization({"tMLSteamPath": "selected tML installation"})
        args, kwargs = run.call_args
        self.assertEqual("selected tML installation", args[0][-1])
        self.assertEqual("check-localization.ps1", Path(args[0][3]).name)
        self.assertTrue(kwargs["check"])
        self.assertNotIn("shell", kwargs)

    def test_rejected_hjson_stops_before_compiler_and_package_backup(self):
        with patch.object(sys, "argv", ["dev.py", "build", "--native"]), \
             patch.object(dev, "environment", return_value={"TModLoaderSavePath": "profile", "tMLSteamPath": "install"}), \
             patch.object(dev, "source_record", return_value={}), \
             patch.object(Path, "is_dir", return_value=True), \
             patch.object(Path, "mkdir") as mkdir, \
             patch.object(dev.subprocess, "run", side_effect=subprocess.CalledProcessError(1, ["Hjson check"])), \
             patch.object(dev.subprocess, "Popen") as compiler, \
             contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(1, dev.main())
        compiler.assert_not_called()
        mkdir.assert_not_called()


if __name__ == "__main__":
    unittest.main()
