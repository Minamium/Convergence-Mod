"""Small integration guards for the formerly double-scaled cinematic callbacks."""
from pathlib import Path
import re
import struct
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
    def test_preparation_silence_does_not_change_volume_or_combat_music(self):
        source = (CLIENT / "FirstSeverancePreparationSilence.cs").read_text(encoding="utf-8")
        self.assertIn("public override int Music => 0", source)
        self.assertIn("state.Combat is null", source)
        self.assertIn("state.Preparation is { } preparation", source)
        self.assertIn("TryGetMemberByServerSlot", source)
        self.assertIn("Main.dedServ || Main.gameMenu", source)
        self.assertNotIn("musicVolume", source)
        self.assertNotIn("SpecialVisuals", source)

    def test_audio_windows_end_and_do_not_own_verdict_tails(self):
        source = (CLIENT / "FirstSeveranceFeedback.cs").read_text(encoding="utf-8")
        update = body(source, "internal void Update(")
        self.assertIn("UpdateTimedVoices(state.EstimatedAuthorityTick)", update)
        self.assertIn("grid.EndTick + 6", update)
        self.assertIn("volley.EndTick + 6", update)
        self.assertIn("cast.EndTick + 6", update)
        self.assertIn("SlicerEnd(combat.ActionIndex)", update)
        timed = body(source, "private void UpdateTimedVoices")
        self.assertIn("tick >= voice.End", timed)
        self.assertIn("sound.Stop()", timed)
        self.assertIn("sound.Volume = Math.Min(sound.Volume", timed)
        critical = body(source, "private ReLogic.Utilities.SlotId PlayCritical")
        self.assertNotIn("timedVoices", critical)
        self.assertIn("impactTails.Add(id)", critical)

    def test_plinth_and_four_posts_do_not_stack_transparent_caps(self):
        source = (CLIENT / "FoundationCoreVisuals.cs").read_text(encoding="utf-8")
        plinth = body(source, "internal void DrawPlinth")
        self.assertIn("new(0, 230, 850, 212)", plinth)
        self.assertIn("foot + Vector2.UnitY", plinth)
        field = body(source, "private void DrawField")
        self.assertIn("tier < 2", field)
        self.assertIn("outer ? 128 : 84", field)
        self.assertIn("outer ? 320 : 190", field)
        self.assertNotIn("h += 256", field)

    def test_preparation_is_compact_and_suspension_has_attached_endpoints(self):
        source = (CLIENT / "FirstSeverancePreparationVisuals.cs").read_text(encoding="utf-8")
        overlay = body(source, "internal bool DrawOverlay")
        self.assertNotIn("CONTAINMENT // ESTABLISHING", overlay)
        self.assertNotIn('Text("Deploying")', overlay)
        self.assertIn("button.Contains(Main.mouseX, Main.mouseY)", overlay)
        self.assertIn("FirstSeveranceClientActions.InteractWithCore", overlay)
        field = (CLIENT / "FoundationCoreVisuals.cs").read_text(encoding="utf-8")
        self.assertNotIn("Line(batch, top, latch", field)
        # The Doll rig now owns both the sealed shell and exposed-body cords.
        doll = (CLIENT / "FirstSeveranceDollVisuals.cs").read_text(encoding="utf-8")
        shell = body(doll, "internal static void DrawShellCords")
        self.assertIn("FoundationCoreVisuals.HoistAnchor(", shell)
        self.assertIn("FirstSeveranceShellSurface.Attachment(", shell)
        self.assertIn("FirstSeveranceShellSurface.Suspension(", shell)
        exposed = body(doll, "private void DrawCords")
        self.assertIn("pose.Cords", exposed)
        self.assertIn("cord.Attachment", exposed)
        cable = body(doll, "internal static void Cord(")
        self.assertIn("i<=count", cable)
        self.assertIn("Vector2.Lerp(from,to,t)", cable)
        self.assertIn("MathF.Sin(t*MathF.PI)", cable)

    def test_preparation_field_ready_and_cinematic_share_unscaled_geometry(self):
        source = (CLIENT / "FirstSeverancePreparationVisuals.cs").read_text(encoding="utf-8")
        overlay = body(source, "internal bool DrawOverlay")
        self.assertIn("GraphicsDevice.Viewport", overlay)
        self.assertNotIn("Main.UIScale", overlay)
        self.assertIn("prep.ReadyOpensTick", overlay)
        self.assertIn('"Ready!"', source)
        presentation = (CLIENT / "FirstSeverancePrototypePresentation.cs").read_text(encoding="utf-8")
        capture = body(presentation, "public override void PostDrawTiles")
        self.assertIn("preparing.GroundX", capture)
        self.assertIn("FirstSeveranceFieldMaskLayout.Capture", capture)
        runtime = (ROOT / "Content/Encounters/FirstSeverance/FirstSeverancePreparationRuntime.cs").read_text(encoding="utf-8")
        self.assertLess(runtime.index("if (!ServerRosterMatches())"), runtime.index("pendingIntents.Sort"))
        self.assertIn("ReadyHoldTicks", runtime)
        self.assertIn("RemoteClient.CheckSection", runtime)
        self.assertIn("Clear(fightId)", runtime)
        resolver = (ROOT / "Content/Encounters/FirstSeverance/FirstSeveranceCoreResolver.cs").read_text(encoding="utf-8")
        self.assertIn("requireAllConnected: true", resolver)
        self.assertNotIn("ParticipationRadiusInTiles", resolver)

    def test_critical_audio_is_not_owned_by_short_live_ray_windows(self):
        source = (CLIENT / "FirstSeveranceFeedback.cs").read_text(encoding="utf-8")
        cues = body(source, "private void PlayCriticalAction")
        self.assertIn("criticalClock.Take", cues)
        self.assertIn('"CrushCataclysm"', cues)
        self.assertIn('"PrismBeamFire"', cues)
        self.assertIn('"PrismBeamSustain"', cues)
        self.assertIn("SecondTurnTick", cues)
        self.assertNotIn('"IronDescent"', source)
        self.assertNotIn('"BladeOrbitSecond"', source)
        for clip in ("MagicCharge", "MagicFire", "LacunaSustain"):
            self.assertTrue((ROOT / f"Assets/Sounds/Weapons/DollTheater/{clip}.wav").is_file())
        self.assertNotIn("ray.Live", cues)
        self.assertIn("StopVoices(preserveImpacts: true)", source)
        self.assertIn("state.TerminalCombat", source)
        self.assertIn("pendingResult = combat", source)
        self.assertIn("tick >= result.MechanicTick", source)
        self.assertIn("orbit.Stop()", source)

    def test_claw_swipe_uses_weapon_foley_without_long_sweep_layer(self):
        source = (CLIENT / "NullCantorClawPresentation.cs").read_text(encoding="utf-8")
        self.assertIn('system.Play("ClawSwipe"', source)
        self.assertNotIn('system.Play("BladeSweep"', source)
        self.assertIn("RitualWeaponFeedback.SoundRoot + name", body(source, "internal void Play("))
        self.assertTrue((ROOT / "Assets/Sounds/Weapons/DollTheater/ClawSwipe.wav").is_file())

    def test_weapon_audio_is_not_muted_by_reduced_visual_effects(self):
        source = (CLIENT / "NullRefrainVisuals.cs").read_text(encoding="utf-8")
        self.assertNotIn("RitualArmamentArt.Reduced", body(source, "private void UpdateSustain"))
        # Sound returns a voice handle; its return type does not govern audibility.
        sound = body(source, " Sound(")
        self.assertIn("SoundEngine.PlaySound", sound)
        for callback in (sound, body(source, "private void UpdateSustain")):
            self.assertNotIn("RitualArmamentArt.Reduced", callback)
            self.assertNotIn(".ReducedEffects", callback)

    def test_boss_registers_a_compact_existing_head_for_vanilla_bar(self):
        source = (ROOT / "Content/Encounters/FirstSeverance/Actors/FirstSeverancePrototypeBoss.cs").read_text(encoding="utf-8")
        self.assertIn("[AutoloadBossHead]", source)
        self.assertIn("NPC.boss = true;", source)
        portrait = re.search(r'BossHeadTexture\s*=>\s*"Convergence/([^"]+)"', source)
        self.assertIsNotNone(portrait, "The native Boss bar must resolve a project portrait")
        png = (ROOT / (portrait.group(1) + ".png")).read_bytes()
        self.assertEqual(b"\x89PNG\r\n\x1a\n", png[:8])
        self.assertEqual((34, 34), struct.unpack(">II", png[16:24]), "Doll portrait dimensions from the visual spec")

    def test_claw_keeps_light_without_dark_trail_or_swipe_debris(self):
        source = (CLIENT / "NullCantorClawArt.cs").read_text(encoding="utf-8")
        self.assertIn("darkUnderlay: false", body(source, "internal static void QueueSwipeTrail"))
        swipe = body(source, "internal static void DrawSwipe")
        self.assertIn("Hand(b,", swipe)
        self.assertIn("Impact(b,", swipe)
        self.assertNotIn("Shards(b,", swipe)
        self.assertIn("if (crush) Shards", body(source, "internal static void Impact"))
        surface = (CLIENT / "RitualSurfacePass.cs").read_text(encoding="utf-8")
        self.assertIn("bool darkUnderlay = true", surface)  # Existing call sites remain valid.
        self.assertNotIn("if (darkUnderlay) Ribbon", body(surface, "internal static void Flame"))
        self.assertIn('ShaderManager.GetShader("Convergence.ArmamentEnergy")', surface)
        for slot in (1, 2):
            self.assertIn(f"device.Textures[{slot}] = t{slot}", surface)
            self.assertIn(f"device.SamplerStates[{slot}] = s{slot}", surface)

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
        self.assertEqual(5, layers.count("InterfaceScaleType.None"))
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
