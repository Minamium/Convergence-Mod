"""Doll weapon audio: exported cues, playback contract and the tuning mirror; not a subjective listening test.

The export, routing and contract checks are pure Python (CI). The render checks need numpy, scipy, soundfile
and the local recording store (tools/generate_doll_weapon_sfx.py find_store) and are skipped without them.
"""
from pathlib import Path
import hashlib
import importlib.util
import re
import struct
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "tools"
SOUNDS = ROOT / "Assets/Sounds/Weapons/DollWeapons"
GENERATOR = TOOLS / "generate_doll_weapon_sfx.py"
AUDIO = ROOT / "Client/Encounters/FirstSeverance/Weapons/DollWeaponAudio.cs"
COMPANION = ROOT / "Client/Encounters/FirstSeverance/DollCompanionVisuals.cs"
TUNING = ROOT / "Content/Encounters/FirstSeverance/Rewards/DollWeaponTuning.cs"
LEGACY_SUMMON = ROOT / "Assets/Sounds/Weapons/DollTheater/DollSummon.wav"


def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if not depth:
            return source[start:end + 1]
    raise AssertionError("Unclosed body: " + signature)


def ogg_pages(data):
    position = 0
    while position < len(data):
        if data[position:position + 4] != b"OggS":
            raise AssertionError("broken Ogg page layout")
        granule, serial = struct.unpack_from("<qI", data, position + 6)
        count = data[position + 26]
        size = sum(data[position + 27:position + 27 + count])
        yield granule, serial, data[position + 27 + count:position + 27 + count + size]
        position += 27 + count + size


def vorbis_info(path):
    """Channels, sample rate, seconds and the stream serials, from the identification header and last granule."""
    pages = list(ogg_pages(path.read_bytes()))
    first = pages[0][2]
    if first[:7] != b"\x01vorbis":
        raise AssertionError(f"{path.name}: first packet is not a Vorbis identification header")
    channels, rate = first[11], struct.unpack("<I", first[12:16])[0]
    return channels, rate, pages[-1][0] / rate, {serial for _, serial, _ in pages}


def registry():
    """The generator's @cue registrations: name -> (target LUFS, max seconds, call-site volume)."""
    rows = re.findall(r'@cue\("(\w+)",\s*(-?[\d.]+),\s*"[^"]*",\s*([\d.]+),\s*([\d.]+),', GENERATOR.read_text(encoding="utf-8"))
    return {name: (float(target), float(limit), float(volume)) for name, target, limit, volume in rows}


def numeric_stack():
    return all(importlib.util.find_spec(name) for name in ("numpy", "scipy", "soundfile"))


class DollWeaponExports(unittest.TestCase):
    def test_registry_and_exports_match(self):
        cues = registry()
        self.assertIn("CompanionSummon", cues)
        self.assertEqual(set(cues), {p.stem for p in SOUNDS.glob("*.ogg")})
        self.assertEqual([], sorted(p.name for p in SOUNDS.iterdir() if p.suffix != ".ogg"), "one-shots are Ogg only")

    def test_exports_are_stereo_vorbis_within_budget_with_pinned_serial(self):
        cues = registry()
        for clip in sorted(SOUNDS.glob("*.ogg")):
            with self.subTest(cue=clip.stem):
                channels, rate, seconds, serials = vorbis_info(clip)
                self.assertEqual((2, 44100), (channels, rate))
                self.assertGreater(seconds, 0.05)
                self.assertLessEqual(seconds, cues[clip.stem][1])
                pinned = int.from_bytes(hashlib.sha256(clip.stem.encode("utf-8")).digest()[:4], "little")
                self.assertEqual({pinned}, serials, "every page carries the serial pinned from the file stem")

    def test_companion_summon_length_budget(self):
        self.assertLessEqual(registry()["CompanionSummon"][1], 1.3)
        self.assertLessEqual(vorbis_info(SOUNDS / "CompanionSummon.ogg")[2], 1.3)

    def test_every_export_has_an_exact_attribution_record(self):
        text = (ROOT / "Assets/ATTRIBUTION.md").read_text(encoding="utf-8")
        for clip in sorted(SOUNDS.glob("*.ogg")):
            with self.subTest(cue=clip.stem):
                rel = clip.relative_to(ROOT).as_posix()
                start = text.index(f"- Runtime file: `{rel}`")
                record = text[start:text.index("\n\n", start)]
                self.assertIn(f"- SHA256: `{hashlib.sha256(clip.read_bytes()).hexdigest()}`", record)
                self.assertIn(f"- Asset ID: doll-weapon-sfx-{clip.stem.lower()}-", record)

    def test_tuning_ladder_is_mirrored_by_the_generator(self):
        cs = TUNING.read_text(encoding="utf-8")
        py = (TOOLS / "doll_sfx_dsp.py").read_text(encoding="utf-8")
        names = re.search(r"LadderNames = \{([^}]*)\}", cs).group(1)
        ladder = re.search(r"^LADDER = \(([^)]*)\)", py, re.MULTILINE).group(1)
        self.assertEqual(re.findall(r'"(\w+)"', names), re.findall(r'"(\w+)"', ladder))
        self.assertEqual(re.findall(r"\d+", re.search(r"Pentatonic = \{([^}]*)\}", cs).group(1)),
                         re.findall(r"\d+", re.search(r"^PENTATONIC = \(([^)]*)\)", py, re.MULTILINE).group(1)))
        self.assertIn("DefaultCents = 0", cs)


class DollWeaponPlaybackContract(unittest.TestCase):
    def setUp(self):
        # Code only: the file's comments may name what it deliberately avoids.
        self.source = re.sub(r"//[^\n]*", "", AUDIO.read_text(encoding="utf-8"))

    def test_weapon_audio_is_not_muted_by_reduced_effects(self):
        self.assertNotIn("Reduced", self.source)
        play = body(self.source, "internal static SlotId Play(")
        self.assertIn("SoundEngine.PlaySound", play)
        self.assertIn("style.Volume = Math.Clamp(volume, 0f, 1f)", play, "volume is applied as given")
        for callback in (play, body(self.source, "internal static void Sustain(")):
            self.assertNotIn("* 2", callback, "no RitualWeaponFeedback-style doubling")
            self.assertNotIn("Reduced", callback)
            self.assertNotIn("VisualConfig", callback)

    def test_dedicated_server_and_menu_never_build_a_style(self):
        self.assertIn("private static bool Audible => !Main.dedServ && !Main.gameMenu;", self.source)
        play = body(self.source, "internal static SlotId Play(")
        self.assertLess(play.index("Audible"), play.index("Style(cue)"))
        sustain = body(self.source, "internal static void Sustain(")
        self.assertLess(sustain.index("Audible"), sustain.index("new(Root + cue)"))
        self.assertEqual(1, self.source.count("new SoundStyle("), "the cached one-shot style is built only in Style()")
        self.assertIn("[Autoload(Side = ModSide.Client)]", self.source)

    def test_voices_are_bounded_and_never_cut_by_a_new_one(self):
        self.assertIn('Root = "Convergence/Assets/Sounds/Weapons/DollWeapons/"', self.source)
        self.assertIn("VoiceCap = 32", self.source)
        self.assertNotIn("ReplaceOldest", self.source)
        self.assertEqual(2, self.source.count("SoundLimitBehavior = SoundLimitBehavior.IgnoreNew"))
        self.assertEqual(2, self.source.count("PauseBehavior = PauseBehavior.StopWhenGamePaused"))
        self.assertEqual(2, self.source.count("PlayOnlyIfFocused = true"))
        self.assertIn("return voices.Count < VoiceCap;", body(self.source, "private static bool Admit("))
        self.assertIn("ModContent.HasAsset(Root + cue)", body(self.source, "private static bool Exists("))
        self.assertIn('Identifier = "Convergence:DollWeapon:" + cue, MaxInstances = Instances(cue)', self.source)
        sustain = body(self.source, "internal static void Sustain(")
        self.assertIn('string id = $"Convergence:DollWeapon:Sustain:{owner}:{identity}:{cue}";', sustain)
        self.assertIn("Identifier = id, IsLooped = true, MaxInstances = 1", sustain)
        self.assertIn("RitualAudioDiagnostics.Track(cue, voice, gain)", self.source)

    def test_cleanup_is_owned_and_idempotent(self):
        self.assertIn("public override void OnWorldUnload() => DollWeaponAudio.StopAll();", self.source)
        self.assertIn("public override void Unload() => DollWeaponAudio.Reset();", self.source)
        stop_all = body(self.source, "internal static void StopAll(")
        self.assertIn("voices.Clear()", stop_all)
        self.assertIn("sustains.Clear()", stop_all)
        self.assertIn("StopAll()", body(self.source, "internal static void Reset("))

    def test_companion_summon_keeps_its_trigger_and_matches_the_old_level(self):
        companion = COMPANION.read_text(encoding="utf-8")
        line = re.search(r'if \(first\) Weapons\.DollWeaponAudio\.Play\("CompanionSummon", p\.Center, ([\d.]+)f\);', companion)
        self.assertIsNotNone(line, "the summon keeps the `first` trigger and plays through DollWeaponAudio")
        self.assertNotIn('"DollSummon"', companion)
        self.assertEqual(registry()["CompanionSummon"][2], float(line.group(1)), "the audition plays the call-site volume")
        self.assertIn('["CompanionSummon"] = 2', self.source)
        self.assertTrue(LEGACY_SUMMON.is_file(), "the old master stays on disk")

    def test_every_routed_cue_is_exported(self):
        routed = set()
        for path in (ROOT / "Client").rglob("*.cs"):
            routed |= set(re.findall(r'DollWeaponAudio\.(?:Play|Note|Sustain)\((?:ref \w+, )?"(\w+)"', path.read_text(encoding="utf-8")))
        self.assertTrue(routed)
        self.assertLessEqual(routed, {p.stem for p in SOUNDS.glob("*.ogg")})


@unittest.skipUnless(numeric_stack(), "numpy, scipy and soundfile are local audio tools, not CI")
class DollWeaponRender(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        sys.path.insert(0, str(TOOLS))
        import generate_doll_weapon_sfx as generator
        cls.gen = generator

    def test_loudness_true_peak_and_level_parity(self):
        import soundfile as sf
        gen = self.gen
        x, rate = sf.read(str(SOUNDS / "CompanionSummon.ogg"), always_2d=True, dtype="float64")
        self.assertEqual(44100, rate)
        self.assertLessEqual(len(x) / rate, 1.3)
        self.assertLessEqual(gen.true_peak_db(x), -1.0)
        spec = gen.CUES["CompanionSummon"]
        lufs = gen.loudness(x)
        self.assertAlmostEqual(spec.target_lufs, lufs, delta=0.5)
        old, old_rate = sf.read(str(LEGACY_SUMMON), always_2d=True, dtype="float64")
        self.assertEqual(44100, old_rate)
        old = old.repeat(2, axis=1) if old.shape[1] == 1 else old
        legacy = gen.loudness(old) + 20 * __import__("math").log10(0.325 * 2)  # RitualWeaponFeedback doubled .325
        effective = lufs + 20 * __import__("math").log10(spec.volume)
        self.assertAlmostEqual(legacy, effective, delta=1.0, msg="the summon keeps the old effective loudness")

    def test_generation_is_deterministic(self):
        store_root = self.gen.find_store()
        if store_root is None:
            self.skipTest("local recording store not found")
        store = self.gen.Store(store_root, (ROOT / "Assets/ATTRIBUTION.md").read_text(encoding="utf-8"))
        with tempfile.TemporaryDirectory() as first, tempfile.TemporaryDirectory() as second:
            self.gen.render_cue("CompanionSummon", store, Path(first))
            self.gen.render_cue("CompanionSummon", store, Path(second))
            a = (Path(first) / "CompanionSummon.ogg").read_bytes()
            b = (Path(second) / "CompanionSummon.ogg").read_bytes()
        self.assertEqual(a, b, "two renders are byte-identical")
        self.assertEqual(a, (SOUNDS / "CompanionSummon.ogg").read_bytes(), "the committed cue is reproducible")


if __name__ == "__main__":
    unittest.main()
