"""Last Witness v2 contracts: clocks shared by the generator and WitnessRules, cue pairs and call-site levels, the
loops' revolutions, the material's passes, the item switch and the presentation's purity. Not a listening or
in-game test. The render checks need numpy, scipy, soundfile and the local recording store and are skipped without them.
"""
from pathlib import Path
import importlib.util
import math
import re
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "tools"
SOUNDS = ROOT / "Assets/Sounds/Weapons/DollWeapons"
GENERATOR = TOOLS / "generate_doll_weapon_sfx.py"
RULES = ROOT / "Content/Encounters/FirstSeverance/Rewards/WitnessRules.cs"
PROJECTILES = ROOT / "Content/Encounters/FirstSeverance/Rewards/WitnessProjectiles.cs"
ITEMS = ROOT / "Content/Encounters/FirstSeverance/Rewards/RitualArmaments.cs"
VISUALS = ROOT / "Client/Encounters/FirstSeverance/Weapons/WitnessVisuals.cs"
PRESENTATION = ROOT / "Client/Encounters/FirstSeverance/Weapons/WitnessPresentation.cs"
BLADE_ART = ROOT / "Client/Encounters/FirstSeverance/Weapons/WitnessBladeArt.cs"
WEAPONS = ROOT / "docs/encounters/first-severance/WEAPONS.md"
LEGACY_VISUALS = ROOT / "Client/Encounters/FirstSeverance/NullRefrainVisuals.cs"
SHADER = ROOT / "Assets/AutoloadedEffects/Shaders/DollWitnessEnergy.fx"


def code(path):
    return re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))


def constant(source, name):
    match = re.search(rf"\b{name}\s*=\s*(-?[\d.]+)f?\b", source)
    if not match:
        raise AssertionError(f"constant {name} not found")
    return float(match.group(1))


def generator_constant(name):
    match = re.search(rf"^{name}\s*=\s*(.+)$", GENERATOR.read_text(encoding="utf-8"), re.MULTILINE)
    if not match:
        raise AssertionError(f"generator constant {name} not found")
    return match.group(1)


def registry():
    rows = re.findall(r'@cue\("(\w+)",\s*(-?[\d.]+),\s*"([^"]*)",\s*([\d.]+),\s*([\d.]+),', GENERATOR.read_text(encoding="utf-8"))
    return {name: (float(target), group, float(limit), float(volume)) for name, target, group, limit, volume in rows}


def witness_cues():
    return {name: row for name, row in registry().items() if row[1].startswith("Last Witness")}


class WitnessClocks(unittest.TestCase):
    def test_generator_ticks_equal_the_rules(self):
        rules = RULES.read_text(encoding="utf-8")
        first, spacing = constant(rules, "FirstTestimony"), constant(rules, "TestimonySpacing")
        testimonies = generator_constant("W_TESTIMONIES")
        self.assertIn(f"tuple({int(first)} + {int(spacing)} * i for i in range(6))", testimonies)
        lead, seal, warn, throw = (int(v) for v in generator_constant("W_TESTIMONY_LEAD, W_SEAL, W_THROW_WARN, W_THROW").split(", "))
        self.assertEqual(lead, constant(rules, "TestimonyLead"))
        self.assertIn("Seal = RitualGrandScore.WitnessMerge", rules)
        self.assertIn("Throw = RitualGrandScore.WitnessFire", rules)
        self.assertEqual((seal, throw), (174, 218), "RitualGrandScore milestones")
        self.assertEqual(warn, constant(rules, "ThrowWarn"))
        bites = generator_constant("W_BITES, W_RETURN")
        self.assertIn("(6, 12, 17, 21)", bites)
        self.assertTrue(bites.endswith(str(int(constant(rules, "ReturnTick")))))
        self.assertIn("6 => 0, 12 => 1, 17 => 2, 21 => 3", rules, "the C# bite windows")
        lock, execute_warn, execute, withdraw = (int(v) for v in generator_constant("W_LOCK, W_EXECUTE_WARN, W_EXECUTE, W_WITHDRAW").split(", "))
        self.assertEqual(execute_warn, constant(rules, "EdgeWriteStart"))
        self.assertEqual(withdraw, constant(rules, "WithdrawStart"))
        choreography = (ROOT / "Content/Encounters/FirstSeverance/Rewards/RitualArmamentChoreography.cs").read_text(encoding="utf-8")
        self.assertEqual(lock, constant(choreography, "VerdictLock"))
        self.assertEqual(execute, constant(choreography, "VerdictHit"))
        self.assertAlmostEqual(float(generator_constant("W_CRUISE_SPIN").split()[0]), constant(rules, "CruiseSpin"))

    def test_loops_hold_a_whole_number_of_blade_revolutions(self):
        cruise = constant(RULES.read_text(encoding="utf-8"), "CruiseSpin")
        peak = 2 * (4 * math.pi / 21) - cruise
        for revolutions_name, samples_name, spin in (("W_SPIN_REVOLUTIONS, W_SPIN_SAMPLES", None, cruise),
                                                     ("W_AXIOM_REVOLUTIONS, W_AXIOM_SAMPLES", None, peak)):
            revolutions, samples = (int(v) for v in generator_constant(revolutions_name).split("#")[0].split(", "))
            period = 2 * math.pi / spin / 60 * 44100
            self.assertAlmostEqual(samples / revolutions, period, delta=period * 1e-4, msg=revolutions_name)


class WitnessCues(unittest.TestCase):
    def test_every_release_pairs_a_warning_with_a_firing_and_the_set_pieces_can_miss(self):
        cues = witness_cues()
        self.assertTrue(cues)
        releases = {re.sub(r"(Warn|Fire)\d*$", "", name) for name in cues if re.search(r"(Warn|Fire)\d*$", name)}
        for release in releases:
            self.assertTrue(any(re.fullmatch(rf"{release}Warn\d*", n) for n in cues), f"{release} has a Warn")
            self.assertTrue(any(re.fullmatch(rf"{release}Fire\d*", n) for n in cues), f"{release} has a Fire")
        self.assertLessEqual({"WitnessTestimony", "WitnessThrow", "WitnessAxiom", "WitnessReturn", "VerdictStake", "VerdictExecute"}, releases)
        self.assertIn("WitnessAxiomMiss", cues)
        self.assertIn("VerdictExecuteMiss", cues)
        for name in cues:
            self.assertRegex(name, r"^(Witness|Verdict)[A-Z]\w*$")

    def test_call_sites_play_registered_cues_at_their_audition_volume(self):
        cues = witness_cues()
        source = code(VISUALS)
        played = re.findall(r'DollWeaponAudio\.Play\("(\w+)",[^;]*?,\s*([\d.]+)f\)', source)
        self.assertTrue(played)
        for name, volume in played:
            self.assertIn(name, cues)
            self.assertAlmostEqual(cues[name][3], float(volume), msg=f"{name}: the audition plays the call-site volume")
        for name, volume in re.findall(r'DollWeaponAudio\.Sustain\(ref \w+, "(\w+)",[^;]*?,\s*\.(\d+)f \* \w+\)', source):
            self.assertAlmostEqual(cues[name][3], float("." + volume), msg=name)
        warns = re.search(r"testimonyWarn =\s*\{([^}]*)\}", source).group(1)
        names = re.findall(r'"(\w+)"', warns)
        self.assertEqual([f"WitnessTestimonyWarn{i}" for i in range(1, 7)], names)
        self.assertIn("DollWeaponAudio.Play(testimonyWarn[birth], center, .75f)", source)
        for name in names:
            self.assertAlmostEqual(cues[name][3], .75)
        bites = re.search(r'bite switch \{([^}]*)\}', source).group(1)
        self.assertEqual([f"WitnessAxiomFire{i}" for i in range(1, 5)], re.findall(r'"(\w+)"', bites))
        self.assertIn("anchored ? .8f : .45f", source)
        exported = {p.stem for p in SOUNDS.glob("*.ogg")} | {p.stem for p in SOUNDS.glob("*.wav")}
        self.assertLessEqual(set(cues), exported)

    def test_the_finishing_execution_is_restrained(self):
        # Owner preference for big closing sounds (2026-10-03): a quiet organ under a soft gong-like strike with a long
        # tail, at T3 or below, rather than a loud organ stab.
        target, _, limit, _ = witness_cues()["VerdictExecuteFire"]
        self.assertLessEqual(target, -11.5, "T3 or quieter")
        self.assertGreaterEqual(limit, 2.0, "a long decaying tail")
        source = GENERATOR.read_text(encoding="utf-8")
        body = source[source.index("def verdict_execute_fire("):]
        body = body[:body.index("\n\n\n")]
        self.assertIn("brass_gong(", body)
        organ = re.search(r"place\(mix, dsp\.organ_pad\([^\n]*\), [\d.]+, (-?[\d.]+)\)", body)
        self.assertIsNotNone(organ)
        self.assertLessEqual(float(organ.group(1)), -12, "the organ stays quiet under the strike")

    def test_no_runtime_transposition_and_no_reduced_effects_on_sound(self):
        source = code(VISUALS)
        self.assertNotIn("DollWeaponAudio.Note(", source, "composite cues and loops are never transposed")
        self.assertIsNone(re.search(r"DollWeaponAudio\.Play\([^;]*,\s*[\d.]+f,\s*[-\d.]", source), "no pitch argument")
        self.assertNotIn("ActiveSound", source, "no live pitch bend")
        self.assertNotIn("Reduced", re.sub(r"canvas\.Reduced", "", source))


class WitnessMaterial(unittest.TestCase):
    def test_passes_and_branchless_uniforms(self):
        shader = SHADER.read_text(encoding="utf-8")
        body = re.sub(r"//[^\n]*", "", shader)
        passes = re.findall(r"pass (\w+) \{", body)
        self.assertEqual(["RibbonPass", "FillPass"], passes)
        presentation = PRESENTATION.read_text(encoding="utf-8")
        self.assertIn("internal const int RibbonPass = 0, FillPass = 1;", presentation)
        self.assertIsNone(re.search(r"\bif\s*\(", body), "no if statement (FNA/MojoShader mis-translates uniform-only branches)")
        # Per-frame values (the fill's ground alpha, eye radius and void alpha) come per vertex from the CPU: the only
        # uniforms are the shared four.
        uniforms = re.findall(r"^(?:float\d?|matrix)\s+(\w+);", body, re.M)
        self.assertEqual(["uWorldViewProjection", "dotOrigin", "clock", "reduced"], uniforms)
        self.assertIn("triangle[i].Local = new Vector4(p.X - middle.X, p.Y - middle.Y, dots, FillGroundAlpha);", presentation)
        self.assertIn("triangle[i].Shape = new Vector4(p.X, p.Y, eye, voidAlpha);", presentation)
        self.assertIn('ShaderName = "Convergence.DollWitnessEnergy"', presentation)


class WitnessOwnership(unittest.TestCase):
    def test_item_points_at_the_new_controller_and_icon(self):
        items = code(ITEMS)
        witness = items[items.index("public sealed class LastWitness"):]
        witness = witness[:witness.index("\npublic sealed class", 1) if "\npublic sealed class" in witness[1:] else len(witness)]
        self.assertIn('WitnessArt.Root + "LastWitnessIcon"', witness)
        self.assertIn("ModContent.ProjectileType<WitnessHang>()", witness)
        self.assertNotIn("WitnessLitany", witness)
        legacy = code(LEGACY_VISUALS)
        excluded = re.search(r"entity\.ModItem is not \(([^)]*)\)", legacy).group(1).split(" or ")
        self.assertIn("NullRefrain", excluded)
        self.assertIn("LastWitness", excluded, "no V3 art for the refreshed icon")
        for name in ("WitnessLitany.cs", "WitnessVerdict.cs", "RitualBolts.cs"):
            self.assertTrue((ROOT / "Content/Encounters/FirstSeverance/Rewards" / name).is_file(), f"legacy {name} kept for the cleanup PR")

    def test_projectiles_are_native_and_draw_only_through_the_layer(self):
        source = code(PROJECTILES)
        for name in ("WitnessHang", "WitnessThrownBlade", "WitnessShard", "WitnessJudgement"):
            body = source[source.index(f"public sealed class {name} : ModProjectile"):]
            body = body[:body.index("\npublic sealed class", 1) if "\npublic sealed class" in body[1:] else len(body)]
            self.assertIn("public override bool PreDraw(ref Color lightColor) => false;", body, name)
        for forbidden in ("ModPacket", "NetMessage.SendData", "GetPacket"):
            self.assertNotIn(forbidden, source)
        blade = source[source.index("public sealed class WitnessThrownBlade"):source.index("public sealed class WitnessShard")]
        send = blade[blade.index("public override void SendExtraAI"):blade.index("public override void ReceiveExtraAI")]
        sizes = {"(byte)": 1, "(ushort)": 2, "anchor.X": 4, "anchor.Y": 4, "Projectile.rotation": 4}
        total = sum(size for token, size in sizes.items() if token in send)
        self.assertLessEqual(total, 16, "ExtraAI within 16 bytes")
        self.assertIn(f"// {total} bytes", PROJECTILES.read_text(encoding="utf-8"), "the SendExtraAI comment states the size")
        self.assertIn(f"{total} bytes of `ExtraAI`", WEAPONS.read_text(encoding="utf-8"), "the spec states the size")

    def test_the_thrown_blade_keeps_the_hanging_rung(self):
        # The shared rules fit the art to the 56 px disc: one k = 2 rung, hanging and thrown; the k = 1 rung is not drawn.
        art = code(BLADE_ART)
        self.assertIn('internal const string Name = "WitnessBlade";', art)
        self.assertIn("internal const float ArcRadius = WitnessRules.SpinRadius;", art)
        for path in (PRESENTATION, VISUALS, PROJECTILES):
            self.assertNotIn("WitnessBlade_L", code(path), path.name)
        self.assertIn('Texture => WitnessArt.Root + "WitnessBlade";', code(PROJECTILES))
        self.assertIn("DollWeaponTextures.Get(WitnessBladeArt.Name)", code(VISUALS))
        self.assertIn("float radius = WitnessBladeArt.ArcRadius;", code(PRESENTATION))
        spec = WEAPONS.read_text(encoding="utf-8")
        rogue = spec[spec.index("## Rogue — Last Witness"):spec.index("## Claw swipe cleanup")]
        self.assertNotIn("260", rogue)
        self.assertNotIn("150 px from its owner", rogue)
        projectiles = code(PROJECTILES)
        shard = projectiles[projectiles.index("public sealed class WitnessShard"):]
        self.assertIn("Projectile.extraUpdates = WitnessRules.ShardUpdates - 1;", shard)
        self.assertIn("at 44 px/tick", rogue, "ShardLaunch per update times two updates a tick")

    def test_presentation_is_terraria_free_and_previewed(self):
        presentation = PRESENTATION.read_text(encoding="utf-8")
        self.assertIsNone(re.search(r"^using\s+(Terraria|ReLogic|Luminance)", presentation, re.M))
        for pure in (RULES, BLADE_ART):
            self.assertIsNone(re.search(r"^using\s+(Terraria|Microsoft\.Xna|ReLogic|Luminance)", pure.read_text(encoding="utf-8"), re.M), pure.name)
        script = (TOOLS / "preview-doll-witness.ps1").read_text(encoding="utf-8")
        for linked in ("WitnessPresentation.cs", "WitnessBladeArt.cs", "WitnessRules.cs", "DollArtAnchors.g.cs", "DollWeaponCanvas.cs"):
            self.assertIn(linked, script)
        project = (ROOT / "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj").read_text(encoding="utf-8")
        self.assertIn("Content/Encounters/FirstSeverance/Rewards/WitnessRules.cs", project)
        self.assertIn("Client/Encounters/FirstSeverance/Weapons/WitnessBladeArt.cs", project)
        # The thrown blade's damage bookkeeping is the pure ledger the domain tests run.
        blade = code(PROJECTILES)
        self.assertIn("private readonly WitnessBladeLedger ledger = new();", blade)
        self.assertIn("ledger.Live((int)Age) ? null : false", blade)
        self.assertIn("if (ledger.Settle(age)) Array.Clear(Projectile.localNPCImmunity);", blade)


def numeric_stack():
    return all(importlib.util.find_spec(name) for name in ("numpy", "scipy", "soundfile"))


@unittest.skipUnless(numeric_stack(), "numpy, scipy and soundfile are local audio tools, not CI")
class WitnessRender(unittest.TestCase):
    def test_witness_cues_are_reproducible(self):
        sys.path.insert(0, str(TOOLS))
        import generate_doll_weapon_sfx as gen
        store_root = gen.find_store()
        if store_root is None:
            self.skipTest("local recording store not found")
        store = gen.Store(store_root, (ROOT / "Assets/ATTRIBUTION.md").read_text(encoding="utf-8"))
        for name in ("WitnessTestimonyWarn1", "WitnessThrowFire", "WitnessSpinLoop", "VerdictExecuteFire"):
            with self.subTest(cue=name), tempfile.TemporaryDirectory() as folder:
                gen.render_cue(name, store, Path(folder))
                path = gen.export_path(name, Path(folder))
                self.assertEqual(path.read_bytes(), (SOUNDS / path.name).read_bytes(), "the committed cue is reproducible")


if __name__ == "__main__":
    unittest.main()
