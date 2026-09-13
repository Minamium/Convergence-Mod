"""Doll Raid render integration/export guards; GPU/game checks are separate."""
import importlib.util
import hashlib
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
        self.assertIn("halfWidth = Math.Min(halfWidth, 3f)", text)
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
                     "OrbPass", "WakePass", "PressurePass", "RiftPass", "FlarePass", "RibbonPass", "ForecastDustPass"):
            self.assertIn(f'"{name}"', renderer)
            self.assertIn(f"pass {name} {{", shader)
        owner = (root / "FirstSeverancePrototypePresentation.cs").read_text()
        self.assertIn("FirstSeveranceRaidVfx.BeginFrame()", owner)
        self.assertIn("FirstSeveranceRaidVfx.EndFrame(batch)", owner)
        self.assertIn("FirstSeveranceRaidVfx.Reset()", owner)

    def test_sparse_forecast_keeps_future_width_before_axis_clamp(self):
        text = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs").read_text()
        self.assertLess(text.index('"ForecastDustPass"'), text.index("halfWidth = Math.Min"))
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/RaidEnergy.fx").read_text()
        dust = shader.split("float4 ForecastDust(")[1].split("float4 Corona(")[0]
        self.assertIn("i.uv.x*shape.x,(i.uv.y*2-1)*shape.y", dust)
        self.assertIn("light*occupancy*twinkle*fade*signal.z,0", dust)
        self.assertIn("lerp(.22,.4,shape.w)", dust)

    def test_all_beam_families_route_to_shared_material(self):
        root = ROOT / "Client/Encounters/FirstSeverance"
        for name in ("FirstSeverancePursuitBeamVisuals.cs", "FirstSeveranceBeamMaterial.cs",
                     "FirstSeveranceImpalingSwordVisuals.cs", "FirstSeveranceScoreVisuals.cs"):
            self.assertIn("FirstSeveranceRaidVfx.Beam", (root / name).read_text())
        emission = (root / "FirstSeveranceEmissionVisuals.cs").read_text()
        self.assertIn("FirstSeveranceBeamMaterial.Flow", emission)
        stage = (root / "FirstSeveranceStageVisuals.cs").read_text()
        self.assertIn("FirstSeveranceBeamMaterial.SingleForecast", stage)
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/RaidEnergy.fx").read_text()
        self.assertIn("Current(x,span,3100,shape.z)", shader)
        self.assertIn("Current(x,span*1.41,2160,shape.z+.43)", shader)

    def test_grid_ribbon_uses_shared_profile_and_stable_packet_coordinates(self):
        root = ROOT / "Client/Encounters/FirstSeverance"
        renderer = (root / "FirstSeveranceRaidVfx.cs").read_text()
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/RaidEnergy.fx").read_text()
        stage = (root / "FirstSeveranceStageVisuals.cs").read_text()
        self.assertIn("grid.PulseAt(line, tick)", stage)
        self.assertIn("FirstSeveranceGridPulse.TailFraction", renderer)
        self.assertIn("FirstSeveranceGridPulse.HeadFraction", renderer)
        self.assertIn("seedOrigin ?? origin", renderer)
        self.assertIn('TrySetParameter("pulse",c.Pulse)', renderer)
        self.assertIn('TrySetParameter("flowOffset",c.FlowOffset)', renderer)
        self.assertIn("i.uv.x*shape.x+flowOffset", shader)
        self.assertIn("Smoother(u/pulse.z)*Smoother((1-u)/pulse.w)", shader)

    def test_current_exports_match(self):
        exports.verify()

    def test_portal_passes_are_opt_in_and_keep_lattice_material(self):
        renderer = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs").read_text()
        portal = (ROOT / "Assets/AutoloadedEffects/Shaders/PortalBeam.fx").read_text()
        self.assertIn("if (double.IsFinite(fireAge))", renderer)
        self.assertIn("ManagedShader shader=c.Portal?portal:legacy", renderer)
        for name in ("AutoloadPass", "PortalForecastPass", "PortalCoronaPass", "PortalMouthPass"):
            self.assertIn(f'"{name}"', renderer)
            self.assertIn(f"pass {name} {{", portal)
        # Accepted 0.2.71 lattice/untimed-verdict suite remains byte-identical;
        # a future deliberate lattice change must explicitly update this guard.
        for name, digest in {
            "RaidEnergy.fx": "924c0c25abdd983ed7e94f4e2880a015f4afa07fcf5ac4c16425f7a1d6c35fb8",
            "RaidEnergy.fxc": "299821ce56aca68b658a20ec7fd126d575d13022b74bf932db5968156062e9c7",
        }.items():
            self.assertEqual(hashlib.sha256((ROOT / "Assets/AutoloadedEffects/Shaders" / name).read_bytes()).hexdigest(), digest)
        stage = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceStageVisuals.cs").read_text()
        lattice = stage.split("for (int line = 0; line < grid.Rays.Count; line++)")[1].split("foreach (var full in grid.CoreBeams)")[0]
        self.assertIn("Arrive(tick - start, 2), color, reduced);", lattice)
        self.assertNotIn("fireAge", lattice)
        self.assertIn("grid.PulseAt(line, tick)", lattice)

    def test_portal_closure_uses_damage_end_not_fire_or_charge(self):
        renderer = (ROOT / "Client/Encounters/FirstSeverance/FirstSeveranceRaidVfx.cs").read_text()
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/PortalBeam.fx").read_text()
        self.assertIn("Math.Clamp(age-endAge,0,60)", renderer)
        self.assertIn("float Closure() { return Ease(ceremony.y/9); }", shader)
        self.assertIn("float TailFade() { return 1-Ease(ceremony.y/13); }", shader)
        self.assertIn("float WarningDip() { return 1-.84*Ease((ceremony.x+8)/7); }", shader)
        root = ROOT / "Client/Encounters/FirstSeverance"
        for file, descriptor in {
            "FirstSeverancePursuitBeamVisuals.cs": "endAge:end-start",
            "FirstSeveranceEmissionVisuals.cs": "endAge:endTick-start",
            "FirstSeveranceStageVisuals.cs": "endAge:grid.CoreEndTick-grid.StartTick",
            "FirstSeveranceImpalingSwordVisuals.cs": "endAge:sword.Retract",
            "FirstSeveranceScoreVisuals.cs": "endAge:FirstSeveranceChoreography.BladeEnd",
        }.items():
            self.assertIn(descriptor, (root / file).read_text())

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


    def test_player_markers_keep_one_exact_world_boundary(self):
        root = ROOT / "Client/Encounters/FirstSeverance"
        renderer = (root / "FirstSeveranceRaidVfx.cs").read_text()
        marker = (root / "FirstSeveranceAttackAccents.cs").read_text().split("internal void Marker(")[1].split("internal void ChargeFracture(")[0]
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/MechanicRing.fx").read_text()
        self.assertIn("MechanicRing(batch, center, radius", marker)
        self.assertNotIn("if (!stack) return", marker)
        self.assertIn("stack ? 18 : -18", marker)
        self.assertNotIn("Arc(", marker)
        self.assertIn("radius / extent", renderer)
        self.assertIn('if (c.Pass == "MechanicRingPass") shader=marker', renderer)
        self.assertIn("(length(p)-signal.w)*shape.y", shader)
        self.assertIn("pass MechanicRingPass", shader)
        self.assertIn("signal.x-.006", shader)
        self.assertIn(".12+.80*signal.x", shader)
        self.assertIn("frac(cycle*24)", shader)


if __name__ == "__main__":
    unittest.main()
