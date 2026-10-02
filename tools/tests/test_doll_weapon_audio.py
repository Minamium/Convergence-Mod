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


def loops():
    """Cues registered with loop=True: sample-exact PCM16 WAV loops instead of Ogg one-shots."""
    text = GENERATOR.read_text(encoding="utf-8")
    return {name for name, args in re.findall(r'@cue\("(\w+)",(.*?)\)\ndef ', text, re.S) if "loop=True" in args}


def exports():
    """Exported cue files by stem: Ogg one-shots and WAV loops."""
    return {p.stem: p for p in SOUNDS.iterdir() if p.suffix in (".ogg", ".wav")}


def wav_info(path):
    """(format, channels, rate, bits, frames) of a RIFF WAV file."""
    data = path.read_bytes()
    if data[:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise AssertionError(f"{path.name}: not a RIFF WAVE file")
    position, fmt, frames = 12, None, None
    while position + 8 <= len(data):
        chunk, size = data[position:position + 4], struct.unpack_from("<I", data, position + 4)[0]
        if chunk == b"fmt ":
            fmt = struct.unpack_from("<HHIIHH", data, position + 8)
        elif chunk == b"data":
            frames = size
        position += 8 + size + (size & 1)
    if fmt is None or frames is None:
        raise AssertionError(f"{path.name}: missing fmt or data chunk")
    audio_format, channels, rate, _, block, bits = fmt
    return audio_format, channels, rate, bits, frames // block


def csharp_ints(text, name):
    match = re.search(rf"\b{name}\s*=\s*\{{([^}}]*)\}}", text) or re.search(rf"\b{name}\s*=\s*(-?\d+)\b", text)
    return [int(v) for v in re.findall(r"-?\d+", match.group(1))]


def numeric_stack():
    return all(importlib.util.find_spec(name) for name in ("numpy", "scipy", "soundfile"))


class DollWeaponExports(unittest.TestCase):
    def test_registry_and_exports_match(self):
        cues = registry()
        self.assertIn("CompanionSummon", cues)
        self.assertEqual(set(cues), set(exports()))
        self.assertEqual(set(cues) - loops(), {p.stem for p in SOUNDS.glob("*.ogg")}, "one-shots are Ogg")
        self.assertEqual(loops(), {p.stem for p in SOUNDS.glob("*.wav")}, "loops, and only loops, are WAV")
        self.assertEqual([], sorted(p.name for p in SOUNDS.iterdir() if p.suffix not in (".ogg", ".wav")))

    def test_loops_are_sample_exact_stereo_pcm16(self):
        cues = registry()
        self.assertIn("LacunaBeamLoop", loops())
        for name in sorted(loops()):
            with self.subTest(cue=name):
                audio_format, channels, rate, bits, frames = wav_info(SOUNDS / f"{name}.wav")
                self.assertEqual((1, 2, 44100, 16), (audio_format, channels, rate, bits))
                self.assertEqual(round(cues[name][1] * 44100), frames, "a loop is exactly its registered length")
        # The Lacuna beam's loop spans eight of the beam's 30-tick pulse periods.
        self.assertEqual(8 * 30 / 60, cues["LacunaBeamLoop"][1])

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
        for clip in sorted(exports().values()):
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

    def test_lacuna_schedule_is_mirrored_by_the_generator(self):
        # The composite Lacuna cues bake their inner beats against the weapon's ticks: the generator's LACUNA table
        # must equal LacunaTestamentScore and the call-site constants of LacunaVisuals.
        score = (ROOT / "Content/Encounters/FirstSeverance/Rewards/LacunaTestamentScore.cs").read_text(encoding="utf-8")
        visuals = (ROOT / "Client/Encounters/FirstSeverance/Weapons/LacunaVisuals.cs").read_text(encoding="utf-8")
        py = GENERATOR.read_text(encoding="utf-8")
        table = re.search(r"^LACUNA = \{(.*?)^\}", py, re.S | re.M).group(1)

        def mirrored(key):
            return [int(v) for v in re.findall(r"-?\d+", re.search(rf'"{key}":\s*(\([^)]*\)|\d+)', table).group(1))]

        self.assertEqual(csharp_ints(score, "births"), mirrored("births"))
        self.assertEqual(csharp_ints(score, "clicks"), mirrored("clicks"))
        fire = csharp_ints(score, "Fire")[0]
        self.assertEqual([fire + 120, fire + 240, fire + 360], mirrored("widen"))
        for cs_name, key in (("ShotDelay", "shot_delay"), ("ShotPeriod", "shot_period"), ("ShotTell", "shot_tell"),
                             ("FrameParting", "frame_parting"), ("FrameHalf", "frame_half"), ("Merge", "merge"),
                             ("Formed", "formed"), ("Fire", "fire"), ("PulsePeriod", "pulse_period")):
            self.assertEqual(csharp_ints(score, cs_name), mirrored(key), cs_name)
        self.assertIn('"docked": tuple(350 + 2 * i for i in range(7))', table)
        self.assertIn("internal static int Docked(int iris) => 350 + 2 * Math.Clamp(iris, 0, Irises - 1);", score)
        self.assertEqual(int(re.search(r"^LACUNA_NOTE_ROOT = (\d+)", py, re.M).group(1)),
                         int(re.search(r"NoteRoot = (\d+);", visuals).group(1)), "single-note cues share their recorded root")
        cues = registry()
        for name in re.findall(r'DollWeaponAudio\.(?:Play|Note|Sustain)\((?:ref \w+, )?"(\w+)"', visuals):
            self.assertIn(name, cues)
            constant = {"LacunaIrisWarn": "IrisWarnVolume", "LacunaIrisFire": "IrisFireVolume", "LacunaPelletWarn": "PelletWarnVolume",
                        "LacunaPelletFire": "PelletFireVolume", "LacunaPelletHit": "PelletHitVolume", "LacunaMergeWarn": "MergeWarnVolume",
                        "LacunaMergeFire": "MergeFireVolume", "LacunaBeamWarn": "BeamWarnVolume", "LacunaBeamFire": "BeamFireVolume",
                        "LacunaBeamLoop": "LoopVolume", "LacunaWiden1": "WidenVolume", "LacunaWiden2": "WidenVolume",
                        "LacunaWiden3": "WidenVolume", "LacunaBeamHit": "BeamHitVolume", "LacunaBeamEnd": "EndVolume",
                        "LacunaBeamMiss": "MissVolume"}[name]
            value = float(re.search(rf"\b{constant} = ([\d.]+)f", visuals).group(1))
            self.assertEqual(cues[name][2], value, f"{name}: the audition plays the call-site volume")


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
        self.assertIn('Identifier = $"Convergence:DollWeapon:Sustain:{owner}:{identity}:{cue}"', sustain)
        self.assertIn("IsLooped = true, MaxInstances = 1", sustain)
        self.assertIn("RitualAudioDiagnostics.Track(cue, voice, gain)", self.source)

    def test_sustain_gain_lives_on_the_lease_so_a_loop_can_fade_in_from_zero(self):
        sustain = body(self.source, "internal static void Sustain(")
        self.assertIn("IsLooped = true, MaxInstances = 1, Volume = 1f", sustain, "the loop style is built at full volume")
        self.assertIn("sound.Volume = started.Gain;", sustain, "the update callback applies the lease gain")
        self.assertIn("active.Volume = gain;", sustain, "a refresh applies the new gain directly")
        self.assertNotIn("Style.Volume", sustain, "no division by the style volume (a loop that starts at 0 would stay silent)")
        self.assertNotIn("Math.Max(.0001f", sustain)

    def test_sustain_builds_no_string_on_a_call_that_finds_its_lease(self):
        self.assertIn("Dictionary<LeaseKey, Lease> sustains", self.source)
        self.assertIn("private readonly record struct LeaseKey(int Owner, int Identity, string Cue);", self.source)
        sustain = body(self.source, "internal static void Sustain(")
        self.assertIn("LeaseKey key = new(owner, identity, cue);", sustain)
        self.assertIn("sustains.TryGetValue(key, out var lease)", sustain)
        self.assertIn("sustains[key] = started;", sustain)
        self.assertEqual(1, sustain.count('$"'), "the identifier is the only interpolated string")
        self.assertLess(sustain.index("Admit()"), sustain.index('$"'), "built only when a voice starts")
        self.assertLess(sustain.index("slot = lease.Voice;"), sustain.index('$"'), "the refresh path returns before it")

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
        self.assertLessEqual(routed, set(exports()))
        # A loop is only ever sustained, and only a loop is.
        sustained = set()
        for path in (ROOT / "Client").rglob("*.cs"):
            sustained |= set(re.findall(r'DollWeaponAudio\.Sustain\(ref \w+, "(\w+)"', path.read_text(encoding="utf-8")))
        self.assertEqual(sustained & set(exports()), loops() & routed)


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

    def test_every_cue_sits_on_its_tier_under_the_peak_ceiling(self):
        import numpy as np
        import soundfile as sf
        gen = self.gen
        for name, path in sorted(exports().items()):
            with self.subTest(cue=name):
                x, rate = sf.read(str(path), always_2d=True, dtype="float64")
                self.assertEqual(44100, rate)
                spec = gen.CUES[name]
                looped = np.concatenate((x, x)) if spec.loop else x
                # The peak ceiling may hold a transient cue a little under its tier, never over it.
                self.assertLessEqual(gen.loudness(looped), spec.target_lufs + 0.5)
                self.assertGreaterEqual(gen.loudness(looped), spec.target_lufs - 2.0)
                self.assertLessEqual(gen.loop_peak_db(x) if spec.loop else gen.true_peak_db(x), -1.0)

    def test_loops_are_seamless(self):
        import soundfile as sf
        for name in sorted(loops()):
            with self.subTest(cue=name):
                x, _ = sf.read(str(SOUNDS / f"{name}.wav"), always_2d=True, dtype="float64")
                seam = self.gen.seam(x)
                self.assertLessEqual(seam["wrap_step"], 0.5 * seam["max_inner_step"], "the wrap is no larger than an inner step")
                self.assertLessEqual(abs(seam["head_tail_rms_db"]), 1.0, "head and tail sit at the same level")

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

    def test_lacuna_cues_are_reproducible(self):
        # Every Lacuna cue except LacunaMergeFire (which reads a store recording) is pure synthesis.
        store = self.gen.Store(self.gen.find_store(), (ROOT / "Assets/ATTRIBUTION.md").read_text(encoding="utf-8"))
        names = [n for n, c in self.gen.CUES.items() if c.group == "Lacuna" and (n != "LacunaMergeFire" or store.root)]
        with tempfile.TemporaryDirectory() as folder:
            for name in names:
                with self.subTest(cue=name):
                    self.gen.render_cue(name, store, Path(folder))
                    suffix = ".wav" if self.gen.CUES[name].loop else ".ogg"
                    self.assertEqual((SOUNDS / f"{name}{suffix}").read_bytes(), (Path(folder) / f"{name}{suffix}").read_bytes(),
                                     "the committed cue is reproducible")


if __name__ == "__main__":
    unittest.main()
