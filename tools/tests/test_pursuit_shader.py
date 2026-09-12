"""Doll Raid render integration/export guards; GPU/game checks are separate."""
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
    def test_pursuit_and_final_keep_their_existing_timing_adapters(self):
        text = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceEmissionVisuals.cs").read_text()
        self.assertIn("e.MainSequence && v.Kind == FirstSeveranceAttackKind.PursuitPrism", text)
        self.assertIn("Accept(volley, tick, true)", text)
        self.assertIn("Accept(cast, tick, false)", text)
        final = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceFinalBeamVisuals.cs").read_text()
        self.assertIn("emissions.DrawPrismRay", final)
        self.assertNotIn("pursuit.Draw", final)

    def test_native_pass_is_client_only_and_preserves_world_width(self):
        root = ROOT / "Client/Encounters/FirstSeverance"
        text = (root / "FirstSeveranceRaidVfx.cs").read_text()
        self.assertLess(text.index("Main.dedServ"), text.index("ShaderManager.GetShader"))
        adapter = (root / "FirstSeverancePursuitBeamVisuals.cs").read_text()
        self.assertIn("ray.Length,ray.HalfWidth", adapter)
        self.assertIn("FirstSeveranceBeamIgnition.At(ray,clock-fire)", adapter)
        self.assertIn("if (live == 0) halfWidth = Math.Min(halfWidth, 3f)", text)
        self.assertIn("Quad(c.Origin,c.Direction,c.Length,c.HalfWidth)", text)
        self.assertIn("Main.GameViewMatrix.TransformationMatrix", text)
        self.assertNotIn("UIScale", text)
        self.assertIn("finally", text)
        self.assertNotIn("Main.rand.", text)
        for slot in (1, 2, 3):
            self.assertIn(f"device.Textures[{slot}]=t{slot}", text)
            self.assertIn(f"device.SamplerStates[{slot}]=s{slot}", text)
        self.assertIn('live > 0 ? "AutoloadPass" : "ForecastPass"', text)
        self.assertIn("!confined && live > 0", text)
        self.assertIn("if(count==commands.Length) Flush(batch)", text)
        self.assertNotIn("new Texture2D", text)

    def test_all_named_passes_exist_and_frame_is_retired(self):
        root = ROOT / "Client/Encounters/FirstSeverance"
        renderer = (root / "FirstSeveranceRaidVfx.cs").read_text()
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/RaidEnergy.fx").read_text()
        for name in ("AutoloadPass", "ForecastPass", "CoronaPass", "MouthPass",
                     "OrbPass", "WakePass", "PressurePass", "RiftPass", "FlarePass"):
            self.assertIn(f'"{name}"', renderer)
            self.assertIn(f"pass {name} {{", shader)
        owner = (root / "FirstSeverancePrototypePresentation.cs").read_text()
        self.assertIn("FirstSeveranceRaidVfx.BeginFrame()", owner)
        self.assertIn("FirstSeveranceRaidVfx.EndFrame(batch)", owner)
        self.assertIn("FirstSeveranceRaidVfx.Reset()", owner)

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
