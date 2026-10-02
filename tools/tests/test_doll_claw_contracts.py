"""Lacrimosa's Claws (2026-10 refresh): cue pairing and loudness, routing and the presentation contract.

Source and export checks are pure Python (CI); the loudness check needs numpy, scipy and soundfile and is skipped
without them. Not a listening test: the owner audition and the in-game mix stay user-owned.
"""
from pathlib import Path
import importlib.util
import re
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "tools"
SOUNDS = ROOT / "Assets/Sounds/Weapons/DollWeapons"
GENERATOR = TOOLS / "generate_doll_weapon_sfx.py"
WEAPONS = ROOT / "Client/Encounters/FirstSeverance/Weapons"
REWARDS = ROOT / "Content/Encounters/FirstSeverance/Rewards"


def claw_cues():
    rows = re.findall(r'@cue\("(Claw\w+)",\s*(-?[\d.]+),\s*"([^"]*)",\s*([\d.]+),\s*([\d.]+),', GENERATOR.read_text(encoding="utf-8"))
    return {name: (float(target), group, float(limit), float(volume)) for name, target, group, limit, volume in rows}


def code(path):
    return re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))


class ClawCues(unittest.TestCase):
    def test_every_release_pairs_a_warning_with_a_firing_and_the_grasp_has_its_own_miss(self):
        cues = claw_cues()
        self.assertTrue(cues)
        self.assertTrue(all(group == "Claws" for _, group, _, _ in cues.values()))
        fires = {name[:-4] for name in cues if name.endswith("Fire")}
        warns = {name[:-4] for name in cues if name.endswith("Warn")}
        self.assertEqual({"ClawRakeDown", "ClawRakeUp", "ClawClap", "ClawGrasp", "ClawCrush"}, fires)
        self.assertEqual(fires, warns, "every release = <Release>Warn + <Release>Fire")
        self.assertIn("ClawGraspMiss", cues, "the grasp can close on air: its own failure cue")
        tiers = {-20, -17, -13, -11.5, -10}
        for name, (target, _, _, _) in cues.items():
            self.assertIn(target, tiers, f"{name} takes a Doll weapon loudness tier")
        self.assertEqual(-10, cues["ClawCrushFire"][0], "the crush is the finale tier")
        for name in cues:
            self.assertTrue((SOUNDS / f"{name}.ogg").is_file(), f"{name} is exported")

    def test_the_client_routes_every_claw_cue_through_the_doll_player(self):
        visuals = code(WEAPONS / "LacrimosaClawVisuals.cs")
        routed = set(re.findall(r'DollWeaponAudio\.(?:Play|Note)\("(Claw\w+)"', visuals))
        self.assertEqual(set(claw_cues()), routed)
        self.assertIn('DollWeaponAudio.Note("ClawBead", 0, beads - 1', visuals, "only the single-note bead moves up the ladder")
        self.assertEqual(1, visuals.count("DollWeaponAudio.Note("), "composite cues are never transposed")
        self.assertIn("DollCueClock.Take(", visuals)
        self.assertNotIn("SoundEngine", visuals)
        self.assertNotIn("ReplaceOldest", visuals)
        self.assertNotIn("ReducedEffects", visuals, "Reduced Effects never changes what is heard")
        self.assertNotIn("ScreenShakeSystem", visuals, "shake only through RitualWeaponFeedback.Kick")
        self.assertIn("RitualWeaponFeedback>().Kick(", visuals)


class ClawSources(unittest.TestCase):
    def test_new_types_and_the_item_points_at_them(self):
        item = code(REWARDS / "NullRefrain.cs")
        self.assertIn("class NullRefrain : RitualArmament", item)
        self.assertIn("ModContent.ProjectileType<LacrimosaClawKata>()", item)
        self.assertNotIn("NullCantorClawSwipe", item)
        self.assertNotIn("NullCantorClawCrush", item)
        self.assertNotIn("PreDrawInInventory", item, "the new icon draws natively")
        self.assertIn("NullRefrainIcon", code(REWARDS / "LacrimosaClawProjectiles.cs"))
        # Legacy pure files, projectile types and tests stay until the cleanup PR.
        for legacy in ("NullCantorClawProjectiles.cs", "NullCantorClawMotion.cs", "NullRefrainSlash.cs", "NullRefrainMotion.cs"):
            self.assertTrue((REWARDS / legacy).is_file(), legacy)
        self.assertTrue((ROOT / "Tests/Convergence.DomainTests/NullCantorClawTests.cs").is_file())

    def test_the_legacy_presentation_no_longer_keys_on_the_held_claws(self):
        legacy = code(ROOT / "Client/Encounters/FirstSeverance/NullCantorClawPresentation.cs")
        self.assertNotIn("NullCantorClawItemVisuals", legacy)
        self.assertNotIn("ParkedHand", legacy)
        self.assertNotIn("HeldItem", legacy)

    def test_the_controller_carries_what_peers_draw_and_ends_with_its_owner(self):
        source = code(REWARDS / "LacrimosaClawProjectiles.cs")
        kata = source[source.index("class LacrimosaClawKata"):source.index("class LacrimosaClawGrasp")]
        self.assertIn("Projectile.netImportant = true", kata)
        for written in ("writer.Write((byte)duration)", "writer.Write((byte)Beads)", "writer.Write(Serial)"):
            self.assertIn(written, kata)
        self.assertIn("RitualArmamentItems.Usable(owner) && !owner.CCed && !owner.noItems", source)
        self.assertIn("++Projectile.localAI[1] > 6", kata, "peers end it after the held-item sync")
        self.assertNotIn("ModPacket", source)
        self.assertNotIn("SendData", source)

    def test_presentation_is_terraria_free_and_material_branchless(self):
        for name in ("LacrimosaClawPresentation.cs", "LacrimosaClawArt.cs"):
            text = (WEAPONS / name).read_text(encoding="utf-8")
            self.assertIsNone(re.search(r"^using\s+(Terraria|ReLogic|Luminance)", text, re.M), f"{name} links into the offline preview")
        shader = (ROOT / "Assets/AutoloadedEffects/Shaders/DollClawEnergy.fx").read_text(encoding="utf-8")
        body = re.sub(r"//[^\n]*", "", shader)
        self.assertIsNone(re.search(r"\bif\s*\(", body), "no 'if' in the claw material (MojoShader mis-translates uniform branches)")
        for name in ("Talon", "Flare", "Ring", "Void"):
            self.assertIn(f"pass {name} {{", shader)


def numeric_stack():
    return all(importlib.util.find_spec(name) for name in ("numpy", "scipy", "soundfile"))


@unittest.skipUnless(numeric_stack(), "numpy, scipy and soundfile are local audio tools, not CI")
class ClawLoudness(unittest.TestCase):
    def test_exports_meet_their_tier_and_peak_ceiling(self):
        sys.path.insert(0, str(TOOLS))
        import soundfile as sf
        import generate_doll_weapon_sfx as gen
        for name, (target, _, limit, _) in claw_cues().items():
            with self.subTest(cue=name):
                x, rate = sf.read(str(SOUNDS / f"{name}.ogg"), always_2d=True, dtype="float64")
                self.assertEqual(44100, rate)
                self.assertLessEqual(len(x) / rate, limit)
                self.assertLessEqual(gen.true_peak_db(x), -1.0)
                self.assertAlmostEqual(target, gen.loudness(x), delta=0.5)


if __name__ == "__main__":
    unittest.main()
