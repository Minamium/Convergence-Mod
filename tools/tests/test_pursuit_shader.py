"""Narrow opt-in and exported-shader contracts; not a claim of game rendering."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("shader_exports", ROOT / "tools/compile_shaders.py")
exports = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exports)


class PursuitShader(unittest.TestCase):
    def test_only_main_pursuit_opts_in(self):
        text = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs").read_text()
        self.assertIn("e.MainSequence && v.Kind == FirstSeveranceAttackKind.PursuitPrism", text)
        self.assertIn("Accept(volley, tick, true)", text)
        self.assertIn("Accept(cast, tick, false)", text)
        final = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceFinalBeamVisuals.cs").read_text()
        self.assertIn("emissions.DrawPrismRay", final)
        self.assertNotIn("pursuit.Draw", final)

    def test_native_pass_is_client_only_and_preserves_world_width(self):
        text = (ROOT / "Client/Encounters/FirstSeverance/FirstSeverancePursuitBeamVisuals.cs").read_text()
        self.assertLess(text.index("Main.dedServ"), text.index("ShaderManager.GetShader"))
        self.assertIn("float live = active ? 1 : 0", text)
        self.assertIn("Quad(origin, direction, ray.Length, ray.HalfWidth)", text)
        self.assertIn("Main.GameViewMatrix.TransformationMatrix", text)
        self.assertNotIn("UIScale", text)
        self.assertIn("finally", text)
        self.assertNotIn("Main.rand", text)

    def test_current_exports_match(self):
        exports.verify()

    def test_stale_source_or_export_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            source, binary, manifest = folder / "Test.fx", folder / "Test.fxc", folder / "compiled.json"
            source.write_text("original")
            binary.write_bytes(b"compiled")
            record = {"files": {source.name: {"source_sha256": exports.digest(source), "output_sha256": exports.digest(binary)}}}
            manifest.write_text(json.dumps(record))
            with patch.object(exports, "SHADERS", folder), patch.object(exports, "MANIFEST", manifest):
                exports.verify()
                source.write_text("changed")
                with self.assertRaises(ValueError): exports.verify()
                source.write_text("original")
                binary.write_bytes(b"stale")
                with self.assertRaises(ValueError): exports.verify()


if __name__ == "__main__":
    unittest.main()
