"""Small integration guards for the formerly double-scaled cinematic callbacks."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / "Client/Encounters/FirstSeverance"


def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if not depth:
            return source[start:end + 1]
    raise AssertionError("Unclosed callback")


class CinematicCoordinates(unittest.TestCase):
    def test_critical_audio_is_not_owned_by_short_live_ray_windows(self):
        source = (CLIENT / "FirstSeveranceFeedback.cs").read_text(encoding="utf-8")
        cues = body(source, "private void PlayCriticalAction")
        self.assertIn("criticalClock.Take", cues)
        self.assertIn('"CrushCataclysm"', cues)
        self.assertIn('"IronDescent"', cues)
        self.assertIn('"BladeOrbitSecond"', cues)
        self.assertNotIn("ray.Live", cues)
        self.assertIn("StopVoices(preserveImpacts: true)", source)
        self.assertIn("state.TerminalCombat", source)
        self.assertIn("pendingResult = combat", source)
        self.assertIn("tick >= result.MechanicTick", source)
        self.assertIn("orbit.Stop()", source)

    def test_claw_swipe_keeps_unsheathe_without_long_sweep_layer(self):
        source = (CLIENT / "NullCantorClawPresentation.cs").read_text(encoding="utf-8")
        self.assertIn('system.Play("BladeUnsheathe"', source)
        self.assertNotIn('system.Play("BladeSweep"', source)

    def test_weapon_audio_is_not_muted_by_reduced_visual_effects(self):
        source = (CLIENT / "NullRefrainVisuals.cs").read_text(encoding="utf-8")
        self.assertNotIn("RitualArmamentArt.Reduced", body(source, "private void UpdateSustain"))
        self.assertNotIn("RitualArmamentArt.Reduced", body(source, "internal static void Sound"))

    def test_boss_registers_a_compact_existing_head_for_vanilla_bar(self):
        source = (ROOT / "Content/Encounters/FirstSeverance/Actors/FirstSeverancePrototypeBoss.cs").read_text(encoding="utf-8")
        self.assertIn("[AutoloadBossHead]", source)
        self.assertIn("public override string BossHeadTexture => Texture;", source)
        self.assertIn("NPC.boss = true;", source)
        self.assertTrue((ROOT / "Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem.png").is_file())

    def test_claw_keeps_light_without_dark_trail_or_swipe_debris(self):
        source = (CLIENT / "NullCantorClawArt.cs").read_text(encoding="utf-8")
        self.assertIn("darkUnderlay: false", body(source, "internal static void QueueSwipeTrail"))
        swipe = body(source, "internal static void DrawSwipe")
        self.assertIn("Hand(b,", swipe)
        self.assertIn("Impact(b,", swipe)
        self.assertNotIn("Shards(b,", swipe)
        self.assertIn("if (crush) Shards", body(source, "internal static void Impact"))
        surface = (CLIENT / "RitualSurfacePass.cs").read_text(encoding="utf-8")
        self.assertIn("bool darkUnderlay = true", surface)  # Other weapons unchanged.
        self.assertIn("if (darkUnderlay) Ribbon", body(surface, "internal static void Flame"))

    def test_shared_pylon_renderer_has_no_instance_caches(self):
        source = (CLIENT / "FirstSeverancePylonVisuals.cs").read_text(encoding="utf-8")
        self.assertIn("private static Asset<Texture2D> cage;", source)
        self.assertIn("private static readonly FirstSeveranceAttackAccents accents", source)
        self.assertNotIn("InstancePerEntity => true", source)
        unload = body(source, "public override void Unload")
        self.assertIn("cage = null", unload)
        self.assertIn("accents.Unload()", unload)

    def test_all_cinematic_layers_are_unscaled(self):
        source = (CLIENT / "FirstSeverancePrototypePresentation.cs").read_text(encoding="utf-8")
        layers = body(source, "public override void ModifyInterfaceLayers")
        self.assertEqual(3, layers.count("InterfaceScaleType.None"))
        self.assertNotIn("InterfaceScaleType.UI", layers)
        self.assertNotIn("InterfaceScaleType.Game", layers)

    def test_start_phase_and_result_use_physical_viewport(self):
        presentation = (CLIENT / "FirstSeverancePrototypePresentation.cs").read_text(encoding="utf-8")
        result = (CLIENT / "FirstSeveranceResultVisuals.cs").read_text(encoding="utf-8")
        for source, name in ((presentation, "private static void DrawIntro"),
                             (presentation, "private static void DrawRupture"),
                             (result, "internal static void Draw")):
            with self.subTest(callback=name):
                callback = body(source, name)
                # Strip comments so explanations of the original defect are allowed.
                code = "\n".join(line.split("//")[0] for line in callback.splitlines())
                self.assertIn("GraphicsDevice.Viewport", code)
                self.assertNotIn("Main.UIScale", code)
                self.assertNotIn("Main.screenWidth", code)
                self.assertNotIn("Main.screenHeight", code)


if __name__ == "__main__":
    unittest.main()
