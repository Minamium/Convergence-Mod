"""Choir of the Unmade (2026-10 refresh): source contracts between the pure concert rules, the client cue routing,
the generator's mirror of the score, the material and the item wiring. Not a listening or play test.

The render check needs numpy, scipy and soundfile (local audio tools) and is skipped without them.
"""
from pathlib import Path
import hashlib
import importlib.util
import re
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "tools"
RULES = ROOT / "Content/Encounters/FirstSeverance/Rewards/ChoirConcertRules.cs"
CONCERT = ROOT / "Content/Encounters/FirstSeverance/Rewards/ChoirConcert.cs"
ITEMS = ROOT / "Content/Encounters/FirstSeverance/Rewards/RitualArmaments.cs"
BUFF = ROOT / "Content/Encounters/FirstSeverance/Rewards/RitualChoir.cs"
LEGACY_VISUALS = ROOT / "Client/Encounters/FirstSeverance/NullRefrainVisuals.cs"
WEAPONS = ROOT / "Client/Encounters/FirstSeverance/Weapons"
VISUALS = WEAPONS / "ChoirVisuals.cs"
PRESENTATION = WEAPONS / "ChoirPresentation.cs"
AUDIO = WEAPONS / "DollWeaponAudio.cs"
SHADER = ROOT / "Assets/AutoloadedEffects/Shaders/DollChoirEnergy.fx"
GENERATOR = TOOLS / "generate_doll_weapon_sfx.py"
SOUNDS = ROOT / "Assets/Sounds/Weapons/DollWeapons"


def code(path):
    """Source without // comments."""
    return re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))


def registry():
    rows = re.findall(r'@cue\("(\w+)",\s*(-?[\d.]+),\s*"([^"]*)",\s*([\d.]+),\s*([\d.]+),', GENERATOR.read_text(encoding="utf-8"))
    return {name: (float(target), group, float(limit), float(volume)) for name, target, group, limit, volume in rows}


def const(source, name):
    match = re.search(rf"\b{name}\s*=\s*([^,;]+)[,;]", source)
    assert match, name
    return match.group(1).strip()


def numeric_stack():
    return all(importlib.util.find_spec(name) for name in ("numpy", "scipy", "soundfile"))


class ChoirScoreMirror(unittest.TestCase):
    """The generator renders the same score the game schedules (a change on one side forces a regeneration)."""

    def test_generator_mirrors_the_concert_rules(self):
        rules = RULES.read_text(encoding="utf-8")
        gen = GENERATOR.read_text(encoding="utf-8")
        table = re.search(r"private static readonly int\[,\] lines =\s*\{(.*?)\};", rules, re.S).group(1)
        rows = [tuple(int(v) for v in re.findall(r"\d+", row)) for row in re.findall(r"\{([^{}]*)\}", table)]
        mirror = re.search(r"^CHOIR_LINES = \((.*)\)$", gen, re.M).group(1)
        self.assertEqual(rows, [tuple(int(v) for v in re.findall(r"\d+", row)) for row in re.findall(r"\(([^()]*)\)", mirror)])
        sung = re.findall(r'"(\w+)"', re.search(r"SungNames = \{([^}]*)\}", rules).group(1))
        self.assertEqual(sung, re.findall(r'"(\w+)"', re.search(r"^CHOIR_SUNG = \(([^)]*)\)", gen, re.M).group(1)))
        beat = int(const(rules, "Beat"))
        self.assertEqual(36, beat)
        ticks = {"Verse": 2, "Gather": 8, "Pipes": 9, "Inhale": 10, "Fire": 11, "Release": 16}
        for name, beats in ticks.items():
            self.assertEqual(f"{beats} * Beat", const(rules, name), name)
        self.assertIn(f"CHOIR_TAPS, CHOIR_VERSE, CHOIR_GATHER, CHOIR_INHALE, CHOIR_FIRE, CHOIR_RELEASE = (1, 36), "
                      f"{2 * beat}, {8 * beat}, {10 * beat}, {11 * beat}, {16 * beat}", gen)
        stagger, rise = int(const(rules, "PipeStagger")), int(const(rules, "PipeRiseTicks"))
        self.assertIn(f"return {9 * beat} + {stagger} * rank + {rise}", gen)
        self.assertIn("return max(2, min(6, voices + 1))", gen)
        self.assertIn("RaisedRanks(int voices) => Math.Clamp(voices + 1, 2, Ranks)", rules)

    def test_every_sung_pitch_is_f_minor_pentatonic(self):
        gen = GENERATOR.read_text(encoding="utf-8")
        names = re.findall(r'"([A-G]b?\d)"', re.search(r"^CHOIR_SUNG = \(([^)]*)\)", gen, re.M).group(1))
        names += re.findall(r'"([A-G]b?\d)"', re.search(r"^CHOIR_PIPES = \(([^)]*)\)", gen, re.M).group(1))
        lines = re.search(r"^CHORUS_LINES = \((.*?)\)\n", gen, re.M | re.S).group(1)
        sung_chorus = re.findall(r'"([A-G]b?\d)"', lines)
        for name in names + sung_chorus:
            letter = name.rstrip("0123456789")
            # The fifth chorus line's Fm9 colour G is the only tone outside F Ab Bb C Eb.
            self.assertIn(letter, {"F", "Ab", "Bb", "C", "Eb"} | ({"G"} if name == "G4" else set()), name)
        self.assertEqual(1, sung_chorus.count("G4"), "G appears once, as the fifth line's Fm9 ninth")


class ChoirCueRouting(unittest.TestCase):
    def setUp(self):
        self.visuals = code(VISUALS)
        self.cues = registry()

    def routed(self):
        names = set(re.findall(r'DollWeaponAudio\.Play\("(\w+)"', self.visuals))
        for array in ("VerseFire", "Pipe", "ChorusFire"):
            body = re.search(rf"static readonly string\[\] {array} =\s*\{{([^}}]*)\}}", self.visuals).group(1)
            names |= set(re.findall(r'"(\w+)"', body))
        return names

    def test_every_routed_choir_cue_is_registered_and_exported(self):
        routed = self.routed()
        self.assertEqual(29, len(routed))
        choir = {name for name, row in self.cues.items() if row[1] == "Choir"}
        self.assertEqual(choir, routed, "every Choir cue is routed and every routed cue is registered")
        exported = {p.stem for p in SOUNDS.glob("*.ogg")}
        self.assertLessEqual(routed, exported)

    def test_releases_pair_a_warning_with_a_firing_cue_and_failure_differs(self):
        routed = self.routed()
        for release in ("ChoirVerse", "ChoirChorus"):
            self.assertIn(release + "Warn", routed)
            self.assertTrue(any(n.startswith(release + "Fire") for n in routed), release)
        self.assertIn("ChoirChorusMiss", routed)
        self.assertIn("ChoirChorusEnd", routed)

    def test_call_site_volumes_match_the_audition(self):
        volumes = {
            "SummonVolume": ["ChoirSummon"], "VerseWarnVolume": ["ChoirVerseWarn"], "OrganRiseVolume": ["ChoirOrganRise"],
            "ChorusWarnVolume": ["ChoirChorusWarn"], "ChorusFireVolume": [f"ChoirChorusFire{i}" for i in range(1, 7)],
            "ChorusEndVolume": ["ChoirChorusEnd"], "MissVolume": ["ChoirChorusMiss"], "NoteHitVolume": ["ChoirNoteHit"],
            "ChorusHitVolume": ["ChoirChorusHit"], "VerseVolume": [f"ChoirVerseFire{i}" for i in range(9)],
            "PipeVolume": [f"ChoirPipe{i}" for i in range(6)],
        }
        for field, names in volumes.items():
            value = float(const(self.visuals, field).rstrip("f"))
            for name in names:
                self.assertAlmostEqual(self.cues[name][3], value, msg=f"{name} audition volume vs {field}")

    def test_a_stopped_chorus_fades_as_the_audition_renders_it(self):
        gen = GENERATOR.read_text(encoding="utf-8")
        rules = RULES.read_text(encoding="utf-8")
        cancel, finish = re.search(r"^CHORUS_CANCEL_FADE, CHORUS_FINISH_FADE = (\d+), (\d+)$", gen, re.M).groups()
        self.assertEqual(const(rules, "CloseTicks"), cancel, "a lost target fades the chorus over CloseTicks")
        self.assertEqual(const(self.visuals, "FinishFadeTicks"), finish, "a target that died fades it under the quiet close")
        # A target that died closes with the success cue; only a loss sounds the failure.
        finish_body = self.visuals[self.visuals.index("private static void Finish("):self.visuals.index("private static void Cancel(")]
        self.assertIn('DollWeaponAudio.Play("ChoirChorusEnd"', finish_body)
        self.assertNotIn("ChoirChorusMiss", finish_body)
        self.assertIn("Died(o.Target)", self.visuals)

    def test_cues_play_on_the_accepted_clock_through_the_shared_player(self):
        self.assertIn("DollCueClock.Take(ref o.Cues[SlotFire], previous, clock, ChoirConcertRules.Fire)", self.visuals)
        self.assertIn("DollCueClock.Take(ref summonCue, previousLife, voice.Life, 1)", self.visuals)
        self.assertNotIn("SoundEngine.PlaySound", self.visuals, "every cue goes through DollWeaponAudio")
        self.assertNotIn("Reduced", self.visuals.split("internal static bool Emit(")[0], "Reduced Effects never changes audio")
        self.assertIn("RitualWeaponFeedback>().Kick(owner, 3)", self.visuals)
        audio = code(AUDIO)
        for i in range(9):
            self.assertIn(f'["ChoirVerseFire{i}"] = 4', audio, "rolling verse parts need more than the default two voices")


class ChoirMaterialAndWiring(unittest.TestCase):
    def test_material_is_branchless_on_uniforms_and_declares_its_passes(self):
        shader = SHADER.read_text(encoding="utf-8")
        body = re.sub(r"//[^\n]*", "", shader)
        uniforms = {"tick", "sparkleShift", "detail", "throat", "throatInverse", "dotOrigin"}
        for match in re.finditer(r"\bif\s*\(([^)]*)\)", body):
            self.assertFalse(set(re.findall(r"\w+", match.group(1))) & uniforms, f"uniform branch: {match.group(0)}")
        # Uniform-only quantities come from the CPU, not preshader math: no raw clock or Reduced flag in the shader.
        for gone in ("float clock;", "float reduced;", "float throatLength;"):
            self.assertNotIn(gone, body)
        apply = code(PRESENTATION)
        apply = apply[apply.index("public bool Apply(GraphicsDevice device"):]
        for name in ("tick", "sparkleShift", "detail", "throat", "throatInverse"):
            self.assertIn(f'DollPixelArt.Set(effect, "{name}"', apply)
        for name in ("Hymn", "Iris", "Trail"):
            self.assertIn(f"pass {name} {{", shader)
        self.assertIn('passes = { "Hymn", "Iris", "Trail" }', PRESENTATION.read_text(encoding="utf-8"))
        self.assertIn('ShaderName = "Convergence.DollChoirEnergy"', PRESENTATION.read_text(encoding="utf-8"))

    def test_presentation_and_rules_stay_linkable_offline(self):
        for path in (PRESENTATION, RULES):
            text = path.read_text(encoding="utf-8")
            self.assertIsNone(re.search(r"^using\s+(Terraria|ReLogic|Luminance)", text, re.M), f"{path.name} links offline")
        self.assertIsNone(re.search(r"^using\s+(Microsoft\.Xna|Terraria|ReLogic|Luminance)", RULES.read_text(encoding="utf-8"), re.M))
        project = (ROOT / "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj").read_text(encoding="utf-8")
        for linked in ("Content/Encounters/FirstSeverance/Rewards/ChoirConcertRules.cs",
                       "Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs"):
            self.assertIn(linked, project)
        preview = (TOOLS / "preview-doll-choir.ps1").read_text(encoding="utf-8")
        for linked in ("ChoirPresentation.cs", "ChoirConcertRules.cs", "DollArtAnchors.g.cs", "DollChoirPreview.cs"):
            self.assertIn(linked, preview)

    def test_item_points_at_the_new_types_and_leaves_the_legacy_visuals(self):
        items = code(ITEMS)
        choir = items[items.index("public sealed class ChoirOfTheUnmade"):items.index("public sealed class LastWitness")]
        self.assertIn('Texture => "Convergence/Assets/Textures/Items/DollWeapons/ChoirOfTheUnmadeIcon"', choir)
        self.assertIn("Item.shoot = ModContent.ProjectileType<ChoirChorister>()", choir)
        self.assertNotIn("Pose(", choir, "the RitualArmament pose call is gone from the Choir")
        self.assertNotIn("ChoirSentinel", choir)
        buff = code(BUFF)
        self.assertIn("ownedProjectileCounts[ModContent.ProjectileType<ChoirChorister>()]", buff)
        self.assertIn('Texture => "Convergence/Assets/Textures/Items/DollWeapons/ChoirOfTheUnmadeBuff"', buff)
        excluded = re.search(r"entity\.ModItem is not \(([^)]*)\)", code(LEGACY_VISUALS)).group(1).split(" or ")
        self.assertIn("NullRefrain", excluded)
        self.assertIn("ChoirOfTheUnmade", excluded, "no V3 art for the refreshed item")
        concert = code(CONCERT)
        for name in ("ChoirChorister", "ChoirSungNote", "ChoirChorus"):
            self.assertIn(f"public sealed class {name} : ModProjectile", concert)
        self.assertNotIn("ModPacket", concert)
        self.assertNotIn("SendData", concert)

    def test_icons_exist_at_their_stored_scale(self):
        for name, size in (("ChoirOfTheUnmadeIcon", (54, 56)), ("ChoirOfTheUnmadeBuff", (32, 32))):
            data = (ROOT / f"Assets/Textures/Items/DollWeapons/{name}.png").read_bytes()
            self.assertEqual(size, (int.from_bytes(data[16:20], "big"), int.from_bytes(data[20:24], "big")), name)


@unittest.skipUnless(numeric_stack(), "numpy, scipy and soundfile are local audio tools, not CI")
class ChoirRender(unittest.TestCase):
    def test_renders_are_reproducible(self):
        sys.path.insert(0, str(TOOLS))
        import generate_doll_weapon_sfx as gen
        store = gen.Store(None, (ROOT / "Assets/ATTRIBUTION.md").read_text(encoding="utf-8"))
        with tempfile.TemporaryDirectory() as folder:
            for name in ("ChoirVerseFire3", "ChoirChorusMiss", "ChoirChorusFire4", "ChoirChorusEnd"):
                with self.subTest(cue=name):
                    gen.render_cue(name, store, Path(folder))
                    rendered = (Path(folder) / f"{name}.ogg").read_bytes()
                    self.assertEqual(hashlib.sha256((SOUNDS / f"{name}.ogg").read_bytes()).hexdigest(),
                                     hashlib.sha256(rendered).hexdigest(), f"{name} reproduces byte for byte")

    def test_the_chorus_closes_restrained(self):
        """The owner's taste for big closings (2026-10-03): a quiet organ and a soft gong with a long tail, not an organ
        played out loud. The open fifth falls away into the cut and ChoirChorusEnd stays at most T2 with a long tail."""
        sys.path.insert(0, str(TOOLS))
        import numpy as np
        import soundfile as sf
        import generate_ebon_sfx as base
        import generate_doll_weapon_sfx as gen

        def short_term(x):
            power = (base.k_weight(x) ** 2).sum(axis=1)
            block = round(0.4 * gen.RATE)
            c = np.concatenate(([0.0], np.cumsum(power)))
            return -0.691 + 10 * np.log10(np.maximum((c[block:] - c[:-block]) / block, 1e-12))

        for voices in range(1, 7):
            x, rate = sf.read(str(SOUNDS / f"ChoirChorusFire{voices}.ogg"), always_2d=True)
            st = short_term(x)
            last = st[round((gen.CHORUS_CUT - 0.4) * rate)]
            self.assertLessEqual(last, st.max() - 3.5, f"ChoirChorusFire{voices}: the last 0.4 s before the cut falls away")
        end, rate = sf.read(str(SOUNDS / "ChoirChorusEnd.ogg"), always_2d=True)
        self.assertLessEqual(gen.CUES["ChoirChorusEnd"].target_lufs, -13, "the close stays at most T2")
        self.assertLessEqual(gen.loudness(end), -12.9)
        self.assertGreaterEqual(gen.character(end)["decay_20db_ms"], 1500, "a long decaying tail")
        self.assertGreaterEqual(len(end) / rate, 2.5)


if __name__ == "__main__":
    unittest.main()
