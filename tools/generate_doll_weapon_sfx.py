"""Doll reward weapon cues and the companion summon (shared audio basis of the Doll weapon refresh).

Renders Assets/Sounds/Weapons/DollWeapons/<Cue>.ogg (a loop: <Cue>.wav, PCM16, sample-exact). Every cue is one builder function registered
with @cue in CUES (short-term loudness target, group, maximum length, in-game playback volume and
the owner audition text); later weapon changes add their cues here and reuse the building blocks
of tools/doll_sfx_dsp.py (music-box comb tooth on the F minor pentatonic ladder, brass ratchet,
porcelain ring and crack, additive organ pad, shimmer, low thump). A weapon's section may add its own
synthesis (the Choir's faceless formant voice) and a SEQUENCES entry: the build-up-to-release sequence the
audition page plays at in-game levels, alone and over the Doll music.

Recordings come only from the pinned SOURCES table: CC0 recordings already attributed for the
Ebon and Cathedral cues, read from the local store (--store, never committed) and project-owned
masters (repo:). Each is SHA-256 checked before use; a store recording must also be listed with
that hash in Assets/ATTRIBUTION.md, a project master by its runtime path.

Loudness is the Ebon scale (generate_ebon_reward_sfx helpers, imported unmodified): BS.1770
K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip.
Deterministic: a cue's random stream is seeded from its name and the Ogg stream serial is pinned
from the file stem (generate_ebon_sfx.write_ogg), so a rerun, or --only, is byte-identical.
Requires numpy, scipy and soundfile (local audio tools, not CI).

One-shots are Vorbis; a cue registered with loop=True (its name ends in Loop) is a PCM16 WAV loop that repeats
sample-exactly (no trim, fade or causal filter across its seam), mastered as its loop_master says (Cue).

Run: py -3.12 tools/generate_doll_weapon_sfx.py [--store DIR] [--only CUE,...] [--group GROUP] [--output DIR]
     [--preview DIR] [--attribution-section FILE] [--previous DIR] [--witness-audition DIR] [--witness-attribution FILE]
     e.g. --group Claws --preview .local/audition/doll-claws for Lacrimosa's Claws and its audition page.
"""
import argparse
import hashlib
import html
import io
import json
import os
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import numpy as np
import soundfile as sf
from scipy import signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_ebon_sfx as base  # noqa: E402  (shared helpers; its CLI only runs as __main__)
from generate_ebon_sfx import RATE, fades, hp, lp, pan, place, seconds, sha256, speed, trim  # noqa: E402
from generate_ebon_reward_sfx import loudness, master, room, soft, true_peak_db  # noqa: E402
import doll_sfx_dsp as dsp  # noqa: E402
import generate_ebon_reward_sfx as reward  # noqa: E402  (sweep, ratchet, sparks, taper: shared helpers, unmodified)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "Weapons" / "DollWeapons"
ATTRIBUTION = ROOT / "Assets" / "ATTRIBUTION.md"
SEED_PREFIX = "DollWeapon:"
TICK = 1 / 60
SIXTEENTH = 60 / 218 / 4  # the Doll BGM source tempo (DistantLiturgy, 218 BPM): one sixteenth = 4.13 ticks

# key: (path, origin, SHA-256, author). A path under the local store ('!' = zip member) or 'repo:' + a path
# in this repository. Store recordings are the CC0 files already in the Assets/ATTRIBUTION.md tables.
KENNEY = "sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip"
SOURCES = {
    "metal_click": (KENNEY + "!OGG/metalClick.ogg",
                    "Kenney RPG Audio metalClick.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                    "9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3", "Kenney"),
    "metal_latch": (KENNEY + "!OGG/metalLatch.ogg",
                    "Kenney RPG Audio metalLatch.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                    "ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708", "Kenney"),
    "metal_pot": (KENNEY + "!OGG/metalPot1.ogg",
                  "Kenney RPG Audio metalPot1.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                  "159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd", "Kenney"),
    "doll_summon": ("repo:Assets/Sounds/Weapons/DollTheater/DollSummon.wav",
                    "Convergence project asset doll-theater-0253-dollsummon (0.3.7 articulation revision)",
                    "f4be1a0733e06ad0262d6703a44fc00a0a01dd9a17a3fb1a3381831d634e8534", "Convergence"),
    # Lacrimosa's Claws (2026-10-03): a CC0 recording already in the Ebon Manor reward audio table (the claws also
    # use air_cut, stick_woosh and chop, the same files and hashes as the Last Witness entries below).
    "low_impact": ("sfx-sources/cc0/impact-FS541029-AudioPapkin-very_low_impact.mp3",
                   "AudioPapkin, very low impact (https://freesound.org/s/541029/, HQ preview, CC0 1.0)",
                   "73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a", "AudioPapkin"),
}


def find_store():
    """The local recording store: $CONVERGENCE_AUDIO_STORE, else the nearest ancestor '.claude-tools' that holds
    sfx-sources (the workspace convention); None when absent (CI)."""
    env = os.environ.get("CONVERGENCE_AUDIO_STORE")
    if env:
        return Path(env)
    for parent in ROOT.parents:
        if (parent / ".claude-tools" / "sfx-sources").is_dir():
            return parent / ".claude-tools"
    return None


class Store:
    """Key -> stereo float64 at 44.1 kHz; every read is hash-checked and recorded for the attribution."""

    def __init__(self, root, attribution_text):
        self.root = Path(root) if root else None
        self.attribution = attribution_text
        self.cache, self.hashes, self.used = {}, {}, set()

    def raw(self, key):
        path, _, expected, _ = SOURCES[key]
        if path.startswith("repo:"):
            data = (ROOT / path[5:]).read_bytes()
            listed = f"`{path[5:]}`" in self.attribution
        else:
            if self.root is None:
                raise RuntimeError(f"{key} needs the local recording store (--store or CONVERGENCE_AUDIO_STORE)")
            if "!" in path:
                archive, member = path.split("!", 1)
                data = zipfile.ZipFile(self.root / archive).read(member)
            else:
                data = (self.root / path).read_bytes()
            listed = expected in self.attribution
        found = sha256(data)
        if found != expected:
            raise RuntimeError(f"Source hash mismatch for {key}: {found} != {expected}")
        if not listed:
            raise RuntimeError(f"Source {key} is not recorded in Assets/ATTRIBUTION.md")
        self.hashes[key] = found
        return data

    def get(self, key):
        self.used.add(key)
        if key not in self.cache:
            x, rate = sf.read(io.BytesIO(self.raw(key)), dtype="float64", always_2d=True)
            if x.shape[1] == 1:
                x = np.repeat(x, 2, axis=1)
            if rate != RATE:
                g = np.gcd(rate, RATE)
                x = signal.resample_poly(x, RATE // g, rate // g, axis=0)
            self.cache[key] = x[:, :2]
        return self.cache[key]


def lay(mix, s, key, start, end, at=0.0, gain=0.0, rate=1.0, hp_=None, lp_=None, fade_in=0.002, fade_out=0.02, side=None):
    """Cut [start, end] s of a recording, varispeed/filter/pan it and add it to the mix at `at` s."""
    x = s.get(key)[round(start * RATE):round(end * RATE)].copy()
    if len(x) < 8:
        raise ValueError(f"Window outside {key}: {start}..{end}")
    if rate != 1.0:
        x = speed(x, rate)
    if hp_:
        x = hp(x, hp_)
    if lp_:
        x = lp(x, lp_)
    if side is not None:
        x = pan(x, side)
    return place(mix, fades(x, fade_in, fade_out), at, gain)


# ---------------------------------------------------------------- cue registry
@dataclass(frozen=True)
class Cue:
    build: Callable
    target_lufs: float   # short-term LUFS of the master (the Ebon scale)
    group: str           # audition page section
    max_seconds: float   # hard length budget, checked after encoding
    volume: float        # SoundStyle volume at the call site (the audition plays it at this level)
    description: str     # owner audition text (Japanese)
    glue: float = 1.6    # tanh bus drive before the loudness match (generate_ebon_reward_sfx.soft)
    loop: bool = False   # a sample-exact PCM16 WAV loop, not an Ogg one-shot (never trimmed or faded; filtered circularly)
    # How a loop is mastered: "round" (render_loop_cue: exactly max_seconds, loudness and true peak over the loop
    # played round), "period" (master() on one period like a one-shot, true peak including the wrap) or "builder"
    # (render_builder_loop: exactly the builder's samples, within max_seconds, by gain and the memoryless bus only).
    loop_master: str = "round"


CUES = {}


def cue(name, target_lufs, group, max_seconds, volume, description, glue=1.6, loop_master="round", loop=False):
    def register(build):
        if name in CUES:
            raise ValueError(f"duplicate cue {name}")
        if loop != name.endswith("Loop"):
            raise ValueError(f"{name}: loops (and only loops) end in Loop")
        CUES[name] = Cue(build, target_lufs, group, max_seconds, volume, description, glue, loop, loop_master)
        return build
    return register


# ---------------------------------------------------------------- cues
# Old DollSummon: 0.55 s at -7.8 LUFS short-term, played by RitualWeaponFeedback at min(.95, .325 x 2) = .65,
# i.e. about -11.5 LUFS effective. CompanionSummon is mastered 1 dB hotter and played at .9 (-0.9 dB):
# the same effective level, with every layer of the new phrase audible.
SUMMON_CLICKS = (0.0, 0.075, 0.14)          # the brass key turns: three ratchet teeth, accelerating
SUMMON_LATCH = 0.195                        # the key lets go: latch, the comb starts to turn
SUMMON_PHRASE = (("F5", 0.235), ("Ab5", 0.235 + SIXTEENTH), ("C6", 0.235 + 2 * SIXTEENTH),
                 ("Eb6", 0.235 + 3 * SIXTEENTH))
SUMMON_LANDING = 0.235 + 5 * SIXTEENTH      # F6 lands an eighth after Eb6, with the porcelain chime
SUMMON_LENGTH = 1.26


@cue("CompanionSummon", -10.5, "Companion", 1.3, 0.9,
     "人形の相棒が現れる。真鍮の鍵が3つ巻かれ（ラチェット）、留め金が外れるとオルゴールが F5→A♭5→C6→E♭6 と上がって "
     "F6 に着地し、磁器のチャイムと柔らかなオルガンの息、短いきらめきで終わる。出だしには旧 DollSummon の"
     "「拡がる到着音」を薄く重ねて、相棒の到着だと分かる性格を残している。")
def companion_summon(s, rng):
    mix = seconds(1.6)
    # Arrival: the old cue's diffuse spectral bloom, thinned so the key and the comb read through it.
    lay(mix, s, "doll_summon", 0.0, 0.42, 0.0, -9.5, hp_=320, lp_=7000, fade_in=0.001, fade_out=0.16)
    place(mix, dsp.thump(150, 62, 0.22, rng), 0.0, -17)
    # The brass key on the doll's back: three accelerating ratchet teeth, then the latch lets go.
    place(mix, dsp.ratchet(SUMMON_CLICKS, rng, freq=2450, gains_db=(-3, -2, -1), side=-0.25), 0.0, -8)
    for at, rate in zip(SUMMON_CLICKS, (1.0, 1.06, 0.97)):  # metalClick's second click starts at 0.2711 s
        lay(mix, s, "metal_click", 0.2705, 0.34, at, -5, rate=rate, hp_=1400, side=-0.3, fade_in=0.0005, fade_out=0.03)
    # metalLatch's catch starts at 0.0418 s.
    lay(mix, s, "metal_latch", 0.041, 0.20, SUMMON_LATCH, -10, rate=1.12, hp_=900, side=-0.2, fade_in=0.0005, fade_out=0.08)
    place(mix, pan(dsp.brass_click(rng, 1900, decay=0.007, thud=0.6), -0.2), SUMMON_LATCH, -10)
    # The comb turns: a rising F minor pentatonic phrase, each tooth a little to the right of the last.
    for i, (note, at) in enumerate(SUMMON_PHRASE):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.9, rng), -0.15 + 0.1 * i), at, -8 + 0.5 * i)
    # The landing: F6 with Ab6 and C7 struck a hair later (an open F minor chord above the BGM's dense mids).
    place(mix, pan(dsp.box_tine(dsp.hz("F6"), 1.0, rng), 0.15), SUMMON_LANDING, -3)
    place(mix, pan(dsp.box_tine(dsp.hz("Ab6"), 0.8, rng), -0.3), SUMMON_LANDING + 0.004, -12)
    place(mix, pan(dsp.box_tine(dsp.hz("C7"), 0.7, rng), 0.35), SUMMON_LANDING + 0.008, -10)
    # Porcelain chime on the landing (a dry ceramic ring an octave above, never a long bell).
    place(mix, dsp.porcelain_ring(dsp.hz("F7"), 0.5, rng, decay=0.09, side=0.3), SUMMON_LANDING + 0.003, -10)
    place(mix, dsp.porcelain_ring(dsp.hz("C8"), 0.3, rng, decay=0.05, side=-0.3), SUMMON_LANDING + 0.018, -18)
    # A soft organ breath under the phrase: F minor (F3 C4 F4 Ab4) and its wind, swelling into the landing
    # and gone before the shimmer ends.
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4", "Ab4")], 0.86, rng, attack=0.38, release=0.45,
                        harmonics=8, rolloff=1.5, chiff=0.03, breath=0.22)
    place(mix, pad, 0.2, -18)
    # A brief shimmer as the doll settles.
    place(mix, dsp.shimmer(0.6, rng, count=10), SUMMON_LANDING + 0.05, -17)
    return room(mix, 0.14, SUMMON_LENGTH, 0.3)


# ---------------------------------------------------------------- Lacuna Testament
# The score mirrors Content/Encounters/FirstSeverance/Rewards/LacunaTestamentScore.cs (pinned by
# tools/tests/test_doll_weapon_audio.py): ticks of the held controller's age, 60 per second. Cues fire on these
# ticks through DollCueClock; the composite cues below bake their inner beats against them.
LACUNA = {
    "births": (0, 108, 183, 233, 269, 296, 316),
    "shot_delay": 15, "shot_period": 38, "shot_tell": 4, "frame_parting": 8, "frame_half": 11,
    "merge": 340, "formed": 362, "fire": 410,
    "docked": tuple(350 + 2 * i for i in range(7)),
    "clicks": (368, 378, 386, 392, 397, 401, 404),
    "widen": (530, 650, 770),
    "pulse_period": 30,
}
# The single-pitch cues (LacunaIrisTine, LacunaPelletFire: every pitched layer is a C) are recorded at ladder step 3
# (C6) and played at the iris's step through DollWeaponAudio.Note: F5 Ab5 Bb5 C6 Eb6 F6 Ab6 as the seven irises join.
# Every other Lacuna cue is a composite and plays as rendered (tools tests check both).
LACUNA_NOTE_ROOT = 3
# The beam's loop: eight visual pulse periods (8 x 30 ticks = 4.0 s), exactly 176,400 frames.
LACUNA_LOOP_SECONDS = 8 * LACUNA["pulse_period"] / 60


def _ticks(t):
    return t / 60


def _sweep(dur, f_from, f_to, rng, bands=12, width=0.35):
    """Noise drawn through a band that glides from f_from to f_to (crossfaded fixed bands; geometric glide)."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    centres = np.geomspace(f_from, f_to, bands)
    for k, c in enumerate(centres):
        lo, hi = c * (1 - width / 2), min(c * (1 + width / 2), RATE * 0.45)
        band = base.bp(base.noise(n, rng), lo, hi)
        centre = (k + 0.5) / bands * dur
        out += band * np.clip(1 - np.abs(t - centre) / (dur / bands * 1.5), 0, 1)[:, None]
    return out / max(1e-9, np.abs(out).max())


def _inhale(dur, rng, low=300, high=5000):
    """A reversed-air intake: band noise rising exponentially into its end."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    x = base.bp(base.noise(n, rng), low, high) * (np.exp((t - dur) / (dur * 0.28)))[:, None]
    return x / max(1e-9, np.abs(x).max())


def _puff(dur, rng, cutoff=520):
    """A soft low void 'puh': low-passed noise with a quick decay."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    x = lp(base.noise(n, rng), cutoff) * (np.exp(-t / (dur * 0.3)) * np.clip(t / 0.004, 0, 1))[:, None]
    return x / max(1e-9, np.abs(x).max())


# A gong-like plate tuned into the key: inharmonic in spirit (every partial slightly off its ratio, each a pair a
# fraction of a hertz apart so it beats and shimmers), but its strong partials sit on F minor pentatonic intervals
# above the root (1 F, 1.5 C, 2 F, 2.38 Ab, 3 C, 3.56 Eb, 4 F) so the long tail never clashes with the music.
GONG_PARTIALS = ((1.0, 1.0, 1.0), (1.498, 0.55, 0.8), (2.004, 0.45, 0.7), (2.381, 0.28, 0.55), (2.993, 0.22, 0.45),
                 (3.566, 0.14, 0.35), (4.011, 0.1, 0.3))


def _gong(freq, dur, rng, tau=1.5, softness=0.012, beat=0.35, glide_cents=-12):
    """A soft mallet on a tuned gong: partials (ratio, gain, decay share) with long decays (the root rings for `tau`
    s, higher partials shorter), a raised-cosine onset of `softness` s (no click), a slow downward pitch settle and a
    low felt thud. Original additive synthesis; returns stereo, peak 1."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    settle = 2 ** (glide_cents * (1 - np.exp(-t / 0.6)) / 1200)
    out = np.zeros((n, 2))
    for k, (ratio, gain, share) in enumerate(GONG_PARTIALS):
        f = freq * ratio
        if f * 1.01 >= dsp.CEILING_HZ:
            break
        env = np.exp(-t / (tau * share))
        for side, offset in ((-1, -beat * (1 + 0.3 * k)), (1, beat * (1 + 0.3 * k))):
            phase = 2 * np.pi * np.cumsum((f + offset) * settle) / RATE + rng.uniform(0, 2 * np.pi)
            y = np.sin(phase) * env * gain
            out += pan(y[:, None].repeat(2, axis=1) * 0.5, 0.25 * side * min(1, k / 3))
    onset = np.clip(t / softness, 0, 1)
    out *= (0.5 - 0.5 * np.cos(np.pi * onset))[:, None]
    k = round(0.09 * RATE)
    felt = lp(base.noise(k, rng), 260) * (np.exp(-np.arange(k) / RATE / 0.025))[:, None]
    out[:k] += felt / max(1e-9, np.abs(felt).max()) * 0.25
    return out / max(1e-9, np.abs(out).max())


@cue("LacunaIrisWarn", -17, "Lacuna", 0.45, 0.7,
     "虹彩が一つ生まれる（第1〜7の虹彩それぞれの誕生の瞬間）。遺言書の穴から磁器の花弁が滑り出す細いこすれ音に、"
     "+8tick（0.13秒）で真鍮の留め金が一段、+11tickでもう半段かかる。開く準備の予告音で、開く瞬間は LacunaIrisFire。")
def lacuna_iris_warn(s, rng):
    mix = seconds(0.5)
    place(mix, pan(_sweep(0.12, 2200, 5200, rng, bands=8) * np.linspace(0.3, 1, round(0.12 * RATE))[:, None], -0.15), 0.0, -14)
    for k, (at, note) in enumerate(((0.012, "Bb7"), (0.05, "C8"), (0.088, "Eb8"))):
        place(mix, dsp.porcelain_ring(dsp.hz(note), 0.05, rng, decay=0.008, side=-0.3 + 0.25 * k), at, -16 - 2 * k)
    place(mix, pan(dsp.brass_click(rng, 2350, decay=0.005, thud=0.45), -0.1), _ticks(LACUNA["frame_parting"]), -4)
    place(mix, dsp.porcelain_ring(dsp.hz("C8"), 0.06, rng, decay=0.01, side=0.1), _ticks(LACUNA["frame_parting"]) + 0.002, -13)
    place(mix, pan(dsp.brass_click(rng, 2600, decay=0.004, thud=0.25), 0.05), _ticks(LACUNA["frame_half"]), -9)
    return room(mix, 0.1, 0.42, 0.08)


@cue("LacunaIrisFire", -20, "Lacuna", 0.4, 0.8,
     "虹彩が開ききって最初の弾を撃つ（誕生から15tick）。磁器の開放音「カッ」（C7 と F7 の短い響き）と真鍮の留め金、"
     "下に虚無の小さな「ぽっ」。どの虹彩でもこの高さのまま鳴らす（複数の音を重ねた音なので移調しない）。"
     "同じ瞬間に、虹彩ごとに一段ずつ上がるオルゴールの一音 LacunaIrisTine が重なる。")
def lacuna_iris_fire(s, rng):
    mix = seconds(0.45)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.12, rng, decay=0.018, side=-0.1), 0.0, -4)
    place(mix, dsp.porcelain_ring(dsp.hz("F7"), 0.1, rng, decay=0.012, side=0.15), 0.003, -8)
    place(mix, pan(dsp.brass_click(rng, 1900, decay=0.006, thud=0.7), 0.0), 0.0, -6)
    place(mix, _puff(0.14, rng, 420), 0.004, -12)
    return room(mix, 0.1, 0.36, 0.08)


@cue("LacunaIrisTine", -20, "Lacuna", 0.75, 0.9,
     "虹彩が開く瞬間のオルゴールの爪一音（C6 で録った単音）。ゲームでは虹彩の順に F5 A♭5 B♭5 C6 E♭6 F6 A♭6 と"
     "一段ずつ上げて鳴らす（一つの音だけなので移調してよい）。開放音 LacunaIrisFire と同時に鳴る。")
def lacuna_iris_tine(s, rng):
    mix = seconds(0.75)
    place(mix, pan(dsp.box_tine(dsp.hz("C6"), 0.7, rng), 0.05), 0.0, -2)
    return room(mix, 0.1, 0.7, 0.08)


@cue("LacunaPelletWarn", -20, "Lacuna", 0.2, 0.55,
     "次の弾の4tick前、花弁が半分閉じる合図。真鍮のシャッターの小さな「トッ」。直後の LacunaPelletFire とで一組。")
def lacuna_pellet_warn(s, rng):
    mix = seconds(0.2)
    place(mix, pan(dsp.brass_click(rng, 2950, decay=0.0032, thud=0.2, tick=0.7), 0.1), 0.0, -2)
    place(mix, dsp.porcelain_ring(dsp.hz("Eb8"), 0.04, rng, decay=0.006, side=-0.1), 0.004, -14)
    return room(mix, 0.06, 0.16, 0.05)


@cue("LacunaPelletFire", -20, "Lacuna", 0.4, 0.7,
     "虹彩が小さな虚無の弾を撃つ（2発目以降の各弾）。明るい磁器の粒の「ピン」（オルゴールの爪、C6 で録り虹彩ごとに"
     "音程が上がる）と、低くやわらかい虚無の「ぷっ」。")
def lacuna_pellet_fire(s, rng):
    mix = seconds(0.42)
    place(mix, pan(dsp.box_tine(dsp.hz("C6"), 0.36, rng, decay=0.11, body=0.2), 0.0), 0.0, -2)
    place(mix, dsp.porcelain_ring(dsp.hz("C8"), 0.06, rng, decay=0.008, side=0.2), 0.001, -16)
    place(mix, _puff(0.09, rng, 360), 0.002, -10)
    place(mix, dsp.thump(140, 70, 0.08, rng), 0.0, -18)
    return room(mix, 0.08, 0.36, 0.08)


@cue("LacunaPelletHit", -20, "Lacuna", 0.25, 0.6,
     "弾が実際に当たったときだけ鳴る（時間切れや持ち主の行動不能で消えた弾は鳴らない）。くぐもった磁器の「トッ」と暗い空気の吐息。")
def lacuna_pellet_hit(s, rng):
    mix = seconds(0.26)
    place(mix, dsp.porcelain_ring(dsp.hz("Bb5"), 0.12, rng, decay=0.02, side=0.0), 0.0, -3)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.06, rng, decay=0.008, side=0.2), 0.001, -12)
    place(mix, _puff(0.07, rng, 650), 0.0, -8)
    place(mix, dsp.porcelain_crack(0.06, rng, count=3, spread=0.012, low=2600, high=4800), 0.004, -20)
    return room(mix, 0.06, 0.22, 0.06)


@cue("LacunaMergeWarn", -13, "Lacuna", 0.42, 0.8,
     "七つの虹彩が座を離れて手の前へ集まる（340tick）。真鍮の歯車が毎秒8から40打へ回り上がり、続いて350〜362tick に"
     "虹彩が一つずつ嵌まる磁器の刻みが7つ、音程を寄せながら並ぶ。最後の刻みが LacunaMergeFire（大きな穴の完成）へつながる。")
def lacuna_merge_warn(s, rng):
    mix = seconds(0.45)
    spin = _ticks(LACUNA["docked"][0] - LACUNA["merge"])
    times, at, rate = [], 0.0, 8.0
    while at < spin - 0.004:
        times.append(at)
        rate = 8 + 32 * (at / spin) ** 1.2
        at += 1 / rate
    place(mix, dsp.ratchet(times, rng, freq=2250, gains_db=[-10 + 8 * (k / max(1, len(times) - 1)) for k in range(len(times))], side=-0.2), 0.0, -6)
    place(mix, pan(_sweep(spin + 0.05, 600, 3800, rng, bands=10), 0.1) * 0.8, 0.0, -16)
    ladder = ("F7", "Eb7", "C7", "Bb6", "Ab6", "F6", "F6")
    for k, dock in enumerate(LACUNA["docked"]):
        when = _ticks(dock - LACUNA["merge"])
        place(mix, dsp.porcelain_ring(dsp.hz(ladder[k]), 0.08, rng, decay=0.012, side=0.6 * np.cos(np.pi * k / 6)), when, -10 + k * 0.6)
        place(mix, pan(dsp.brass_click(rng, 2700 - 90 * k, decay=0.0035, thud=0.3), 0.0), when, -12)
    return room(mix, 0.12, 0.42, 0.03)


@cue("LacunaMergeFire", -11.5, "Lacuna", 0.8, 0.85,
     "七つが一つの大きな穴（大虹彩）になる（362tick）。重い真鍮の輪がはまる音、F6 と C7 の磁器の響き、低い一撃。")
def lacuna_merge_fire(s, rng):
    mix = seconds(0.85)
    lay(mix, s, "metal_latch", 0.041, 0.24, 0.0, -6, rate=0.82, hp_=380, side=0.0, fade_in=0.0005, fade_out=0.1)
    place(mix, pan(dsp.brass_click(rng, 1150, decay=0.03, thud=1.0, dur=0.2), 0.0), 0.0, -4)
    place(mix, dsp.porcelain_ring(dsp.hz("F6"), 0.6, rng, decay=0.16, side=-0.25), 0.004, -8)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.45, rng, decay=0.11, side=0.3), 0.009, -12)
    place(mix, dsp.thump(130, 48, 0.4, rng), 0.0, -5)
    return room(mix, 0.16, 0.78, 0.18)


@cue("LacunaBeamWarn", -13, "Lacuna", 0.75, 0.85,
     "大きな穴の溜め（362〜404tick）。低いパイプオルガン F2・C3 が膨らみ、ノイズの吸い込みが 4 kHz から 300 Hz へ落ちていく。"
     "輪が一段ずつ回る真鍮のラチェット7打（368・378・386・392・397・401・404tick）を焼き込み、404tick で切れて"
     "発射までの6tickは無音（静かな溜め）。")
def lacuna_beam_warn(s, rng):
    length = _ticks(LACUNA["clicks"][-1] - LACUNA["formed"])
    mix = seconds(0.76)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3", "F3")], length + 0.02, rng, attack=length * 0.85, release=0.05,
                        harmonics=12, rolloff=1.15, chiff=0.0, breath=0.3)
    place(mix, pad, 0.0, -4)
    suck = _sweep(length, 4000, 300, rng, bands=14) * np.linspace(0.25, 1, round(length * RATE))[:, None]
    place(mix, pan(suck, 0.0), 0.0, -12)
    for k, click in enumerate(LACUNA["clicks"]):
        when = _ticks(click - LACUNA["formed"]) - 0.003
        place(mix, pan(dsp.brass_click(rng, 2050 * 2 ** (k / 12), decay=0.004, thud=0.5), -0.15 + 0.05 * k), when, -9 + k * 0.7)
    out = mix[:round((length + 0.012) * RATE)]
    k = round(0.012 * RATE)
    out[-k:] *= np.linspace(1, 0, k)[:, None]
    return out


@cue("LacunaBeamFire", -11.5, "Lacuna", 2.8, 0.9,
     "黒い芯の光線が開く（410tick、溜めの静寂のあと）。60ミリ秒の吸い込みから、F に合わせた銅鑼のような低い一打"
     "（やわらかい撥、長く減衰する余韻）と、その下で静かにふくらむオルガン Fm7（F2 C3 E♭3 A♭3 C4）、ゆっくり沈む低音、"
     "かすかな磁器のきらめき。オルガンを強く鳴らすのではなく、控えめなオルガンと響く余韻で締める（T3、約2.6秒）。")
def lacuna_beam_fire(s, rng):
    mix = seconds(2.9)
    inhale = 0.06
    place(mix, pan(_inhale(inhale, rng), 0.0), 0.0, -14)
    # The strike: a tuned gong-like plate on F2 an octave under F3, soft mallet, long tail.
    place(mix, _gong(dsp.hz("F2"), 2.6, rng, tau=1.6), inhale, -3)
    place(mix, _gong(dsp.hz("F3"), 1.9, rng, tau=0.9, beat=0.5), inhale + 0.004, -13)
    # The organ only breathes under it: a slow swell, never a stab.
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3", "Eb3", "Ab3", "C4")], 2.3, rng, attack=0.18, release=1.6,
                        harmonics=10, rolloff=1.4, chiff=0.0, breath=0.12)
    place(mix, pad, inhale + 0.02, -13)
    place(mix, dsp.thump(60, 38, 1.1, rng), inhale, -9)
    place(mix, dsp.porcelain_crack(0.12, rng, count=5, spread=0.035, low=2400, high=6400), inhale, -21)
    place(mix, dsp.shimmer(0.9, rng, count=8), inhale + 0.05, -20)
    return room(mix, 0.24, 2.6, 0.6)


assert LACUNA_LOOP_SECONDS == 4.0  # the registry line below spells the length out for the export tests


@cue("LacunaBeamLoop", -14, "Lacuna", 4.0, 0.7,
     "光線が出ている間ずっと鳴るループ（4.0秒、ちょうど脈の8周期、サンプル単位で継ぎ目なし）。暗いオルガンの持続音 F2・C3"
     "（0.5 Hz のうなり）、200〜600 Hz の吸い込みの帯、脈と同じ毎秒2回のゆるいトレモロ、ときどき遠くで鳴る磁器の小さな響き。",
     loop=True)
def lacuna_beam_loop(s, rng):
    """Periodic by construction: every partial and modulation completes whole cycles in the loop (0.25 Hz grid),
    the noise band is FFT-synthesised on the loop's own bins, and the tings are placed circularly."""
    n = round(LACUNA_LOOP_SECONDS * RATE)
    t = np.arange(n) / RATE
    grid = 1 / LACUNA_LOOP_SECONDS

    def snap(f):
        return round(f / grid) * grid

    out = np.zeros((n, 2))
    voices = ((snap(dsp.hz("F2")), 1.0, -0.35), (snap(dsp.hz("F2")) + 2 * grid, 0.6, 0.35),
              (snap(dsp.hz("C3")), 0.55, 0.15), (snap(dsp.hz("F3")), 0.3, -0.2))
    for f0, gain, side in voices:
        y = np.zeros(n)
        for h in range(1, 10):
            f = f0 * h
            if f >= dsp.CEILING_HZ:
                break
            y += np.sin(2 * np.pi * f * t + rng.uniform(0, 2 * np.pi)) / h ** 1.25
        out += pan(y / np.abs(y).max() * gain, side)
    tremolo = 1 + 0.15 * np.sin(2 * np.pi * 2.0 * t)
    out *= tremolo[:, None]
    # the suction band: random-phase bins 200-600 Hz, swelling twice per loop
    spectrum = np.zeros((n // 2 + 1, 2), dtype=complex)
    freqs = np.fft.rfftfreq(n, 1 / RATE)
    band = (freqs >= 200) & (freqs <= 600)
    spectrum[band] = np.exp(1j * rng.uniform(0, 2 * np.pi, (band.sum(), 2)))
    hiss = np.fft.irfft(spectrum, n=n, axis=0)
    hiss *= (0.6 + 0.4 * np.sin(2 * np.pi * 0.5 * t))[:, None]
    out += hiss / np.abs(hiss).max() * 0.35
    # sparse porcelain tings, placed circularly so the seam stays exact
    for k, note in enumerate(("F7", "C7", "Ab6", "Eb7", "Bb6", "F7")):
        ring = dsp.porcelain_ring(dsp.hz(note), 0.35, rng, decay=0.07, side=rng.uniform(-0.7, 0.7))
        start = round((0.31 + k * 0.67) * RATE) % n
        idx = (start + np.arange(len(ring))) % n
        out[idx] += ring * 10 ** (-24 / 20) * 3
    return out


def _widen(rng, note_names, length, extra=-14):
    mix = seconds(length + 0.1)
    place(mix, pan(dsp.brass_click(rng, 1700, decay=0.012, thud=0.8), 0.0), 0.0, -4)
    place(mix, dsp.ratchet((0.0, 0.035), rng, freq=2300, gains_db=(-6, -10), side=0.2), 0.0, -8)
    pad = dsp.organ_pad([dsp.hz(n) for n in note_names], length * 0.95, rng, attack=0.09, release=length * 0.6,
                        harmonics=10, rolloff=1.3, chiff=0.05, breath=0.12)
    place(mix, pad, 0.02, extra)
    return mix


@cue("LacunaWiden1", -17, "Lacuna", 0.62, 0.75,
     "光線が太くなる一段目（発射から2秒）。輪がもう一段回る真鍮の音に、オルガンの声が一つ（A♭3）加わる。")
def lacuna_widen_1(s, rng):
    return room(_widen(rng, ("Ab3",), 0.5), 0.14, 0.58, 0.14)


@cue("LacunaWiden2", -17, "Lacuna", 0.62, 0.75,
     "太くなる二段目（発射から4秒）。同じ真鍮の音に、オルガンの声 C4 が加わる。")
def lacuna_widen_2(s, rng):
    return room(_widen(rng, ("C4",), 0.5), 0.14, 0.58, 0.14)


@cue("LacunaWiden3", -13, "Lacuna", 1.0, 0.75,
     "最大の太さに達する（発射から6秒）。輪が落ち着く真鍮の響きと、F マイナーの和音（F3 A♭3 C4 E♭4）がふくらむ。")
def lacuna_widen_3(s, rng):
    mix = _widen(rng, ("F3", "Ab3", "C4", "Eb4"), 0.85, extra=-8)
    place(mix, dsp.porcelain_ring(dsp.hz("F6"), 0.5, rng, decay=0.12, side=0.25), 0.004, -14)
    return room(mix, 0.16, 0.95, 0.25)


@cue("LacunaBeamHit", -20, "Lacuna", 0.28, 0.6,
     "光線が敵に当たっている間、20tickに1回まで。低い磁器の共鳴と、虚無のやわらかなパチパチ。")
def lacuna_beam_hit(s, rng):
    mix = seconds(0.3)
    place(mix, dsp.porcelain_ring(dsp.hz("Ab4"), 0.2, rng, decay=0.05, side=0.0), 0.0, -3)
    place(mix, dsp.porcelain_ring(dsp.hz("Eb6"), 0.1, rng, decay=0.02, side=0.15), 0.002, -11)
    place(mix, lp(dsp.porcelain_crack(0.1, rng, count=4, spread=0.04, low=1800, high=4200), 3500), 0.006, -12)
    place(mix, _puff(0.08, rng, 300), 0.0, -12)
    return room(mix, 0.08, 0.26, 0.07)


@cue("LacunaBeamEnd", -13, "Lacuna", 0.75, 0.8,
     "手を放す（光線中・溜め中）。光線が穴へ吸い戻され、七つの虹彩がパタパタと閉じる磁器の音（C7 から F5 へ下がる7打）と"
     "真鍮の輪、最後に逆回しの吸気。光線の前に放したとき（開いた虹彩が閉じるだけ）は、同じ音を小さく鳴らす。")
def lacuna_beam_end(s, rng):
    mix = seconds(0.8)
    place(mix, pan(_sweep(0.3, 2200, 220, rng, bands=10) * np.linspace(1, 0.2, round(0.3 * RATE))[:, None], 0.0), 0.0, -10)
    ladder = ("C7", "Bb6", "Ab6", "F6", "Eb6", "C6", "F5")
    for k, note in enumerate(ladder):
        when = 0.035 + k * 0.022 * (1 - 0.04 * k)
        place(mix, dsp.porcelain_ring(dsp.hz(note), 0.09, rng, decay=0.014, side=0.55 * np.cos(np.pi * k / 6)), when, -7 - 0.4 * k)
        place(mix, pan(dsp.brass_click(rng, 2500 - 120 * k, decay=0.003, thud=0.2), 0.0), when, -14)
    place(mix, pan(dsp.brass_click(rng, 1250, decay=0.02, thud=0.7, dur=0.15), 0.0), 0.2, -8)
    place(mix, pan(_inhale(0.22, rng, 400, 3200), 0.0), 0.22, -15)
    return room(mix, 0.14, 0.7, 0.18)


@cue("LacunaBeamMiss", -13, "Lacuna", 0.9, 0.8,
     "マナが尽きて儀式が崩れる（失敗）。オルガンが F・E♭・C と下がりながら途切れ途切れに3回つまずき、磁器がひび割れ、音程が F3 から C3 へ"
     "沈みながら消える。放したときの LacunaBeamEnd より低く暗い。")
def lacuna_beam_miss(s, rng):
    mix = seconds(0.95)
    for k, (at, dur, low, high) in enumerate(((0.0, 0.07, "F3", "C4"), (0.1, 0.06, "Eb3", "Bb3"), (0.19, 0.09, "C3", "F3"))):
        blip = dsp.organ_pad([dsp.hz(low), dsp.hz(high)], dur, rng, attack=0.006,
                             release=0.02, harmonics=10, rolloff=1.2, chiff=0.0, breath=0.4)
        place(mix, blip, at, -6 - 2 * k)
        place(mix, pan(_puff(0.05, rng, 700), 0.2 * (k - 1)), at, -14)
    place(mix, dsp.porcelain_crack(0.2, rng, count=9, spread=0.08, low=1500, high=5200), 0.27, -8)
    n = round(0.6 * RATE)
    tt = np.arange(n) / RATE
    glide = dsp.hz("F3") * 2 ** (-7 * np.clip(tt / 0.5, 0, 1) / 12)
    phase = 2 * np.pi * np.cumsum(glide) / RATE
    y = sum(np.sin(h * phase) / h ** 1.3 for h in range(1, 8)) * np.exp(-tt / 0.22) * np.clip(tt / 0.01, 0, 1)
    place(mix, pan(lp(y[:, None].repeat(2, axis=1), 1800) / np.abs(y).max(), 0.0), 0.3, -6)
    place(mix, dsp.thump(90, 40, 0.4, rng), 0.3, -10)
    return room(mix, 0.12, 0.9, 0.3)


# ---------------------------------------------------------------- Pale Meridian
# The music-box siege rifle (docs/encounters/first-severance/WEAPONS.md "Pale Meridian"). Every timed layer sits on
# the weapon's score ticks (Content/Encounters/FirstSeverance/Rewards/PaleMeridianScore.cs and PaleMeridianLattice.cs),
# so a cue started on its tick lands on the visual beat: the wind's ratchet clicks, the overcharge loop (whole heavy
# bars) and the lattice's ring ripple. Notes are one file per ladder step: nothing is transposed at runtime.
from generate_ebon_sfx import bp, mono, noise  # noqa: E402

MERIDIAN_KEY_RISE, MERIDIAN_IGNITE = 300, 348
MERIDIAN_WIND = (318, 324, 328, 331, 334, 337, 339, 341, 343, 345, 347, 348)  # ratchet steps; the last is the release
MERIDIAN_LOOP_TICKS = 144   # 2.4 s: four 36-tick heavy bars, nine 16-tick key turns, 48 rounds
MERIDIAN_MERIDIAN_FIRE = 10  # meridian age of the strike
MERIDIAN_RIPPLE = 2          # lattice rings fire 2 ticks apart
MERIDIAN_NOTE_TEXT = ("F5", "A♭5", "B♭5", "C6", "E♭6", "F6", "A♭6", "B♭6", "C7")


def meridian_note(step, rng):
    """One shot of the gun: the next music-box tooth of the tune, a brass 'tk' and an air puff from the muzzle."""
    mix = seconds(0.95)
    place(mix, pan(dsp.box_tine(dsp.ladder(step), 0.9, rng), -0.12 + 0.03 * step), 0.0, 0)
    k = round(0.012 * RATE)
    puff = bp(noise(k, rng), 1800, 6500) * (np.linspace(1, 0, k) ** 2)[:, None]
    place(mix, pan(puff, 0.25), 0.0, -15)
    place(mix, pan(dsp.brass_click(rng, 3400, decay=0.003, thud=0.15), 0.2), 0.0, -18)
    return room(mix, 0.12, 0.9, 0.18)


def brass_ring(freq, dur, rng, decay=0.12, side=0.0):
    """A small brass part ringing: inharmonic pawl partials with a longer decay than a click."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    y = np.zeros(n)
    for ratio, amp in ((1.0, 1.0), (2.76, 0.5), (5.40, 0.22), (8.93, 0.1)):
        if freq * ratio < dsp.CEILING_HZ:
            y += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6.28)) * np.exp(-t / (decay / ratio ** 0.4))
    y *= np.clip(t / 0.0003, 0, 1)
    return pan(y / max(1e-9, np.abs(y).max()), side)


def spring_twang(freq, dur, rng, side=0.0):
    """A coil spring let go: a fast-decaying wobbling tone that slides down a little."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    f = freq * (1 - 0.18 * (1 - np.exp(-t / 0.05))) * (1 + 0.03 * np.sin(2 * np.pi * 38 * t))
    y = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.07) + 0.3 * np.sin(4 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.03)
    return pan(y * np.clip(t / 0.0008, 0, 1), side)


def soft_gong(freq, dur, rng, decay=1.2, bloom=0.08, side=0.0):
    """A soft-mallet gong resonance for the closing strikes (the owner's pick for big finishing sounds: a quiet organ
    under a soft gong-like ring with a long tail, never an organ played out loud). Not a tuned bell: a strong low
    fundamental with a beating twin mode, an inharmonic upper cluster (no octave or fifth partials) that blooms in over
    the first ~0.1 s and dies faster, a slight downward sag in pitch, a felt-mallet attack with no click, and the
    whole ring low-passed so it hums rather than chimes."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    sag = 1 - 0.004 * (1 - np.exp(-t / 0.4))
    y = np.zeros(n)
    for ratio, amp, life in ((1.0, 1.0, 1.0), (1.007, 0.6, 0.9), (1.43, 0.42, 0.6), (1.89, 0.32, 0.5), (2.36, 0.24, 0.42),
                             (2.95, 0.17, 0.35), (3.61, 0.11, 0.3), (4.43, 0.06, 0.25)):
        f = freq * ratio * (1 + rng.uniform(-0.002, 0.002))
        if f >= dsp.CEILING_HZ:
            continue
        env = np.exp(-t / (decay * life))
        if ratio > 1.2:
            env = env * (1 - np.exp(-t / (bloom * ratio ** 0.5)))
        y += amp * np.sin(2 * np.pi * np.cumsum(f * sag) / RATE + rng.uniform(0, 6.28)) * env
    y *= np.clip(t / 0.014, 0, 1) ** 2
    k = round(0.07 * RATE)
    felt = lp(noise(k, rng), 380).mean(axis=1) * np.exp(-np.arange(k) / RATE / 0.018)
    y[:k] += felt / max(1e-9, np.abs(felt).max()) * 0.25
    out = lp(pan(y / max(1e-9, np.abs(y).max()), side), 2200)
    return out


def whoosh(dur, rng, low=700, high=4200, rise=True):
    """Band-swept air: a filter sweeping up (or down) across filtered noise, swelling and fading."""
    n = round(dur * RATE)
    x = noise(n, rng)
    out = np.zeros((n, 2))
    steps = 12
    for i in range(steps):
        a, b = i * n // steps, (i + 1) * n // steps
        u = i / (steps - 1)
        centre = low * (high / low) ** (u if rise else 1 - u)
        out[a:b] = bp(x, centre * 0.7, min(centre * 1.4, dsp.CEILING_HZ))[a:b]
    t = np.arange(n) / RATE
    return out * (np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 1.5)[:, None]


@cue("MeridianAssemble", -17.0, "Meridian build", 0.5, 0.55,
     "押した瞬間、何もない銃身（部品の窪みが空いた素の銃）が手に収まる。磁器の板が3枚カチカチと座り、鉄の留め金が"
     "ゴトンと閉まり、オルゴールの櫛を指でかすめるような小さな下降音で終わる。")
def meridian_assemble(s, rng):
    mix = seconds(0.55)
    for i, at in enumerate((0.0, 0.055, 0.095)):
        place(mix, dsp.porcelain_ring(rng.uniform(2300, 3100), 0.12, rng, decay=0.02, side=-0.3 + 0.3 * i), at, -7 - 2 * i)
    # Kenney metalLatch's catch starts at 0.0418 s: slowed and darkened into an iron latch.
    lay(mix, s, "metal_latch", 0.041, 0.20, 0.13, -8, rate=0.8, lp_=3600, fade_out=0.06)
    place(mix, dsp.thump(150, 72, 0.16, rng), 0.13, -12)
    for i, step in enumerate((8, 6, 4, 3, 1)):
        place(mix, pan(dsp.box_tine(dsp.ladder(step), 0.35, rng, decay=0.07, body=0), 0.4 - 0.2 * i), 0.18 + 0.018 * i, -19 - i)
    return room(mix, 0.1, 0.5, 0.1)


@cue("MeridianNote0", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（F5）。旋律の一音として順に鳴る。真鍮の「カッ」と銃口の小さな空気音つき。")
def meridian_note_0(s, rng):
    return meridian_note(0, rng)


@cue("MeridianNote1", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（A♭5）。")
def meridian_note_1(s, rng):
    return meridian_note(1, rng)


@cue("MeridianNote2", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（B♭5）。")
def meridian_note_2(s, rng):
    return meridian_note(2, rng)


@cue("MeridianNote3", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（C6）。")
def meridian_note_3(s, rng):
    return meridian_note(3, rng)


@cue("MeridianNote4", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（E♭6）。")
def meridian_note_4(s, rng):
    return meridian_note(4, rng)


@cue("MeridianNote5", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（F6）。")
def meridian_note_5(s, rng):
    return meridian_note(5, rng)


@cue("MeridianNote6", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（A♭6）。")
def meridian_note_6(s, rng):
    return meridian_note(6, rng)


@cue("MeridianNote7", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（B♭6）。")
def meridian_note_7(s, rng):
    return meridian_note(7, rng)


@cue("MeridianNote8", -17.0, "Meridian notes", 1.0, 0.5, "1発ごとのオルゴールの音（C7、組み上げの最後の駆け上がりの頂点）。")
def meridian_note_8(s, rng):
    return meridian_note(8, rng)


@cue("MeridianPartWarn", -17.0, "Meridian build", 0.4, 0.45,
     "真鍮の部品がばねで弾き出されて飛んでくる（16tick）。ばねの「ビン」、加速するラチェットの回転音、上がっていく風切り音。"
     "着座（MeridianPartFire）の予告。")
def meridian_part_warn(s, rng):
    mix = seconds(0.4)
    place(mix, spring_twang(820, 0.2, rng, side=-0.35), 0.0, -9)
    times = [0.02 + 0.22 * (i / 9) ** 0.8 for i in range(10)]
    place(mix, dsp.ratchet(times, rng, freq=2900, gains_db=[-14 + 1.0 * i for i in range(10)], side=-0.2,
                           decay=0.0025, thud=0.1), 0.0, -8)
    place(mix, whoosh(0.26, rng, 900, 5200), 0.0, -16)
    return room(mix, 0.1, 0.36, 0.08)


@cue("MeridianPartFire", -13.0, "Meridian build", 0.4, 0.5,
     "部品が銃に嵌まる。真鍮の留め金の「カチャン」、磁器の小さな「チン」、短いばねの残響（音程なし）。小節の頭に来るので、"
     "その小節の最初の音符と重なる。", glue=3.0)
def meridian_part_fire(s, rng):
    mix = seconds(0.4)
    # The latch and pawl are offset by 3 ms so their transients do not stack into one spike.
    lay(mix, s, "metal_latch", 0.041, 0.18, 0.0, -9, rate=1.08, hp_=600, fade_out=0.05)
    place(mix, pan(dsp.brass_click(rng, 2100, decay=0.009, thud=0.7), -0.1), 0.003, -11)
    place(mix, brass_ring(2600, 0.25, rng, decay=0.07, side=-0.2), 0.003, -12)
    place(mix, dsp.porcelain_ring(4200, 0.15, rng, decay=0.04, side=0.3), 0.006, -11)
    place(mix, spring_twang(1500, 0.2, rng, side=0.1), 0.008, -15)
    place(mix, dsp.thump(220, 95, 0.12, rng), 0.0, -12)
    return room(mix, 0.14, 0.36, 0.08)


@cue("MeridianIgniteWarn", -13.0, "Meridian wind", 0.9, 0.6,
     "巻き鍵が筐体からせり上がって座り、18tick息を止めたあと、ラチェットが11回、詰まりながら巻かれる（318〜347tick に"
     "ぴったり合わせてある）。下でオルガンの F の空気が膨らみ、オルゴールの櫛が細かく震え始める。過充填（MeridianIgniteFire）の予告。")
def meridian_ignite_warn(s, rng):
    length = (MERIDIAN_IGNITE - MERIDIAN_KEY_RISE) / 60
    mix = seconds(length + 0.06)
    # The key slides up out of the housing (KeyRiseTicks = 6) and seats with a clink.
    place(mix, whoosh(0.1, rng, 1600, 6000) * 0.8, 0.0, -14)
    lay(mix, s, "metal_click", 0.2705, 0.34, 0.1, -9, rate=0.9, hp_=1200, side=-0.15, fade_in=0.0005, fade_out=0.03)
    place(mix, dsp.porcelain_ring(3800, 0.12, rng, decay=0.025, side=-0.1), 0.1, -15)
    # Held organ air for 18 ticks, then the F pedal swelling under the wind.
    breath = dsp.organ_pad([dsp.hz("F2")], 0.3, rng, attack=0.12, release=0.05, harmonics=4, chiff=0.0, breath=0.9)
    place(mix, breath, 0.0, -22)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3", "F3", "Ab3", "C4")], length - 0.25, rng, attack=length - 0.3,
                        release=0.04, harmonics=9, rolloff=1.4, chiff=0.02, breath=0.25)
    place(mix, pad, 0.27, -12)
    # Eleven ratchet clicks on the wind ticks (the twelfth is the release in MeridianIgniteFire).
    clicks = [(tick - MERIDIAN_KEY_RISE) / 60 for tick in MERIDIAN_WIND[:-1]]
    place(mix, dsp.ratchet(clicks, rng, freq=2300, gains_db=[-6 + 0.55 * i for i in range(len(clicks))], side=-0.2,
                           decay=0.005, thud=0.55), 0.0, -3)
    for at in clicks:
        lay(mix, s, "metal_click", 0.2705, 0.31, at, -14, rate=1.0 + 0.012 * clicks.index(at), hp_=1500, side=-0.25,
            fade_in=0.0005, fade_out=0.02)
    # A comb tremolo that tightens toward the release: Eb7 / F7 alternating, faster and louder.
    t, i = 0.32, 0
    while t < length - 0.01:
        note = "Eb7" if i % 2 == 0 else "F7"
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.12, rng, decay=0.04, body=0, modes=dsp.COMB_MODES[:2]), 0.3 if i % 2 else 0.45),
              t, -26 + 14 * (t - 0.32) / (length - 0.32))
        t += 0.06 - 0.042 * (t - 0.32) / (length - 0.32)
        i += 1
    out = room(mix, 0.12, length + 0.06, 0.05)
    return out


@cue("MeridianIgniteFire", -11.5, "Meridian wind", 1.05, 0.75,
     "ばねが解き放たれて過充填に入る（348tick）。真鍮のばねが弾ける音、F5 から C7 まで 0.15 秒で駆け上がるオルゴール、"
     "短い F マイナーのオルガンの和音、空気の破裂と低い胴鳴り。")
def meridian_ignite_fire(s, rng):
    mix = seconds(1.1)
    lay(mix, s, "metal_click", 0.2705, 0.34, 0.0, -4, rate=0.78, hp_=500, fade_in=0.0005, fade_out=0.04)
    place(mix, pan(dsp.brass_click(rng, 1500, decay=0.01, thud=1.0), 0.0), 0.0, -4)
    place(mix, spring_twang(620, 0.3, rng, side=-0.2), 0.0, -9)
    for i, step in enumerate(range(9)):
        place(mix, pan(dsp.box_tine(dsp.ladder(step), 0.6, rng), -0.5 + 0.12 * i), 0.012 + 0.0175 * i, -9 + 0.4 * i)
    chord = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4", "Ab4", "C5")], 0.75, rng, attack=0.025, release=0.5,
                          harmonics=10, rolloff=1.25, chiff=0.06, breath=0.12)
    place(mix, chord, 0.02, -8)
    k = round(0.3 * RATE)
    burst = hp(noise(k, rng), 900) * np.exp(-np.arange(k) / RATE / 0.07)[:, None]
    place(mix, burst, 0.0, -13)
    place(mix, dsp.thump(130, 52, 0.4, rng), 0.0, -6)
    place(mix, dsp.shimmer(0.5, rng, count=8), 0.18, -19)
    return room(mix, 0.16, 1.05, 0.25)


@cue("MeridianLoop", -17.0, "Meridian wind", 2.4, 0.5,
     "過充填中ずっと鳴る継ぎ目なしのループ（2.4 秒 = 144tick、重弾4小節ぶん）。時計仕掛けの歯車列が弾の間隔（3tick）で刻み、"
     "F のペダルと C のオルガン、ふいごの息、ごく小さな高い櫛のきらめき。放すと6tickで消える。",
     glue=0.8, loop_master="period", loop=True)
def meridian_loop(s, rng):
    n = round(MERIDIAN_LOOP_TICKS / 60 * RATE)
    cross = round(0.12 * RATE)
    length = (n + cross) / RATE
    mix = np.zeros((n + cross, 2))
    # Gear train: a pawl click on every round (3 ticks), the 4-tooth pattern of a real wheel, and a softer
    # counter-gear between them.
    step = 3 / 60
    clicks = [i * step for i in range(int(length / step) + 1)]
    gains = [-4 if i % 12 == 0 else -9 - 2 * (i % 4 == 2) for i in range(len(clicks))]
    place(mix, dsp.ratchet(clicks, rng, freq=3100, gains_db=gains, side=-0.15, decay=0.0022, thud=0.2, tick=0.3), 0.0, -8)
    place(mix, dsp.ratchet([c + step / 2 for c in clicks], rng, freq=4300, gains_db=[-14] * len(clicks), side=0.25,
                           decay=0.0015, thud=0.0, tick=0.2), 0.0, -14)
    # Organ pedal F2 + C3 and the bellows' breath, one slow swell per loop.
    t = np.arange(n + cross) / RATE
    pedal = np.zeros(n + cross)
    for f0, amp in ((dsp.hz("F2"), 1.0), (dsp.hz("C3"), 0.55), (dsp.hz("F3"), 0.3)):
        for h in range(1, 7):
            pedal += amp * np.sin(2 * np.pi * f0 * h * t + rng.uniform(0, 6.28)) / h ** 1.4
    swell = 0.8 + 0.2 * np.sin(2 * np.pi * t / (n / RATE))
    mix += pan(pedal / np.abs(pedal).max() * swell, 0.0) * 10 ** (-14 / 20)
    breath = lp(hp(noise(n + cross, rng), 350), 2200) * (0.6 + 0.4 * np.sin(2 * np.pi * t / (n / RATE) + 1.3))[:, None]
    mix += breath * 10 ** (-30 / 20)
    # A faint high comb shimmer.
    at = 0.03
    while at < length - 0.2:
        note = ("F7", "Ab7", "C8", "Eb7")[int(rng.integers(0, 4))]
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.2, rng, decay=0.05, body=0, modes=dsp.COMB_MODES[:2]), rng.uniform(-0.7, 0.7)),
              at, -30)
        at += rng.uniform(0.11, 0.2)
    # Seamless: the overhang crossfades into the head (equal power), so sample n-1 runs into sample 0.
    w = np.sin(np.linspace(0, np.pi / 2, cross)) ** 2
    out = mix[:n].copy()
    out[:cross] = mix[:cross] * w[:, None] + mix[n:n + cross] * (1 - w)[:, None]
    return out


@cue("MeridianHeavy", -13.0, "Meridian wind", 0.6, 0.5,
     "36tick ごとの重弾（小節の頭）。低い真鍮の「ゴン」（F3 の櫛の歯と真鍮の胴）とオルガンのパイプの短い一吹き。")
def meridian_heavy(s, rng):
    mix = seconds(0.6)
    place(mix, pan(dsp.box_tine(dsp.hz("F3"), 0.55, rng, decay=0.35), -0.1), 0.0, -3)
    # Kenney metalPot1: its first strike starts at 0.090 s (a second one follows at 0.7 s and is left out).
    lay(mix, s, "metal_pot", 0.088, 0.45, 0.0, -9, rate=0.7, lp_=2600, fade_out=0.12)
    place(mix, brass_ring(dsp.hz("C5"), 0.4, rng, decay=0.16, side=0.15), 0.002, -14)
    pop = dsp.organ_pad([dsp.hz("F2"), dsp.hz("C3")], 0.18, rng, attack=0.01, release=0.1, harmonics=8, chiff=0.12, breath=0.1)
    place(mix, pop, 0.0, -10)
    place(mix, dsp.thump(110, 55, 0.25, rng), 0.0, -9)
    return room(mix, 0.12, 0.55, 0.15)


@cue("MeridianStrikeWarn", -13.0, "Meridian release", 0.45, 0.6,
     "放した瞬間（子午線の予告線が出る10tick）。ばねが8回の速い下降クリックでほどけ、オルガンが息を吸い込み、"
     "予告の間だけ高いオルゴールのトレモロが張りつめる。MeridianStrikeFire の予告。")
def meridian_strike_warn(s, rng):
    mix = seconds(0.45)
    times = [0.13 * (i / 7) ** 0.75 for i in range(8)]
    place(mix, dsp.ratchet(times, rng, freq=3000, pattern=[1.0 - 0.06 * i for i in range(8)],
                           gains_db=[-4 - 0.8 * i for i in range(8)], side=0.2, decay=0.003, thud=0.25), 0.0, -4)
    place(mix, whoosh(0.17, rng, 2600, 700, rise=False), 0.0, -14)
    fire = MERIDIAN_MERIDIAN_FIRE / 60
    t, i = 0.0, 0
    while t < fire:
        place(mix, pan(dsp.box_tine(dsp.hz("C7" if i % 2 else "F7"), 0.1, rng, decay=0.03, body=0, modes=dsp.COMB_MODES[:2]),
                       -0.2 if i % 2 else 0.2), t, -15 + 6 * t / fire)
        t += 0.022
        i += 1
    return room(mix, 0.1, 0.42, 0.12)


@cue("MeridianStrikeFire", -11.5, "Meridian release", 2.0, 0.85,
     "子午線が撃ち出される（放してから10tick）。櫛の歯を全部いっせいに弾いた F マイナーの和音（F5 A♭5 C6 E♭6 F6 A♭6 C7）と"
     "短い真鍮のハンマーで打ち出し、その下で柔らかな銅鑼のような低い響き（C3）が長く尾を引き、静かなオルガン（Fm7）が"
     "息を吸うように重なって消える。大きく鳴らさず余韻で締める。段階が高いほど大きく鳴らす（.85 / .72 / .60）。")
def meridian_strike_fire(s, rng):
    mix = seconds(2.1)
    for i, note in enumerate(("F5", "Ab5", "C6", "Eb6", "F6", "Ab6", "C7")):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 1.3, rng), -0.6 + 0.2 * i), 0.002 * i, -7 + 0.3 * i)
    # The brass hammer gives the strike its edge; it is short so the resonance carries the rest.
    place(mix, pan(dsp.brass_click(rng, 1200, decay=0.012, thud=0.8), 0.0), 0.0, -7)
    lay(mix, s, "metal_pot", 0.088, 0.42, 0.0, -13, rate=1.05, hp_=900, lp_=5000, fade_out=0.1)
    k = round(0.12 * RATE)
    air = bp(noise(k, rng), 1800, 7000) * np.exp(-np.arange(k) / RATE / 0.03)[:, None]
    place(mix, air, 0.0, -15)
    # The resonant low strike and its long tail, then a quiet Fm7 organ breathing in under it and dying away.
    place(mix, soft_gong(dsp.hz("C3"), 1.95, rng, decay=0.95, side=0.05), 0.004, -3)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "Eb4", "Ab4")], 1.6, rng, attack=0.12, release=1.1,
                        harmonics=8, rolloff=1.6, chiff=0.0, breath=0.12)
    place(mix, pad, 0.02, -18)
    place(mix, dsp.thump(110, 48, 0.5, rng), 0.0, -13)
    place(mix, dsp.shimmer(0.8, rng, count=10), 0.08, -22)
    return room(mix, 0.22, 2.0, 0.6)


@cue("MeridianStrikeMiss", -17.0, "Meridian release", 0.6, 0.55,
     "放したのに子午線が出ないとき（弾切れ、ウィンドウ外、全画面マップ）。オルゴールは鳴らさず、部品が3つ外れ、ばねが空回りして"
     "止まり、鈍い磁器の「ゴッ」で終わる。成功の音とはっきり違う。")
def meridian_strike_miss(s, rng):
    mix = seconds(0.6)
    for i, (at, f) in enumerate(((0.0, 2000), (0.05, 1700), (0.1, 1400))):
        place(mix, pan(dsp.brass_click(rng, f, decay=0.005, thud=0.4), -0.3 + 0.3 * i), at, -6 - i)
    gaps = [0.03 * 1.32 ** i for i in range(8)]
    times = list(np.cumsum(gaps))
    place(mix, dsp.ratchet(times, rng, freq=1900, pattern=[1.0 - 0.05 * i for i in range(8)],
                           gains_db=[-6 - 1.6 * i for i in range(8)], side=0.1, decay=0.004, thud=0.5), 0.12, -6)
    place(mix, dsp.porcelain_ring(620, 0.25, rng, decay=0.05, side=0.0), 0.43, -10)
    place(mix, dsp.thump(160, 90, 0.18, rng), 0.43, -12)
    return room(mix, 0.08, 0.6, 0.12)


@cue("MeridianLatticeWarn", -13.0, "Meridian release", 0.45, 0.6,
     "子午線の先頭が節点を通り、格子が分かれ始める（2〜3段階）。オルゴールを下から上へ5本かき鳴らし（F5 C6 F6 A♭6 C7）、"
     "真鍮の格子が「チャリ」と鳴る。MeridianLatticeFire の予告。")
def meridian_lattice_warn(s, rng):
    mix = seconds(0.45)
    for i, note in enumerate(("F5", "C6", "F6", "Ab6", "C7")):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.4, rng, decay=0.16), -0.4 + 0.2 * i), 0.025 * i, -6 + 0.4 * i)
    place(mix, dsp.porcelain_ring(4600, 0.1, rng, decay=0.02, side=0.35), 0.11, -12)
    place(mix, brass_ring(2900, 0.2, rng, decay=0.05, side=-0.3), 0.115, -12)
    return room(mix, 0.12, 0.42, 0.12)


@cue("MeridianLatticeFire", -11.5, "Meridian release", 2.8, 0.9,
     "格子が光る（締めの音）。格子の波紋（2tick ずつ）に合わせて櫛の滝が 0・33・67ms に3回こぼれ、柔らかな銅鑼のような"
     "低い響き（主音の F2）がゆっくり広がって長く減衰し、その下で静かな F マイナーのオルガンが膨らんで一緒に消える。"
     "オルガンを鳴り響かせず、余韻で締める。3段階 .9、2段階 .75。")
def meridian_lattice_fire(s, rng):
    mix = seconds(2.9)
    cascades = (("F6", "C6", "Ab5"), ("Ab6", "Eb6", "Bb5"), ("C7", "F6", "C6"))
    for ring, notes in enumerate(cascades):
        for j, note in enumerate(notes):
            place(mix, pan(dsp.box_tine(dsp.hz(note), 1.0, rng), (-0.5 + 0.5 * ring) * (1 if j % 2 else -1)),
                  ring * MERIDIAN_RIPPLE / 60 + 0.012 * j, -8 - 1.5 * j - ring)
    # The closing resonance on the home note, and a quiet F minor organ swelling under it and fading with it.
    place(mix, soft_gong(dsp.hz("F2"), 2.75, rng, decay=1.35, side=-0.05), 0.0, -2)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4", "Ab4")], 2.4, rng, attack=0.2, release=1.7,
                        harmonics=8, rolloff=1.6, chiff=0.0, breath=0.14)
    place(mix, pad, 0.03, -17)
    place(mix, brass_ring(1750, 0.9, rng, decay=0.3, side=0.2), 0.0, -18)
    place(mix, dsp.thump(95, 42, 0.6, rng), 0.0, -13)
    place(mix, dsp.shimmer(1.2, rng, count=14), 0.07, -20)
    return room(mix, 0.26, 2.8, 0.9)


@cue("MeridianHit", -20.0, "Meridian hits", 0.2, 0.35,
     "弾が当たったとき（持ち主の画面のみ、4tick に1回まで）。小さな磁器の「チッ」と櫛の倍音。")
def meridian_hit(s, rng):
    mix = seconds(0.2)
    place(mix, dsp.porcelain_ring(rng.uniform(3600, 4200), 0.12, rng, decay=0.015, side=0.1), 0.0, -2)
    place(mix, pan(dsp.box_tine(dsp.hz("F7"), 0.15, rng, decay=0.04, body=0, modes=dsp.COMB_MODES[:2]), -0.1), 0.0, -12)
    return room(mix, 0.06, 0.18, 0.06)


@cue("MeridianHitHeavy", -17.0, "Meridian hits", 0.45, 0.5,
     "重弾・子午線・格子が当たったとき（弾ごとに6tick に1回まで）。磁器の割れる音と真鍮の響き。")
def meridian_hit_heavy(s, rng):
    mix = seconds(0.45)
    place(mix, dsp.porcelain_crack(0.25, rng, count=7, spread=0.035), 0.0, -3)
    place(mix, brass_ring(1300, 0.35, rng, decay=0.1, side=0.1), 0.002, -10)
    place(mix, dsp.thump(140, 70, 0.15, rng), 0.0, -12)
    return room(mix, 0.1, 0.42, 0.1)


# ---------------------------------------------------------------- Last Witness (v2)
# Clocks of Content/Encounters/FirstSeverance/Rewards/WitnessRules.cs (real ticks, 1/60 s). Each cue fires once through
# DollCueClock on its tick (Client/Encounters/FirstSeverance/Weapons/WitnessVisuals.cs), so time baked into a file is
# measured from its trigger: a Warn ends where its Fire begins. Pitches stay on the F minor pentatonic; no bells
# (porcelain, brass and organ instead); the two spin loops are fixed-pitch PCM16 WAV loops, one per spin stage,
# crossfaded by the client (no live pitch bend).
from generate_ebon_reward_sfx import glide, reverse, sweep, taper  # noqa: E402  (shared helpers, imported unmodified)

W_TICK = 1 / 60
W_TESTIMONIES = tuple(16 + 29 * i for i in range(6))
W_TESTIMONY_LEAD, W_SEAL, W_THROW_WARN, W_THROW = 10, 174, 196, 218
W_BITES, W_RETURN = (6, 12, 17, 21), 22
W_LOCK, W_EXECUTE_WARN, W_EXECUTE, W_WITHDRAW = 16, 17, 28, 36
W_CRUISE_SPIN = 0.45                                   # rad/tick
W_TURN_PEAK = 2 * (4 * np.pi / 21) - W_CRUISE_SPIN     # 0.7468 rad/tick at the end of the Axiom turns
# Loops: a whole number of blade revolutions, so the whums land on the same blade angle every pass.
W_SPIN_REVOLUTIONS, W_SPIN_SAMPLES = 8, 82100           # 2 pi / 0.45 rad per tick = 13.963 ticks = 10262.5 samples
W_AXIOM_REVOLUTIONS, W_AXIOM_SAMPLES = 16, 98942        # 2 pi / 0.7468 = 8.414 ticks = 6183.9 samples
WITNESS = "Last Witness"
VERDICT = "Last Witness: Triangle Judgement"
SOURCES.update({
    "air_cut": ("sfx-sources/cc0/wind-FS60030-qubodup-air_cut.mp3",
                "qubodup, Air Cut (https://freesound.org/s/60030/, CC0 1.0, HQ preview)",
                "0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0", "qubodup"),
    "stick_woosh": ("sfx-sources/cc0/swing-FS352719-Dalesome-woosh_stick.mp3",
                    "Dalesome, woosh stick (https://freesound.org/s/352719/, CC0 1.0, HQ preview)",
                    "5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016", "Dalesome"),
    "swoosh": ("sfx-sources/cc0/swing-FS263595-PorkMuncher-swoosh.mp3",
               "PorkMuncher, swoosh (https://freesound.org/s/263595/, CC0 1.0, HQ preview)",
               "5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058", "PorkMuncher"),
    "woosh": ("sfx-sources/cc0/wind-FS683096-florianreichelt-woosh.mp3",
              "florianreichelt, woosh (https://freesound.org/s/683096/, CC0 1.0, HQ preview)",
              "3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265", "florianreichelt"),
    "sword_hit": ("sfx-sources/cc0/metal-FS442769-qubodup-sword_hit.mp3",
                  "qubodup, Sword Hit (https://freesound.org/s/442769/, CC0 1.0, HQ preview)",
                  "93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167", "qubodup"),
    "armor_strike": ("sfx-sources/cc0/metal-FS568170-Merrick079-sword_sound_1.mp3",
                     "Merrick079, sword sound 1 (https://freesound.org/s/568170/, CC0 1.0, HQ preview)",
                     "5f9ab16b7a74a205b1490001d4c913d0d55f561df796cb2d43c1a30b97c351b1", "Merrick079"),
    "rock_tumble": ("sfx-sources/cc0/impact-FS389618-_stubb-rock_tumble_2.mp3",
                    "_stubb, rock tumble 2 (https://freesound.org/s/389618/, CC0 1.0, HQ preview)",
                    "199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a", "_stubb"),
    "knife_slice": (KENNEY + "!OGG/knifeSlice2.ogg",
                    "Kenney RPG Audio knifeSlice2.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                    "6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def", "Kenney"),
    "draw_knife": (KENNEY + "!OGG/drawKnife3.ogg",
                   "Kenney RPG Audio drawKnife3.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                   "a11ae62fb1a628425769d11a9de394980ad8909c31f4c9a4316f226963e21caf", "Kenney"),
    "chop": (KENNEY + "!OGG/chop.ogg",
             "Kenney RPG Audio chop.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
             "d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486", "Kenney"),
    "creak1": (KENNEY + "!OGG/creak1.ogg",
               "Kenney RPG Audio creak1.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
               "8a346186fd297254248cab8e8117060a52a5cf2a84f603153a762108550ea95e", "Kenney"),
    "creak2": (KENNEY + "!OGG/creak2.ogg",
               "Kenney RPG Audio creak2.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
               "8a990afdc03aebb91d528f5385e2f95582dbfa8e2c12c71098ab01be9142294a", "Kenney"),
})


def window(s, key, start, end, rate=1.0, hp_=None, lp_=None, fade_in=0.002, fade_out=0.02):
    """A trimmed, filtered window of a store recording (stereo), for reversing or shaping before it is placed."""
    x = s.get(key)[round(start * RATE):round(end * RATE)].copy()
    if rate != 1.0:
        x = speed(x, rate)
    if hp_:
        x = hp(x, hp_)
    if lp_:
        x = lp(x, lp_)
    return fades(x, fade_in, fade_out)


def brass_fitting(freq, dur, rng, decay=0.2, side=0.0):
    """A struck brass fitting: inharmonic partials 1 / 1.47 / 2.09 / 2.76 / 3.53 with a short metallic tick. Dull and
    damped (the upper partials die first and nothing hums on), so it never reads as a tuned or church bell."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    y = np.zeros(n)
    for ratio, amp in ((1.0, 1.0), (1.47, 0.62), (2.09, 0.45), (2.76, 0.28), (3.53, 0.16)):
        if freq * ratio >= dsp.CEILING_HZ:
            break
        y += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / (decay / ratio ** 0.8))
    y *= np.clip(t / 0.0004, 0, 1)
    k = round(0.0015 * RATE)
    y[:k] += hp(base.noise(k, rng), 2800)[:, 0] * np.linspace(1, 0, k) * 0.4
    y = dsp.tail_fade(y / max(1e-9, np.abs(y).max()), 0.02)
    return pan(y, side)


# A large brass plate under a felt mallet: ratio, amplitude and share of the decay for each partial. The fundamental
# beats slowly against a near twin; the partials are deliberately off the harmonic series (no tuned or church bell).
GONG_MODES = ((1.0, 1.0, 1.0), (1.0041, 0.5, 0.92), (1.49, 0.40, 0.62), (2.02, 0.42, 0.5), (2.41, 0.2, 0.4),
              (2.97, 0.18, 0.3), (3.6, 0.09, 0.24), (4.23, 0.06, 0.2), (5.13, 0.035, 0.15), (6.28, 0.02, 0.12))


def brass_gong(freq, dur, rng, decay=1.6, bloom=0.12, side=0.0):
    """A soft gong-like resonance: a large brass plate struck with a felt mallet. The upper partials die first while two
    middle ones bloom a moment after the strike, so the tone swells and then decays for seconds instead of clanging;
    the mallet is a dull low thud with no bright attack."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    y = np.zeros((n, 2))
    for i, (ratio, amp, share) in enumerate(GONG_MODES):
        if freq * ratio >= dsp.CEILING_HZ:
            break
        env = np.exp(-t / (decay * share))
        if i in (2, 3):
            env = env * (1 - np.exp(-t / bloom))
        tone = amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 2 * np.pi)) * env
        y += pan(tone, float(np.clip(side + rng.uniform(-0.35, 0.35), -1, 1)))
    y *= (0.5 - 0.5 * np.cos(np.pi * np.clip(t / 0.008, 0, 1)))[:, None]
    k = round(0.03 * RATE)
    y[:k] += lp(base.noise(k, rng), 600) * (np.linspace(1, 0, k) ** 2)[:, None] * 0.5
    return dsp.tail_fade(y / max(1e-9, np.abs(y).max()), 0.05)


def air_pulses(dur, rng, period, low=350, high=2600, first=0.0, sharp=6.0):
    """Band-limited air that swells once per `period` s (a blade passing): the whir of a spinning blade."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    phase = 2 * np.pi * (t - first) / period
    gate = (0.5 + 0.5 * np.cos(phase)) ** sharp
    air = signal.sosfilt(signal.butter(2, (low, high), "bandpass", fs=RATE, output="sos"), base.noise(n, rng), axis=0)
    return air / max(1e-9, np.abs(air).max()) * gate[:, None]


def testimony_warn(s, rng, note):
    """A testimony speaks: one music-box tooth on the ladder, the edge cracking a moment later as the shard slides out."""
    mix = seconds(0.6)
    place(mix, pan(dsp.box_tine(dsp.hz(note), 0.56, rng), 0.05), 0.0, -3)
    place(mix, dsp.porcelain_crack(0.08, rng, count=4, spread=0.012, low=4200, high=9000, decay=(0.004, 0.012)), 0.055, -15)
    place(mix, pan(dsp.brass_click(rng, 4200, decay=0.002, thud=0.0, tick=0.3), 0.3), 0.06, -24)
    return room(mix, 0.1, 0.5, 0.16)


@cue("WitnessSettle", -17, "Last Witness", 0.6, 0.7,
     "刃が手元に現れて吊られる（押し始め、12tick で 24px 下りて止まる）。空気が上がり、真鍮の軸受けが2回小さく鳴って、"
     "低めの磁器の響き（F5 と C6）で止まる。")
def witness_settle(s, rng):
    mix = seconds(0.75)
    place(mix, sweep(0.22, rng, 900, 3200, shape=lambda t: np.sin(np.pi * np.clip(t / 0.22, 0, 1)) ** 2), 0.0, -16)
    place(mix, pan(dsp.brass_click(rng, 1800, decay=0.006, thud=0.5), -0.2), 0.02, -12)
    place(mix, pan(dsp.brass_click(rng, 3100, decay=0.005, thud=0.2), 0.15), 0.19, -10)
    place(mix, dsp.porcelain_ring(dsp.hz("F5"), 0.5, rng, decay=0.09, side=0.1), 0.19, -8)
    place(mix, dsp.porcelain_ring(dsp.hz("C6"), 0.4, rng, decay=0.06, side=-0.2), 0.195, -15)
    place(mix, dsp.thump(220, 90, 0.18, rng), 0.19, -16)
    return room(mix, 0.12, 0.6, 0.2)


@cue("WitnessTestimonyWarn1", -20, "Last Witness", 0.55, 0.75,
     "1つ目の証言の予兆（発射の10tick前）。オルゴールの歯 F5 が1音鳴り、刃の縁に細いひびが入る磁器の音が続く。証言ごとに音が上がる。")
def witness_testimony_warn1(s, rng):
    return testimony_warn(s, rng, "F5")


@cue("WitnessTestimonyWarn2", -20, "Last Witness", 0.55, 0.75, "2つ目の証言の予兆。A♭5。")
def witness_testimony_warn2(s, rng):
    return testimony_warn(s, rng, "Ab5")


@cue("WitnessTestimonyWarn3", -20, "Last Witness", 0.55, 0.75, "3つ目の証言の予兆。B♭5。")
def witness_testimony_warn3(s, rng):
    return testimony_warn(s, rng, "Bb5")


@cue("WitnessTestimonyWarn4", -20, "Last Witness", 0.55, 0.75, "4つ目の証言の予兆。C6。")
def witness_testimony_warn4(s, rng):
    return testimony_warn(s, rng, "C6")


@cue("WitnessTestimonyWarn5", -20, "Last Witness", 0.55, 0.75, "5つ目の証言の予兆。E♭6。")
def witness_testimony_warn5(s, rng):
    return testimony_warn(s, rng, "Eb6")


@cue("WitnessTestimonyWarn6", -20, "Last Witness", 0.55, 0.75, "6つ目（最後）の証言の予兆。F6 で1オクターブ上がり切る。")
def witness_testimony_warn6(s, rng):
    return testimony_warn(s, rng, "F6")


@cue("WitnessTestimonyFire", -17, "Last Witness", 0.4, 0.8,
     "証言の発射（6回とも同じ音）。磁器の破片がはじけ、小さな真鍮のクリックと、短く高い風切りが追尾の破片を送り出す。")
def witness_testimony_fire(s, rng):
    mix = seconds(0.45)
    place(mix, dsp.porcelain_crack(0.12, rng, count=6, spread=0.018, low=3000, high=8500), 0.0, -4)
    place(mix, pan(dsp.brass_click(rng, 2900, decay=0.004, thud=0.15), 0.1), 0.004, -10)
    lay(mix, s, "air_cut", 0.05, 0.30, 0.01, -9, rate=1.25, hp_=1800, side=0.2)
    place(mix, sweep(0.2, rng, 7000, 2200, shape=lambda t: np.exp(-t / 0.06)), 0.01, -17)
    return room(mix, 0.08, 0.38, 0.12)


@cue("WitnessShardHit", -20, "Last Witness", 0.3, 0.7,
     "破片が当たった音（自分の攻撃だけ）。磁器の小さな打音と、鈍く短い手応え。")
def witness_shard_hit(s, rng):
    mix = seconds(0.3)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.12, rng, decay=0.02), 0.0, -6)
    place(mix, dsp.porcelain_crack(0.06, rng, count=3, spread=0.01), 0.0, -10)
    place(mix, dsp.thump(320, 120, 0.12, rng), 0.0, -8)
    return room(mix, 0.06, 0.26, 0.1)


@cue("WitnessSeal", -13, "Last Witness", 0.85, 0.8,
     "判決の封印（174tick、ここから放しても投擲は止まらない）。真鍮のラチェットが3回かかり、6つの切れ込みの光が目へ走るのに合わせて"
     "オルゴールが C7 から C6 へ駆け下り、目が光る瞬間に磁器の F6 と柔らかなオルガン（F と C）。")
def witness_seal(s, rng):
    mix = seconds(0.9)
    place(mix, dsp.ratchet((0.0, 0.03, 0.06), rng, freq=2300, gains_db=(-4, -3, -1), side=-0.1), 0.0, -6)
    for i, note in enumerate(("C7", "Bb6", "Ab6", "F6", "Eb6", "C6")):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.5, rng, decay=0.16), 0.3 - 0.12 * i), 0.02 + 0.02 * i, -9 - 0.4 * i)
    place(mix, dsp.porcelain_ring(dsp.hz("F6"), 0.5, rng, decay=0.1), 0.13, -7)
    place(mix, dsp.shimmer(0.4, rng, count=6), 0.14, -17)
    place(mix, dsp.organ_pad([dsp.hz("F3"), dsp.hz("C4")], 0.5, rng, attack=0.04, release=0.35, harmonics=8, chiff=0.04,
                             breath=0.1), 0.12, -18)
    return room(mix, 0.14, 0.8, 0.25)


@cue("WitnessThrowWarn", -13, "Last Witness", 0.4, 0.85,
     "投擲の予兆（196tick から投擲の 218tick まで、ちょうど 22tick）。5度のパイプ2本（F3 と C4）がふくらみ、ばねの張る軋みと"
     "低く上がる刃のうなりの上で、最後の 8tick（振り抜き）に風が駆け上がって投擲の音へ渡す。")
def witness_throw_warn(s, rng):
    length = (W_THROW - W_THROW_WARN) * W_TICK
    mix = seconds(length + 0.05)
    place(mix, dsp.organ_pad([dsp.hz("F3"), dsp.hz("C4")], length, rng, attack=length * 0.9, release=0.012, harmonics=10,
                             rolloff=1.2, chiff=0.0, breath=0.12), 0.0, -6)
    lay(mix, s, "creak1", 0.17, 0.5, 0.0, -13, rate=0.8, hp_=500, side=-0.2, fade_out=0.05)
    place(mix, glide(70, 150, length, harmonics=(1.0, 0.5, 0.25), curve=1.6, attack=0.08), 0.0, -12)
    whip = (W_THROW - 210) * W_TICK
    place(mix, sweep(whip, rng, 600, 5000, shape=lambda t: (t / whip) ** 2), length - whip, -8)
    return taper(mix[:round(length * RATE)], 0.012)


@cue("WitnessThrowFire", -11.5, "Last Witness", 0.95, 0.95,
     "投擲（218tick）。真鍮の留め金が外れる音、重い振り抜きの風、低い F2 のオルガンの一撃（息の頭つき）と、"
     "回り始めた刃のうなり（刃の回転 4.3 回転/秒に合わせた風の脈）。")
def witness_throw_fire(s, rng):
    mix = seconds(1.0)
    lay(mix, s, "metal_latch", 0.03, 0.2, 0.0, -6, rate=0.85, hp_=600)
    place(mix, pan(dsp.brass_click(rng, 1500, decay=0.012, thud=0.8), 0.0), 0.0, -6)
    lay(mix, s, "stick_woosh", 0.09, 0.45, 0.0, -2, rate=1.1, hp_=250)
    lay(mix, s, "swoosh", 1.0, 1.35, 0.02, -8, hp_=300)
    place(mix, dsp.organ_pad([dsp.hz("F2"), dsp.hz("C3"), dsp.hz("F3")], 0.4, rng, attack=0.008, release=0.3, harmonics=10,
                             rolloff=1.1, chiff=0.08, breath=0.05), 0.0, -7)
    place(mix, dsp.thump(140, 55, 0.35, rng), 0.0, -6)
    half_turn = np.pi / W_CRUISE_SPIN * W_TICK
    whir = air_pulses(0.7, rng, half_turn) * np.clip(np.arange(round(0.7 * RATE)) / RATE / 0.25, 0, 1)[:, None]
    place(mix, whir, 0.08, -13)
    return room(mix, 0.16, 0.95, 0.4)


@cue("WitnessAxiomWarn", -11.5, "Last Witness", 0.75, 0.9,
     "刃が標的に食い込む（最初の接触、アクシオムの2回転の始まり）。鋼が磁器に食い込む砕ける音、低い手応え、真鍮の鈍い響きの尾。")
def witness_axiom_warn(s, rng):
    mix = seconds(0.8)
    place(mix, dsp.porcelain_crack(0.2, rng, count=12, spread=0.04, low=1800, high=7500), 0.0, -3)
    lay(mix, s, "sword_hit", 0.0, 0.35, 0.0, -8, hp_=1200)
    lay(mix, s, "armor_strike", 0.05, 0.4, 0.0, -10, hp_=500, side=0.15)
    place(mix, dsp.thump(160, 60, 0.4, rng), 0.0, -4)
    place(mix, brass_fitting(380, 0.6, rng, decay=0.22, side=-0.1), 0.01, -13)
    return room(mix, 0.14, 0.75, 0.3)


def axiom_bite(s, rng, note, step):
    """One half-turn bite of the Axiom turns: a brass gear tooth, a steel shing and a dry porcelain tone on the ladder."""
    mix = seconds(0.35)
    place(mix, pan(dsp.brass_click(rng, 2400 * (1 + 0.06 * step), decay=0.005, thud=0.3), 0.0), 0.0, -6)
    lay(mix, s, "knife_slice", 0.26, 0.5, 0.0, -11, rate=1.0 + 0.05 * step, hp_=2500, side=(-0.3, 0.3, -0.2, 0.2)[step])
    place(mix, dsp.porcelain_ring(dsp.hz(note), 0.3, rng, decay=0.07 + 0.01 * step), 0.003, -6)
    place(mix, dsp.thump(200, 90, 0.12, rng), 0.0, -12)
    return room(mix, 0.08, 0.32, 0.1)


@cue("WitnessAxiomFire1", -17, "Last Witness", 0.35, 0.8,
     "1回目の半回転の噛みつき（接触から6tick）。真鍮の歯車が1歯進み、鋼のシャリンと磁器の F5。回転が速まるほど4つの音が上がる。")
def witness_axiom_fire1(s, rng):
    return axiom_bite(s, rng, "F5", 0)


@cue("WitnessAxiomFire2", -16, "Last Witness", 0.35, 0.8, "2回目の噛みつき（12tick）。A♭5。")
def witness_axiom_fire2(s, rng):
    return axiom_bite(s, rng, "Ab5", 1)


@cue("WitnessAxiomFire3", -15, "Last Witness", 0.35, 0.8, "3回目の噛みつき（17tick）。C6。")
def witness_axiom_fire3(s, rng):
    return axiom_bite(s, rng, "C6", 2)


@cue("WitnessAxiomFire4", -13, "Last Witness", 0.35, 0.8, "4回目、最後の噛みつき（21tick、2回転の終わり）。F6 で1オクターブ上がり切る。")
def witness_axiom_fire4(s, rng):
    return axiom_bite(s, rng, "F6", 3)


@cue("WitnessAxiomMiss", -17, "Last Witness", 0.6, 0.8,
     "何にも当たらなかった（カーソル位置で止まって空中で回る）。中空の風切りと、下がるオルゴール2音（C6→A♭5）、砕ける音はない。"
     "このあとの噛みつき4音も小さく鳴る。")
def witness_axiom_miss(s, rng):
    mix = seconds(0.6)
    lay(mix, s, "air_cut", 0.04, 0.32, 0.0, -4, rate=0.85, hp_=200)
    place(mix, pan(dsp.box_tine(dsp.hz("C6"), 0.3, rng, decay=0.1), -0.2), 0.03, -16)
    place(mix, pan(dsp.box_tine(dsp.hz("Ab5"), 0.3, rng, decay=0.1), 0.2), 0.11, -17)
    place(mix, dsp.thump(260, 150, 0.12, rng), 0.0, -18)
    return room(mix, 0.12, 0.55, 0.2)


@cue("WitnessReturnWarn", -17, "Last Witness", 0.55, 0.8,
     "刃が引き抜けて戻り始める（22tick）。逆回しの風が吸い込まれ、真鍮のばねが引かれて上がる音。")
def witness_return_warn(s, rng):
    mix = seconds(0.6)
    place(mix, reverse(window(s, "woosh", 0.35, 0.95, rate=1.4, hp_=300)), 0.0, -6)
    lay(mix, s, "creak2", 0.03, 0.3, 0.05, -14, rate=1.3, hp_=700, side=0.2)
    place(mix, glide(300, 900, 0.3, harmonics=(1.0, 0.4, 0.2), curve=1.5), 0.08, -20)
    place(mix, pan(dsp.brass_click(rng, 2100, decay=0.006, thud=0.2), 0.0), 0.36, -12)
    return room(mix, 0.1, 0.55, 0.15)


@cue("WitnessReturnFire", -13, "Last Witness", 0.55, 0.8,
     "受け止め（戻った刃が吊りの位置か手に収まる。光の輪が開く）。真鍮の留め金が閉まり、磁器の F5 と C6 が柔らかく鳴る。")
def witness_return_fire(s, rng):
    mix = seconds(0.6)
    lay(mix, s, "metal_latch", 0.035, 0.2, 0.0, -5, rate=0.9)
    place(mix, pan(dsp.brass_click(rng, 1600, decay=0.01, thud=0.7), 0.0), 0.0, -7)
    place(mix, dsp.porcelain_ring(dsp.hz("F5"), 0.5, rng, decay=0.12, side=-0.1), 0.006, -7)
    place(mix, dsp.porcelain_ring(dsp.hz("C6"), 0.45, rng, decay=0.1, side=0.15), 0.012, -11)
    place(mix, dsp.shimmer(0.35, rng, count=5), 0.03, -18)
    return room(mix, 0.12, 0.55, 0.25)


def periodic_band(n, rng, low, high, tilt=0.0):
    """Stereo band noise that repeats exactly every n samples (a random-phase spectrum on the loop's own harmonics)."""
    f = np.fft.rfftfreq(n, 1 / RATE)
    magnitude = ((f >= low) & (f <= high)) * (np.maximum(f, 1.0) / low) ** tilt
    x = np.column_stack([np.fft.irfft(magnitude * np.exp(1j * rng.uniform(0, 2 * np.pi, len(f))), n) for _ in range(2)])
    return x / max(1e-9, np.abs(x).max())


def wrap(loop, x, at):
    """Add x into a loop buffer circularly from sample `at`."""
    index = (at + np.arange(len(x))) % len(loop)
    np.add.at(loop, index, x)


def spin_loop(rng, samples, revolutions, band, tick_hz, rotor):
    """A blade turning in place, exactly periodic: band air swelling twice per turn (the long tip pass, then the
    shorter pommel pass), a faint brass tick once per turn and a low rotor hum on whole harmonics of the turn rate."""
    t = np.arange(samples)
    turn = 2 * np.pi * revolutions * t / samples
    whum = (0.5 + 0.5 * np.cos(turn)) ** 6 + 0.5 * (0.5 + 0.5 * np.cos(turn - np.pi)) ** 8
    out = periodic_band(samples, rng, *band) * whum[:, None]
    hum = sum(a * np.sin(k * revolutions * 2 * np.pi * t / samples + rng.uniform(0, 2 * np.pi)) for k, a in rotor)
    out += np.column_stack((hum, hum)) / max(1e-9, np.abs(hum).max()) * 0.22 * (0.7 + 0.3 * whum)[:, None]
    period = samples / revolutions
    for r in range(revolutions):
        click = pan(dsp.brass_click(rng, tick_hz * (1 + 0.03 * (r % 4)), decay=0.003, thud=0.1, dur=0.03), 0.0)
        wrap(out, click * 0.18, round(r * period + period * 0.42))
    return out


@cue("WitnessSpinLoop", -15, "Last Witness", 1.9, 0.5,
     "飛んでいる刃のうなり（往路と帰路、巡航の回転 0.45 rad/tick）。8回転ちょうどの継ぎ目のないループで、1回転に2回の風の脈"
     "（切っ先と柄頭）、かすかな真鍮の刻み、低い回転音。ゲームではアクシオムのループと数tickで入れ替わる（音程は動かさない）。",
     loop_master="builder", loop=True)
def witness_spin_loop(s, rng):
    return spin_loop(rng, W_SPIN_SAMPLES, W_SPIN_REVOLUTIONS, (320, 2400), 2600, ((10, 1.0), (14, 0.6), (21, 0.35)))


@cue("WitnessAxiomLoop", -15, "Last Witness", 2.3, 0.55,
     "アクシオムの2回転中のうなり（最高速 0.747 rad/tick に合わせた速い風の脈、より明るい帯域）。16回転ちょうどのループ。",
     loop_master="builder", loop=True)
def witness_axiom_loop(s, rng):
    return spin_loop(rng, W_AXIOM_SAMPLES, W_AXIOM_REVOLUTIONS, (520, 4200), 3300, ((6, 1.0), (9, 0.6), (13, 0.3)))


@cue("VerdictStakeWarn", -13, "Last Witness: Triangle Judgement", 0.35, 0.85,
     "ステルスの三角の審判、杭の予兆（審判の 1tick から杭が刺さる 16tick まで）。オルガンのペダル F1・F2 がふくらみ、"
     "剣3本ぶんのガラスのような高い倍音（C6・F6・A♭6）が順に現れ、落ちてくる剣の口笛が下がって杭の音へ渡す。")
def verdict_stake_warn(s, rng):
    length = (W_LOCK - 1) * W_TICK
    mix = seconds(length + 0.05)
    place(mix, dsp.organ_pad([dsp.hz("F1"), dsp.hz("F2"), dsp.hz("C3")], length, rng, attack=length * 0.85, release=0.012,
                             harmonics=12, rolloff=1.0, chiff=0.0, breath=0.15), 0.0, -5)
    n = round(length * RATE)
    t = np.arange(n) / RATE
    for i, note in enumerate(("C6", "F6", "Ab6")):
        start = 0.03 * i
        tone = np.sin(2 * np.pi * dsp.hz(note) * t) * np.clip((t - start) / (length - start), 0, 1) ** 2
        place(mix, pan(tone, -0.5 + 0.5 * i), 0.0, -17)
        fall = (W_LOCK - 10) * W_TICK
        place(mix, pan(glide(2600 - 200 * i, 900, fall - 0.01 * i, harmonics=(1.0, 0.2), curve=0.7, attack=0.01), -0.6 + 0.6 * i),
              length - fall + 0.01 * i, -19)
    return taper(mix[:round(length * RATE)], 0.01)


@cue("VerdictStakeFire", -11.5, "Last Witness: Triangle Judgement", 0.85, 0.95,
     "剣3本が三角の角に刺さる（16tick、ここで三角が止まる）。30ミリ秒の間に鉄が磁器へ刺さる重い音が3つ、砕ける音と真鍮の鈍い響き。")
def verdict_stake_fire(s, rng):
    mix = seconds(0.9)
    for i, at in enumerate((0.0, 0.012, 0.027)):
        place(mix, dsp.thump(130 - 10 * i, 50, 0.35, rng), at, -5)
        place(mix, dsp.porcelain_crack(0.12, rng, count=6, spread=0.02, low=1500, high=6000), at, -8)
        lay(mix, s, "chop", 0.03, 0.2, at, -8, rate=0.8 + 0.06 * i, side=(-0.6, 0.0, 0.6)[i])
    place(mix, brass_fitting(310, 0.7, rng, decay=0.25), 0.02, -13)
    lay(mix, s, "rock_tumble", 0.06, 0.4, 0.03, -17, lp_=2500)
    return room(mix, 0.16, 0.85, 0.35)


@cue("VerdictExecuteWarn", -13, "Last Witness: Triangle Judgement", 0.25, 0.85,
     "処刑の予兆（光が杭から杭へ書かれる 17tick から処刑の 28tick まで）。オルゴールの速いトレモロが F6 から F7 へ駆け上がり、"
     "下でオルガンの和音（F・C・E♭・F）がふくらむ。")
def verdict_execute_warn(s, rng):
    length = (W_EXECUTE - W_EXECUTE_WARN) * W_TICK
    mix = seconds(length + 0.05)
    notes = ("F6", "Ab6", "Bb6", "C7", "Eb7", "F7", "Ab6", "C7", "F7")
    for i, note in enumerate(notes):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.2, rng, decay=0.06, body=0.2), -0.4 + 0.1 * i), length * i / len(notes), -12 + 0.5 * i)
    place(mix, dsp.organ_pad([dsp.hz("F3"), dsp.hz("C4"), dsp.hz("Eb4"), dsp.hz("F4")], length, rng, attack=length * 0.9,
                             release=0.01, harmonics=9, rolloff=1.2, chiff=0.0, breath=0.1), 0.0, -8)
    return taper(mix[:round(length * RATE)], 0.008)


@cue("VerdictExecuteFire", -11.5, "Last Witness: Triangle Judgement", 2.7, 0.95,
     "処刑（28tick、185px の三角の内側に当たる）。磁器が砕けて散り、低い真鍮の板を柔らかく打った銅鑼のような響き（F2）が"
     "ふくらんでから長く消えていく。その下でオルガンが F マイナーの和音（F2 F3 C4 A♭4）を小さく鳴らす。オルガンを大きく"
     "鳴らす一撃はやめ、鐘も使っていない。")
def verdict_execute_fire(s, rng):
    mix = seconds(2.8)
    place(mix, dsp.porcelain_crack(0.25, rng, count=14, spread=0.06), 0.0, -3)
    place(mix, base.glass(0.5, rng, count=18, spread=0.3, low=2500, high=8000), 0.01, -13)
    place(mix, dsp.thump(100, 38, 0.5, rng), 0.0, -8)
    place(mix, brass_gong(dsp.hz("F2"), 2.6, rng, decay=1.0), 0.004, -2)
    chord = [dsp.hz(n) for n in ("F2", "F3", "C4", "Ab4")]
    place(mix, dsp.organ_pad(chord, 2.3, rng, attack=0.25, release=1.6, harmonics=6, rolloff=1.7, chiff=0.0, breath=0.08), 0.02, -16)
    lay(mix, s, "rock_tumble", 0.06, 0.6, 0.03, -20, lp_=2200)
    return room(mix, 0.22, 2.7, 1.0)


@cue("VerdictExecuteMiss", -13, "Last Witness: Triangle Judgement", 1.0, 0.85,
     "処刑が空振りした（三角の中に誰もいない）。オルガンが和音を打たずに5度（F3 と C4）を息だけでかすかに鳴らし、磁器の粉がまばらに落ちる。")
def verdict_execute_miss(s, rng):
    mix = seconds(1.0)
    place(mix, dsp.organ_pad([dsp.hz("F3"), dsp.hz("C4")], 0.8, rng, attack=0.12, release=0.6, harmonics=6, rolloff=1.6, chiff=0.0,
                             breath=0.35), 0.0, -8)
    place(mix, base.glass(0.6, rng, count=10, spread=0.5, low=3000, high=8000, decay=(0.01, 0.05)), 0.05, -17)
    place(mix, dsp.thump(200, 120, 0.2, rng), 0.0, -18)
    return room(mix, 0.18, 0.95, 0.4)


@cue("VerdictWithdraw", -20, "Last Witness: Triangle Judgement", 0.6, 0.7,
     "剣が光の中へ上へ引き上げられる（36tick）。金属が上へ滑る音と、高い磁器のきらめき（C7 と F7）。")
def verdict_withdraw(s, rng):
    mix = seconds(0.6)
    place(mix, glide(900, 2600, 0.35, harmonics=(1.0, 0.3, 0.12), curve=1.3), 0.0, -15)
    lay(mix, s, "draw_knife", 0.08, 0.4, 0.0, -10, rate=1.15, hp_=1500)
    place(mix, pan(dsp.box_tine(dsp.hz("C7"), 0.3, rng, decay=0.08, body=0), 0.3), 0.12, -14)
    place(mix, pan(dsp.box_tine(dsp.hz("F7"), 0.3, rng, decay=0.06, body=0), -0.3), 0.2, -16)
    return room(mix, 0.12, 0.55, 0.2)
# ---------------------------------------------------------------- Choir of the Unmade (2026-10)
# The concert (Content/Encounters/FirstSeverance/Rewards/ChoirConcertRules.cs, mirrored here and pinned by
# tools/tests/test_doll_choir.py): 18 beats at 100 BPM (36 ticks), count-in taps on ticks 1 and 36, a verse of six
# sung notes per voice from tick 72 (part p = ordinal & 3 a sixteenth = 9 ticks later), the organ at 288, pipe rank r
# seated at 332 + 5r, the inhale (the chorus warning) at 360, the chorus at 396 for 180 ticks, the release at 576.
# Every pitch is F minor pentatonic (sung one octave under the shared ladder): Fm7 Fm7 Bb7sus Bb7sus Abmaj7 Ebsus
# in the verse, Bb7sus -> Fm9 -> an open F5 fifth in the chorus. The faceless doll voice is original additive
# synthesis through soprano formants (no recordings), the organ the additive flue pipe of doll_sfx_dsp.
CHOIR_TICK = 1 / 60
CHOIR_BEAT = 36 * CHOIR_TICK                     # 0.6 s
CHOIR_SUNG = ("F4", "Ab4", "Bb4", "C5", "Eb5", "F5", "Ab5", "Bb5", "C6")
CHOIR_LINES = ((6, 8, 7, 6, 8, 7), (5, 4, 4, 5, 4, 4), (3, 1, 2, 5, 3, 1), (0, 3, 2, 4, 1, 4))
CHOIR_PIPES = ("F3", "C4", "F4", "Ab4", "C5", "Eb5")
CHOIR_TAPS, CHOIR_VERSE, CHOIR_GATHER, CHOIR_INHALE, CHOIR_FIRE, CHOIR_RELEASE = (1, 36), 72, 288, 360, 396, 576


def choir_pipe_seat(rank):
    return 324 + 5 * rank + 8


def choir_raised(voices):
    return max(2, min(6, voices + 1))


# Chorus lines in the order voices add them (1..6): each is (Bb7sus, Fm9, F5) for 1.2, 1.2 and 0.6 s. Lines 1-4 are
# pure pentatonic; the fifth adds Fm9's G as its colour; the sixth is the bass.
CHORUS_LINES = (("Ab5", "C6", "C6"), ("F5", "Ab5", "F5"), ("Eb5", "Eb5", "C5"), ("Bb4", "C5", "F4"), ("F4", "G4", "C4"),
                ("Bb3", "F3", "F3"))
CHORUS_ORGAN = ((("Bb1", "Bb2"), ("F3", "Ab3", "Eb4")), (("F1", "F2"), ("C3", "Ab3", "Eb4")), (("F1", "F2"), ("C3", "F3", "C4")))
CHORUS_CHANGES = (0.0, 1.2, 2.4)
CHORUS_CUT = 3.0
# The close is restrained (the owner's 2026-10-03 A/B on another raid's finishers: a quiet organ and a soft gong with a
# long tail over an organ played out loud): the open fifth falls away by CHORUS_FALL dB into the cut, and
# ChoirChorusEnd, which plays on the cut, carries the ending.
CHORUS_FALL = 7.0
# Ticks over which a stopped chorus cue fades: a lost target (ChoirConcertRules.CloseTicks) and a target that died
# under the quiet close (ChoirClient.FinishFadeTicks).
CHORUS_CANCEL_FADE, CHORUS_FINISH_FADE = 6, 30

# Soprano formants (Hz, bandwidth Hz, dB): a dark "ah" leaning to "oh", and a closed-mouth hum.
VOWEL_AH = ((640, 90, 0.0), (1050, 110, -8.0), (2860, 150, -26.0), (3850, 190, -22.0), (4950, 240, -46.0))
VOWEL_MM = ((290, 60, 0.0), (1350, 160, -26.0), (2400, 220, -34.0))


def _formant_gain(freq, formants):
    return sum(10 ** (g / 20) / (1 + ((freq - f) / (bw / 2)) ** 2) for f, bw, g in formants)


def doll_voice(contour, dur, rng, vowel=VOWEL_AH, attack=0.045, release=0.14, vibrato=0.0038, rate=5.3, breath=0.05,
               tilt=0.85, side=0.0):
    """A faceless doll's sung vowel: harmonics of a gliding, vibrating pitch shaped by soprano formants (the first
    formant rises to follow a high note), a raised-cosine attack and release, and a little formant-coloured
    breath. `contour` is a frequency or a per-sample frequency array; mono, panned to `side`."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    f0 = np.full(n, float(contour)) if np.isscalar(contour) else np.asarray(contour, dtype=float)[:n]
    vib = 1 + vibrato * np.sin(2 * np.pi * rate * t + rng.uniform(0, 2 * np.pi)) * np.clip((t - 0.09) / 0.18, 0, 1)
    f = f0 * vib
    phase = 2 * np.pi * np.cumsum(f) / RATE
    mean = float(np.median(f0))
    formants = [(max(fq, mean * 1.06) if i == 0 else fq, bw, g) for i, (fq, bw, g) in enumerate(vowel)]
    y = np.zeros(n)
    for h in range(1, 48):
        if mean * h >= 9500:
            break
        y += _formant_gain(mean * h, formants) / h ** tilt * np.sin(h * phase + rng.uniform(0, 2 * np.pi))
    rise = np.clip(t / attack, 0, 1)
    fall = np.clip((dur - t) / release, 0, 1)
    shape = (0.5 - 0.5 * np.cos(np.pi * rise)) * (0.5 - 0.5 * np.cos(np.pi * fall))
    y = y / max(1e-9, np.abs(y).max()) * shape
    air = base.noise(n, rng)[:, 0]
    breathy = sum(10 ** (g / 20) * signal.sosfilt(signal.butter(2, (fq * 0.85, min(fq * 1.15, 0.45 * RATE)), "bandpass", fs=RATE,
                                                                output="sos"), air) for fq, _, g in formants[:3])
    y = y + breathy / max(1e-9, np.abs(breathy).max()) * breath * shape
    return pan(y, side)


def glide_contour(points, dur, glide=0.035):
    """Per-sample frequency through (time, Hz) points, with half-cosine portamento of `glide` s into each step."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    f = np.full(n, points[0][1], dtype=float)
    for (at, freq), (_, previous) in zip(points[1:], points[:-1]):
        k = np.clip((t - at) / glide, 0, 1)
        f = np.where(t >= at, previous * (freq / previous) ** (0.5 - 0.5 * np.cos(np.pi * k)), f)
    return f


def inhale(dur, rng, vowel=VOWEL_AH, low=700, curve=1.5):
    """A doll drawing breath: noise through the vowel's first formants, high-passed, swelling over `dur`."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    air = base.noise(n, rng)
    shaped = sum(10 ** (g / 20) * signal.sosfilt(signal.butter(2, (fq * 0.8, min(fq * 1.25, 0.45 * RATE)), "bandpass", fs=RATE,
                                                               output="sos"), air, axis=0) for fq, _, g in vowel[:3])
    shaped = hp(shaped, low)
    shaped /= max(1e-9, np.abs(shaped).max())
    return shaped * ((t / dur) ** curve * np.clip((dur - t) / 0.02, 0, 1))[:, None]


def porcelain_onset(freq, rng, gain=1.0):
    """The glassy porcelain onset of a doll's note: a dry ceramic ring two octaves up and a short tine."""
    return dsp.porcelain_ring(min(freq * 4, 8000), 0.12, rng, decay=0.03) * gain


def organ_glide(freqs, dur, rng, cents, attack=0.02, harmonics=9, rolloff=1.25, breath=0.08):
    """An additive flue chord whose wind fails: every pipe follows the same pitch curve (`cents`, per sample)."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    ratio = 2 ** (np.asarray(cents)[:n] / 1200)
    out = np.zeros((n, 2))
    for i, f0 in enumerate(freqs):
        phase = 2 * np.pi * np.cumsum(f0 * ratio) / RATE
        y = sum(np.sin(h * phase + rng.uniform(0, 2 * np.pi)) / h ** rolloff for h in range(1, harmonics + 1) if f0 * h < dsp.CEILING_HZ)
        out += pan(y / max(1e-9, np.abs(y).max()), -0.5 + i / max(1, len(freqs) - 1)) / len(freqs)
    out *= np.clip(t / attack, 0, 1)[:, None]
    return out + lp(hp(base.noise(n, rng), 300), 2200) * breath * np.clip(t / attack, 0, 1)[:, None]


# The Choir's own soft gong (Pale Meridian's soft_gong and Last Witness's brass_gong are separate blocks): (ratio,
# level, decay factor) per mode, each mode a slowly beating pair. Low and long; the upper modes bloom a moment after
# the felt-mallet strike, as a struck gong's sound spreads upward.
CHOIR_GONG_MODES = ((1.0, 0.55, 1.0), (1.48, 0.45, 0.85), (2.02, 0.6, 0.72), (2.71, 0.52, 0.6), (3.42, 0.46, 0.52),
                    (4.18, 0.4, 0.44), (5.37, 0.32, 0.36), (6.62, 0.25, 0.3), (8.13, 0.18, 0.24), (9.94, 0.12, 0.19),
                    (12.31, 0.08, 0.15))


def choir_gong(freq, dur, rng, tau=1.5, mallet=0.014, beat=0.3, bloom=0.16):
    """A soft gong-like resonance on `freq`: inharmonic modes, each a pair `beat` Hz apart panned apart, a felt
    mallet onset and decays of `tau` x the mode's factor; modes from the third bloom in over `bloom` s."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for m, (ratio, level, decay) in enumerate(CHOIR_GONG_MODES):
        f = freq * ratio
        if f * 1.01 >= dsp.CEILING_HZ:
            break
        env = np.exp(-t / (tau * decay)) * (1 - np.exp(-t / bloom) if m >= 2 else 1)
        for side, shift in ((-0.35, -beat), (0.35, beat)):
            y = np.sin(2 * np.pi * (f + shift * ratio ** 0.5) * t + rng.uniform(0, 2 * np.pi)) * env * level * 0.5
            out += pan(y, side)
    out *= (0.5 - 0.5 * np.cos(np.pi * np.clip(t / mallet, 0, 1)))[:, None]
    out += lp(base.noise(n, rng), 900) * (np.exp(-t / 0.03) * 0.04)[:, None]   # the felt touching the metal
    return out / max(1e-9, np.abs(out).max())


def tap(rng, ring, gain=0.0):
    """The porcelain baton tapping the brass music stand: a dry tick, a brass ping and a porcelain ring."""
    x = np.zeros((round(0.22 * RATE), 2))
    place(x, pan(dsp.brass_click(rng, 3300, decay=0.0028, tick=0.9, thud=0.15), 0.1), 0.0, gain)
    place(x, dsp.porcelain_ring(dsp.hz(ring), 0.2, rng, decay=0.045, side=0.15), 0.002, gain - 5)
    return x


def sung_note(index, rng):
    """One verse note: the doll's "ah" with a porcelain onset and a quiet music-box tine an octave up."""
    f = dsp.hz(CHOIR_SUNG[index])
    mix = seconds(0.62)
    place(mix, doll_voice(f, 0.5, rng, side=-0.15 + 0.04 * index), 0.0, -2)
    place(mix, porcelain_onset(f, rng), 0.0, -17)
    place(mix, pan(dsp.box_tine(f * 2, 0.4, rng, body=0.2), 0.2), 0.004, -19)
    return room(mix, 0.16, 0.6, 0.12)


def pipe_pip(rank, rng):
    """A raised pipe seats: a brass click and one short organ pip (chiff and speech)."""
    mix = seconds(0.4)
    place(mix, pan(dsp.brass_click(rng, 2500, decay=0.004, thud=0.5), -0.1 + 0.05 * rank), 0.0, -9)
    pip = dsp.organ_pad([dsp.hz(CHOIR_PIPES[rank])], 0.26, rng, attack=0.012, release=0.12, harmonics=10, rolloff=1.15,
                        chiff=0.16, breath=0.05)
    place(mix, pip, 0.008, -3)
    return room(mix, 0.18, 0.38, 0.1)


def fall_away(n, start, offset=0.0):
    """A per-sample gain for a part placed at `offset` s: 1 until `start` s, then a half-cosine fall of CHORUS_FALL dB
    to the cut."""
    t = offset + np.arange(n) / RATE
    k = np.clip((t - start) / (CHORUS_CUT - start), 0, 1)
    return 10 ** (-CHORUS_FALL * (0.5 - 0.5 * np.cos(np.pi * k)) / 20)


def chorus(voices, rng):
    """The chorus: organ and n doll lines through Bb7sus -> Fm9 -> the open fifth, a swell and music-box glints on
    every beat; the open fifth falls away into the cut at 3.0 s and the hall, where ChoirChorusEnd takes over."""
    length = CHORUS_CUT + 1.2
    mix = seconds(length)
    # Organ: pedal and manual per chord, crossfading at the changes; the last chord is cut with everyone.
    for k, ((pedal_a, pedal_b), manual) in enumerate(CHORUS_ORGAN):
        start = CHORUS_CHANGES[k]
        end = CHORUS_CHANGES[k + 1] + 0.06 if k + 1 < len(CHORUS_CHANGES) else CHORUS_CUT
        seg = end - start
        pedal = dsp.organ_pad([dsp.hz(pedal_a), dsp.hz(pedal_b)], seg, rng, attack=0.05 if k else 0.012, release=0.07,
                              harmonics=12, rolloff=1.2, chiff=0.02 if k else 0.08, breath=0.07, spread=0.2)
        chord = dsp.organ_pad([dsp.hz(n) for n in manual], seg, rng, attack=0.05 if k else 0.015, release=0.07,
                              harmonics=9, rolloff=1.35, chiff=0.02 if k else 0.1, breath=0.05, spread=0.7)
        if k == len(CHORUS_ORGAN) - 1:
            # The open fifth is sung, not blasted: it falls away into the cut.
            fall = fall_away(len(pedal), start, offset=start)[:, None]
            pedal, chord = pedal * fall, chord * fall
        place(mix, pedal, start, -2 + 0.4 * voices)
        place(mix, chord, start, -4 + 0.3 * voices)
    # The doll lines, one per voice (up to six), each its own pan, slightly late and slightly detuned.
    for i in range(min(6, voices)):
        notes = CHORUS_LINES[i]
        contour = glide_contour([(CHORUS_CHANGES[k] + 0.012 * i, dsp.hz(notes[k], cents=(-3, 2, -1, 3, -2, 1)[i])) for k in range(3)],
                                CHORUS_CUT)
        side = (0.0, -0.45, 0.45, -0.75, 0.75, -0.2)[i]
        line = doll_voice(contour, CHORUS_CUT, rng, attack=0.035, release=0.1, vibrato=0.0045, breath=0.07, side=side)
        line *= fall_away(len(line), CHORUS_CHANGES[2])[:, None]
        place(mix, line, 0.0, (-3, -6, -7, -8, -9, -9)[i] - 1.2 * np.log2(max(1, voices)))
    # Every beat: a short swell into it and music-box glints on the chord's tones.
    glints = (("Bb5", "Eb6", "F6"), ("C6", "Ab6", "F6"), ("C6", "F6", "C7"))
    for beat in range(5):
        at = beat * CHOIR_BEAT
        chord = 0 if at < 1.2 else 1 if at < 2.4 else 2
        for j, note in enumerate(glints[chord][: 1 + min(2, voices // 2)]):
            place(mix, pan(dsp.box_tine(dsp.hz(note), 0.5, rng, body=0.15), -0.4 + 0.4 * j), at + 0.006 * j,
                  -16 - 2 * j - (4 if chord == 2 else 0))
        place(mix, dsp.shimmer(0.35, rng, count=4 + beat), at, -24)
    place(mix, dsp.thump(110, 46, 0.5, rng), 0.0, -10)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.4, rng, decay=0.08, side=0.25), 0.0, -16)
    head = fades(mix[:round(CHORUS_CUT * RATE)].copy(), 0.0005, 0.14)
    out = seconds(length)
    out[:len(head)] = head
    return room(out, 0.34, length, 0.4)


@cue("ChoirSummon", -13, "Choir", 0.85, 0.7,
     "未成の聖歌隊員を1体呼ぶ（アイテム使用ごと）。磁器のチャイムが C6→F6 と2つ鳴り、真鍮の小さなクリックのあと、"
     "顔のない人形が「んー」と F5 から C6 へハミングして、短いきらめきで終わる。")
def choir_summon(s, rng):
    mix = seconds(1.0)
    place(mix, dsp.porcelain_ring(dsp.hz("C6"), 0.4, rng, decay=0.07, side=-0.3), 0.0, -6)
    place(mix, dsp.porcelain_ring(dsp.hz("F6"), 0.4, rng, decay=0.08, side=0.3), 0.085, -5)
    place(mix, pan(dsp.brass_click(rng, 2200, decay=0.005, thud=0.4), 0.0), 0.02, -12)
    hum = doll_voice(glide_contour([(0.0, dsp.hz("F5")), (0.24, dsp.hz("C6"))], 0.55, glide=0.09), 0.55, rng, vowel=VOWEL_MM,
                     attack=0.06, release=0.2, breath=0.03)
    place(mix, hum, 0.13, -4)
    place(mix, dsp.shimmer(0.4, rng, count=7), 0.36, -18)
    return room(mix, 0.2, 0.84, 0.18)


@cue("ChoirVerseWarn", -17, "Choir", 1.3, 0.8,
     "歌の予兆（カウントイン、拍0と1）。磁器の指揮棒が真鍮の譜面台を2回たたき（C6、F6 の小さな響き）、"
     "1.18 秒の最初の音符の直前に人形たちが息を吸う。")
def choir_verse_warn(s, rng):
    mix = seconds(1.3)
    place(mix, tap(rng, "C6"), 0.0, -1)
    place(mix, tap(rng, "F6"), (CHOIR_TAPS[1] - CHOIR_TAPS[0]) * CHOIR_TICK, 0)
    first = (CHOIR_VERSE - CHOIR_TAPS[0]) * CHOIR_TICK
    place(mix, inhale(0.34, rng, low=900), first - 0.34, -15)
    return room(mix, 0.14, 1.28, 0.1)


@cue("ChoirVerseFire0", -17, "Choir", 0.6, 0.7, "歌の音符（F4）。顔のない人形の「あー」に、磁器の澄んだ立ち上がりとオクターブ上のオルゴール。")
def choir_verse_fire0(s, rng):
    return sung_note(0, rng)


@cue("ChoirVerseFire1", -17, "Choir", 0.6, 0.7, "歌の音符（A♭4）。")
def choir_verse_fire1(s, rng):
    return sung_note(1, rng)


@cue("ChoirVerseFire2", -17, "Choir", 0.6, 0.7, "歌の音符（B♭4）。")
def choir_verse_fire2(s, rng):
    return sung_note(2, rng)


@cue("ChoirVerseFire3", -17, "Choir", 0.6, 0.7, "歌の音符（C5）。")
def choir_verse_fire3(s, rng):
    return sung_note(3, rng)


@cue("ChoirVerseFire4", -17, "Choir", 0.6, 0.7, "歌の音符（E♭5）。")
def choir_verse_fire4(s, rng):
    return sung_note(4, rng)


@cue("ChoirVerseFire5", -17, "Choir", 0.6, 0.7, "歌の音符（F5）。")
def choir_verse_fire5(s, rng):
    return sung_note(5, rng)


@cue("ChoirVerseFire6", -17, "Choir", 0.6, 0.7, "歌の音符（A♭5）。")
def choir_verse_fire6(s, rng):
    return sung_note(6, rng)


@cue("ChoirVerseFire7", -17, "Choir", 0.6, 0.7, "歌の音符（B♭5）。")
def choir_verse_fire7(s, rng):
    return sung_note(7, rng)


@cue("ChoirVerseFire8", -17, "Choir", 0.6, 0.7, "歌の音符（C6、主旋律の最高音）。")
def choir_verse_fire8(s, rng):
    return sung_note(8, rng)


@cue("ChoirOrganRise", -13, "Choir", 1.0, 0.75,
     "パイプオルガンの登場（拍8）。真鍮の歯車がだんだん速く巻かれ、0.14 秒に木のケースが鳴り、F の足鍵盤が膨らんでくる。")
def choir_organ_rise(s, rng):
    mix = seconds(1.05)
    gaps = np.linspace(0.075, 0.024, 11)
    times = tuple(np.concatenate(([0.0], np.cumsum(gaps)))[:11])
    place(mix, dsp.ratchet(times, rng, freq=2300, gains_db=tuple(np.linspace(-6, 0, len(times))), side=-0.2), 0.0, -7)
    knock = dsp.thump(150, 70, 0.3, rng)
    place(mix, knock, 0.14, -6)
    place(mix, dsp.porcelain_ring(dsp.hz("F5"), 0.2, rng, decay=0.04, side=0.2), 0.14, -18)
    pedal = dsp.organ_pad([dsp.hz("F1"), dsp.hz("F2"), dsp.hz("C3")], 0.8, rng, attack=0.55, release=0.12, harmonics=12,
                          rolloff=1.2, chiff=0.03, breath=0.2, spread=0.3)
    place(mix, pedal, 0.2, -4)
    return room(mix, 0.22, 1.0, 0.1)


@cue("ChoirPipe0", -17, "Choir", 0.4, 0.6, "パイプ（中央）がせり上がって収まる。真鍮のカチッと短いオルガンの一声（F3）。")
def choir_pipe0(s, rng):
    return pipe_pip(0, rng)


@cue("ChoirPipe1", -17, "Choir", 0.4, 0.6, "パイプ（1組目）が収まる（C4）。")
def choir_pipe1(s, rng):
    return pipe_pip(1, rng)


@cue("ChoirPipe2", -17, "Choir", 0.4, 0.6, "パイプ（2組目）が収まる（F4）。")
def choir_pipe2(s, rng):
    return pipe_pip(2, rng)


@cue("ChoirPipe3", -17, "Choir", 0.4, 0.6, "パイプ（3組目）が収まる（A♭4）。")
def choir_pipe3(s, rng):
    return pipe_pip(3, rng)


@cue("ChoirPipe4", -17, "Choir", 0.4, 0.6, "パイプ（4組目）が収まる（C5）。")
def choir_pipe4(s, rng):
    return pipe_pip(4, rng)


@cue("ChoirPipe5", -17, "Choir", 0.4, 0.6, "パイプ（いちばん外側）が収まる（E♭5）。")
def choir_pipe5(s, rng):
    return pipe_pip(5, rng)


@cue("ChoirChorusWarn", -13, "Choir", 0.7, 0.85,
     "合唱の予兆（拍10、光線は無害な細い軸だけ）。送風機の風が高まり、人形たちが「はー」と息を吸い込み、"
     "B♭ の低いペダルが下で膨らんで、0.55 秒にストップを引く音で締まる。直後に合唱が鳴る。")
def choir_chorus_warn(s, rng):
    mix = seconds(0.7)
    n = round(0.6 * RATE)
    t = np.arange(n) / RATE
    air = base.noise(n, rng)
    cutoff = 400 + 2600 * (t / 0.6) ** 1.6
    blower = np.zeros_like(air)
    for k in range(0, n, 2205):
        seg = slice(k, min(n, k + 2205))
        blower[seg] = lp(hp(air[seg], 180), float(cutoff[k]))
    place(mix, blower * ((t / 0.6) ** 1.4)[:, None], 0.0, -14)
    place(mix, inhale(0.58, rng, low=650, curve=1.2), 0.0, -12)
    pedal = dsp.organ_pad([dsp.hz("Bb1"), dsp.hz("F2")], 0.6, rng, attack=0.45, release=0.05, harmonics=10, chiff=0.0, breath=0.1)
    place(mix, pedal, 0.0, -9)
    place(mix, pan(dsp.brass_click(rng, 1800, decay=0.006, thud=0.8), 0.15), 0.55, -6)
    place(mix, dsp.thump(140, 80, 0.1, rng), 0.55, -16)
    return room(mix, 0.12, 0.68, 0.04)


def _chorus_description(voices):
    lines = ("主旋律", "アルト", "第3声", "第4声", "第5声（Fm9 の9度 G）", "バス")
    return (f"合唱の光線（拍11〜15、声 {voices} 本）。オルガンと人形の合唱が B♭7sus → Fm9 → F の空虚五度と進み、"
            f"拍ごとにふくらみとオルゴールのきらめき。最後の空虚五度は鳴らし切らずに引いていき、3.0 秒で切れて残響へ"
            f"（締めは ChoirChorusEnd）。重なる声: {'、'.join(lines[:voices])}。")


@cue("ChoirChorusFire1", -11.5, "Choir", 4.3, 0.9, _chorus_description(1))
def choir_chorus_fire1(s, rng):
    return chorus(1, rng)


@cue("ChoirChorusFire2", -11.5, "Choir", 4.3, 0.9, _chorus_description(2))
def choir_chorus_fire2(s, rng):
    return chorus(2, rng)


@cue("ChoirChorusFire3", -10, "Choir", 4.3, 0.9, _chorus_description(3))
def choir_chorus_fire3(s, rng):
    return chorus(3, rng)


@cue("ChoirChorusFire4", -10, "Choir", 4.3, 0.9, _chorus_description(4))
def choir_chorus_fire4(s, rng):
    return chorus(4, rng)


@cue("ChoirChorusFire5", -10, "Choir", 4.3, 0.9, _chorus_description(5))
def choir_chorus_fire5(s, rng):
    return chorus(5, rng)


@cue("ChoirChorusFire6", -10, "Choir", 4.3, 0.9, _chorus_description(6))
def choir_chorus_fire6(s, rng):
    return chorus(6, rng)


@cue("ChoirChorusEnd", -13, "Choir", 3.4, 0.7,
     "合唱の締め（拍16、成功。合唱の途中で標的を倒したときも）。合唱の F の空虚五度が引いたところへ、低い F の柔らかい"
     "ゴングのような響きが入り、静かなオルガン（F と C）がその下で鳴って、長い余韻で消えていく。真鍮の歯車がゆっくり"
     "戻る音（パイプが沈む）はごく小さく残す。オルガンを鳴らし切らず、抑えて余韻で締める版。")
def choir_chorus_end(s, rng):
    length = 3.3
    mix = seconds(length)
    # The soft gong on low F, the ending's weight: struck with a felt mallet, long and slowly beating.
    place(mix, choir_gong(dsp.hz("F2"), 3.25, rng), 0.0, -2)
    # The quiet organ: the open fifth the chorus fell away from, now low and soft, swelling in under the strike and
    # dying with the gong.
    pad = dsp.organ_pad([dsp.hz("F2"), dsp.hz("C3"), dsp.hz("F3"), dsp.hz("C4")], 2.7, rng, attack=0.22, release=2.1,
                        harmonics=7, rolloff=1.7, chiff=0.0, breath=0.04, spread=0.5)
    place(mix, pad, 0.03, -10)
    # The pipes sinking: a few slow brass gear teeth, far back.
    gaps = np.linspace(0.12, 0.2, 5)
    times = tuple(0.35 + np.concatenate(([0.0], np.cumsum(gaps)))[:5])
    place(mix, dsp.ratchet(times, rng, freq=1500, gains_db=tuple(np.linspace(0, -8, len(times))), side=0.2), 0.0, -24)
    # The dolls close their mouths: one faint porcelain C6 on the strike.
    place(mix, dsp.porcelain_ring(dsp.hz("C6"), 0.5, rng, decay=0.09, side=-0.2), 0.004, -22)
    return room(mix, 0.3, length, 0.9)


@cue("ChoirChorusMiss", -13, "Choir", 0.7, 0.8,
     "合唱の失敗（標的を失った、動けなくなった、隊員がいなくなった）。オルガンの和音の風が抜けて音程が崩れ落ち、"
     "磁器にひびが入り、ため息のような空気で終わる。成功の終わり（ChoirChorusEnd）とは別の音。")
def choir_chorus_miss(s, rng):
    mix = seconds(0.7)
    n = round(0.42 * RATE)
    t = np.arange(n) / RATE
    cents = -320 * (t / 0.42) ** 1.6
    chord = organ_glide([dsp.hz("F2"), dsp.hz("C3"), dsp.hz("F3"), dsp.hz("Ab3")], 0.42, rng, cents)
    chord *= (np.exp(-t / 0.16))[:, None]
    place(mix, chord, 0.0, -2)
    place(mix, dsp.porcelain_crack(0.25, rng, count=9), 0.05, -7)
    m = round(0.45 * RATE)
    u = np.arange(m) / RATE
    sigh = lp(hp(base.noise(m, rng), 200), 1500) * (np.sin(np.pi * np.clip(u / 0.45, 0, 1)) ** 2)[:, None]
    place(mix, sigh, 0.18, -15)
    return room(mix, 0.15, 0.68, 0.12)


@cue("ChoirNoteHit", -20, "Choir", 0.3, 0.6, "音符が当たる。高い磁器の「チン」と小さなひびの尾。")
def choir_note_hit(s, rng):
    mix = seconds(0.3)
    place(mix, dsp.porcelain_ring(dsp.hz("C7"), 0.2, rng, decay=0.04, side=0.1), 0.0, -2)
    place(mix, dsp.porcelain_crack(0.12, rng, count=4, spread=0.025, low=3000, high=7500), 0.012, -12)
    return room(mix, 0.1, 0.28, 0.06)


@cue("ChoirChorusHit", -20, "Choir", 0.35, 0.55, "合唱の光線が当たる（持ち主だけ、24 tick に1回まで）。低い真鍮の「ボーン」と磁器のざらつき。")
def choir_chorus_hit(s, rng):
    mix = seconds(0.35)
    n = round(0.32 * RATE)
    t = np.arange(n) / RATE
    f = dsp.hz("F2")
    bong = sum(a * np.sin(2 * np.pi * f * r * t + rng.uniform(0, 6.28)) * np.exp(-t / (0.18 / r ** 0.6))
               for r, a in ((1, 1.0), (2.0, 0.5), (2.76, 0.32), (5.4, 0.12)))
    place(mix, pan(bong * np.clip(t / 0.002, 0, 1), 0.0), 0.0, -2)
    place(mix, dsp.porcelain_crack(0.1, rng, count=5, spread=0.03, low=2500, high=6000), 0.004, -14)
    return room(mix, 0.12, 0.34, 0.06)


def choir_concert(samples, voices, cancel=None, finish=None):
    """The whole concert at its in-game levels: count-in, verse (voices sharing a part sing one louder cue), organ,
    raised pipes, inhale, the chorus for this many voices and the release; or, with `cancel` (a concert tick), the
    failure cue there with the chorus fading over CHORUS_CANCEL_FADE ticks; or, with `finish` (the target died),
    the success close there with the chorus fading over CHORUS_FINISH_FADE ticks. Returns (mix, [(seconds, cue)])."""
    events = [(CHOIR_TAPS[0], "ChoirVerseWarn", CUES["ChoirVerseWarn"].volume)]
    for beat in range(6):
        for part in range(4):
            k = sum(1 for o in range(voices) if o & 3 == part)
            if k:
                name = f"ChoirVerseFire{CHOIR_LINES[part][beat]}"
                events.append((CHOIR_VERSE + 36 * beat + 9 * part, name, CUES[name].volume * min(1.5, 1 + 0.25 * np.log2(k))))
    events.append((CHOIR_GATHER, "ChoirOrganRise", CUES["ChoirOrganRise"].volume))
    for rank in range(choir_raised(voices)):
        events.append((choir_pipe_seat(rank), f"ChoirPipe{rank}", CUES[f"ChoirPipe{rank}"].volume))
    events.append((CHOIR_INHALE, "ChoirChorusWarn", CUES["ChoirChorusWarn"].volume))
    fire = f"ChoirChorusFire{min(6, voices)}"
    events.append((CHOIR_FIRE, fire, CUES[fire].volume))
    events.append((CHOIR_RELEASE, "ChoirChorusEnd", CUES["ChoirChorusEnd"].volume))
    stop, fade = (cancel, CHORUS_CANCEL_FADE) if cancel is not None else (finish, CHORUS_FINISH_FADE)
    if stop is not None:
        close = "ChoirChorusMiss" if cancel is not None else "ChoirChorusEnd"
        events = [e for e in events if e[0] < stop] + [(stop, close, CUES[close].volume)]
    end = max(at * CHOIR_TICK + len(samples[name]) / RATE for at, name, _ in events)
    mix = seconds(end + 0.1)
    for at, name, volume in events:
        x = samples[name] * volume
        if stop is not None and name.startswith("ChoirChorusFire"):
            keep = round((stop - at) * CHOIR_TICK * RATE)
            x = x[:max(1, keep + round(fade * CHOIR_TICK * RATE))].copy()
            x[keep:] *= np.linspace(1, 0, len(x) - keep)[:, None]
        place(mix, x, at * CHOIR_TICK)
    return mix, [(round(at * CHOIR_TICK, 3), name) for at, name, _ in events]


# ---------------------------------------------------------------- Lacrimosa's Claws (2026-10-03)
# The claw cues follow the gameplay clock (LacrimosaClawMotion, LacrimosaClawVisuals): a firing cue starts `lead`
# ticks before its event so its main transient lands on it. Rake fires peak 3 ticks in, on the first live tick; the
# clap, grasp and crush peak 1 tick in. ClawCrushWarn starts at grasp age 18 and carries the four squeezes at their
# beat ticks, ending just before the crush. Materials: brass pawls and key, porcelain fingertips and knuckles, cut
# air, the music-box comb on the F minor pentatonic ladder and the flue organ; no runtime transposition except the
# single-note ClawBead (recorded at F5, played up the ladder for beads 1-6). The crush, the weapon's closing release,
# is restrained (the owner's choice for finishing sounds, 2026-10-03): the shatter and the low weight land the hit,
# then a soft F gong and a quiet organ carry a long decaying tail instead of an organ chord played out loud.
CLAW_RAKE_LEAD = 3 * TICK
CLAW_STRIKE_LEAD = 1 * TICK
CLAW_SQUEEZES = (22, 28, 33, 36)    # LacrimosaClawMotion.SqueezeBeats
CLAW_WARN_FROM, CLAW_CRUSH = 18, 40  # GraspContactEnd (CrushWarn starts), GraspCrush


def taper(x, fade_out, fade_in=0.0005):
    return reward.taper(x, fade_out, fade_in)


def rake_warn(rng, rising):
    """Three brass pawl teeth as the hand coils (rising for the down-rake's lift, falling for the up-rake's dip),
    a porcelain knuckle tick and a short inhale of air."""
    mix = seconds(0.26)
    steps = (1.0, 1.06, 1.12) if rising else (1.12, 1.06, 1.0)
    side = -0.25 if rising else 0.25
    for i, at in enumerate((0.0, 0.032, 0.058)):
        place(mix, pan(dsp.brass_click(rng, 2300 * steps[i], decay=0.004, tick=0.6, thud=0.15), side), at, -3 - i)
    place(mix, dsp.porcelain_ring(dsp.hz("C7" if rising else "Ab6"), 0.08, rng, decay=0.025, side=-side), 0.085, -9)
    air = reward.sweep(0.16, rng, 900, 3200 if rising else 2200, shape=lambda t: np.sin(np.pi * np.clip(t / 0.16, 0, 1)) ** 2)
    place(mix, air, 0.02, -15)
    place(mix, dsp.shimmer(0.16, rng, count=4, air=0.05), 0.05, -24)
    return taper(mix[:round(0.2 * RATE)], 0.04)


def rake_fire(s, rng, down):
    """Cut air cresting on the first live tick, a tear swept down (A) or up (B) through the 8-tick live window,
    three talon tinks stepping C7 Bb6 Ab6 an octave up and the hand's short weight. No delayed second note."""
    mix = seconds(0.42)
    peak = CLAW_RAKE_LEAD
    if down:  # air_cut crests 0.14 s into its file: cut 0.09 s in and sped up, it crests about 0.045 s into the cue
        lay(mix, s, "air_cut", 0.09, 0.40, 0.0, -3, rate=1.12, hp_=500, side=0.25, fade_in=0.004, fade_out=0.08)
    else:  # stick_woosh crests 0.14 s in
        lay(mix, s, "stick_woosh", 0.095, 0.44, 0.0, -3, rate=1.08, hp_=450, side=-0.25, fade_in=0.004, fade_out=0.08)
    tear = reward.sweep(0.17, rng, 6200 if down else 1400, 1400 if down else 6200,
                        shape=lambda t: np.clip(t / 0.012, 0, 1) * np.exp(-t / 0.07))
    place(mix, tear, peak - 0.012, -7)
    notes = ("C7", "Bb6", "Ab6") if down else ("Ab6", "Bb6", "C7")
    for i, note in enumerate(notes):
        place(mix, dsp.porcelain_ring(dsp.hz(note) * 2, 0.06, rng, decay=0.012, side=(0.4 - 0.3 * i) * (1 if down else -1)),
              peak + 0.004 + 0.009 * i, -12 - i)
    place(mix, dsp.thump(140, 62, 0.16, rng), peak, -13)
    place(mix, pan(dsp.brass_click(rng, 1700, decay=0.006, thud=0.4), 0.0), peak + 0.002, -14)
    return mix


@cue("ClawRakeDownWarn", -17, "Claws", 0.25, 0.6,
     "A（右手の振り下ろし）の予備動作。手が背中側へ巻き上がるあいだに真鍮の爪車が3回、上がっていく音程で鳴り、"
     "磁器の指関節のコツッと短い吸気が続く。")
def claw_rake_down_warn(s, rng):
    return rake_warn(rng, True)


@cue("ClawRakeDownFire", -11.5, "Claws", 0.4, 0.7,
     "A の振り下ろし。最初の有効フレームに頂点が来る鋭い風切り（旧爪の受け入れられた切れ味を継承）に、"
     "上から下へ掃く裂ける音、指先の磁器が C7→B♭6→A♭6 と3つ、手の重さの短い低音。後から鳴る二音目はない。", glue=3.2)
def claw_rake_down_fire(s, rng):
    return rake_fire(s, rng, True)


@cue("ClawRakeUpWarn", -17, "Claws", 0.25, 0.6,
     "B（左手の振り上げ）の予備動作。手が低く背後へ沈むあいだに爪車が下がっていく音程で3回、指関節と吸気。")
def claw_rake_up_warn(s, rng):
    return rake_warn(rng, False)


@cue("ClawRakeUpFire", -11.5, "Claws", 0.4, 0.7,
     "B の振り上げ。別の風切り素材に、下から上へ掃く裂ける音、指先が A♭6→B♭6→C7 と上がる。A と対になる鏡写しの音。", glue=3.2)
def claw_rake_up_fire(s, rng):
    return rake_fire(s, rng, False)


@cue("ClawClapWarn", -13, "Claws", 0.38, 0.8,
     "C（両手の打ち合わせ）の予備動作。両手が大きく開いて構えるあいだ、オルガンの鞴が息を吸い込むように"
     "空気と F の5度が膨らみ、真鍮の蝶番が軋む。打ち合わせの直前で切れる。")
def claw_clap_warn(s, rng):
    length = 0.34
    mix = seconds(length + 0.05)
    place(mix, reward.sweep(length, rng, 380, 2600, shape=lambda t: (np.clip(t, 0, length) / length) ** 1.6), 0.0, -6)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4")], length, rng, attack=length * 0.95, release=0.02, harmonics=7,
                        rolloff=1.6, chiff=0.0, breath=0.3)
    place(mix, pad, 0.0, -9)
    place(mix, dsp.ratchet((0.04, 0.09, 0.13, 0.16, 0.185), rng, freq=1500, gains_db=(-6, -5, -4, -3, -2), decay=0.006, thud=0.3), 0.0, -10)
    return taper(mix[:round(length * RATE)], 0.02)


@cue("ClawClapFire", -11.5, "Claws", 0.65, 0.95,
     "C の打ち合わせ。両掌が噛み合う瞬間に磁器の平手打ちと高い磁器の響き、オルガンの短い F の和音（F3 C4 F4 C5）の一撃、"
     "低い胴鳴りと真鍮の余韻。", glue=3.2)
def claw_clap_fire(s, rng):
    mix = seconds(0.7)
    hit = CLAW_STRIKE_LEAD
    place(mix, dsp.porcelain_crack(0.12, rng, count=5, spread=0.012, low=2600, high=5200, decay=(0.01, 0.03)), hit, -4)
    place(mix, dsp.porcelain_ring(3100, 0.25, rng, decay=0.05, side=-0.3), hit, -8)
    place(mix, dsp.porcelain_ring(4200, 0.2, rng, decay=0.04, side=0.3), hit + 0.002, -9)
    lay(mix, s, "chop", 0.035, 0.2, hit - 0.012, -7, hp_=300, fade_in=0.001)  # chop's body peaks 0.05 s in
    stab = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "F4", "C5")], 0.3, rng, attack=0.012, release=0.16, harmonics=9,
                         rolloff=1.3, chiff=0.06, breath=0.1)
    place(mix, stab, hit, -7)
    place(mix, dsp.thump(120, 48, 0.3, rng), hit, -6)
    place(mix, pan(dsp.brass_click(rng, 1250, decay=0.03, dur=0.25, thud=0.2), 0.0), hit + 0.004, -12)
    return room(mix, 0.12, 0.6, 0.12)


@cue("ClawHit", -20, "Claws", 0.18, 0.9,
     "爪が当たったとき（その一撃で最初の接触）。磁器の小さな欠けるチッという音と、こもった低い当たり。強い一撃の音より控えめ。")
def claw_hit(s, rng):
    mix = seconds(0.2)
    place(mix, dsp.porcelain_crack(0.1, rng, count=3, spread=0.006, low=3000, high=6500, decay=(0.006, 0.018)), 0.0, -3)
    place(mix, lp(dsp.thump(180, 80, 0.1, rng), 900), 0.0, -6)
    return mix


@cue("ClawBead", -20, "Claws", 0.75, 0.85,
     "手の甲の心臓玉がひとつ灯る（持ち主にだけ聞こえる）。F5 で録ったオルゴールの一音で、ゲームでは1つ目から6つ目まで "
     "F5 A♭5 B♭5 C6 E♭6 F6 と梯子を上る。ここでは F5 のまま。")
def claw_bead(s, rng):
    mix = seconds(0.7)
    place(mix, dsp.box_tine(dsp.hz("F5"), 0.65, rng), 0.0, 0)
    place(mix, pan(dsp.brass_click(rng, 3100, decay=0.002, dur=0.03, tick=0.4, thud=0.05), 0.2), 0.0, -14)
    return mix


@cue("ClawBeadsFull", -13, "Claws", 0.95, 0.8,
     "6つの玉がすべて灯った（持ち主のみ、6つ目の音の少し後）。ぜんまいの鍵が2回巻かれ、オルゴールが C6→F6 と終止し、"
     "A♭6 と C7 がかすかに重なる。下で F マイナーのオルガンが柔らかく膨らむ。右クリックが使える合図。")
def claw_beads_full(s, rng):
    mix = seconds(1.0)
    place(mix, dsp.ratchet((0.0, 0.07), rng, freq=2300, gains_db=(-2, 0), side=-0.2), 0.0, -8)
    lay(mix, s, "metal_click", 0.2705, 0.34, 0.0, -11, hp_=1400, side=-0.2, fade_in=0.0005, fade_out=0.03)
    place(mix, pan(dsp.box_tine(dsp.hz("C6"), 0.7, rng), -0.1), 0.12, -5)
    place(mix, pan(dsp.box_tine(dsp.hz("F6"), 0.8, rng), 0.15), 0.24, -2)
    place(mix, pan(dsp.box_tine(dsp.hz("Ab6"), 0.6, rng), -0.3), 0.244, -11)
    place(mix, pan(dsp.box_tine(dsp.hz("C7"), 0.5, rng), 0.35), 0.248, -12)
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F3", "C4", "Ab4")], 0.75, rng, attack=0.3, release=0.35, harmonics=7, rolloff=1.5,
                        chiff=0.02, breath=0.2)
    place(mix, pad, 0.1, -16)
    place(mix, dsp.shimmer(0.4, rng, count=6), 0.3, -20)
    return room(mix, 0.12, 0.9, 0.25)


@cue("ClawBeadDry", -20, "Claws", 0.15, 0.8,
     "玉が6つそろう前に右クリックしたとき（持ち主のみ）。くぐもった真鍮のコトッと、こもった短い一音。何も起きないことが分かる音。")
def claw_bead_dry(s, rng):
    mix = seconds(0.16)
    place(mix, pan(dsp.brass_click(rng, 900, decay=0.003, dur=0.05, tick=0.3, thud=0.6), 0.0), 0.0, -2)
    place(mix, lp(dsp.box_tine(dsp.hz("F5"), 0.12, rng, decay=0.025, body=0.8), 1200), 0.004, -7)
    return mix


@cue("ClawGraspWarn", -13, "Claws", 0.32, 0.9,
     "右クリックで掴みに行く（予告）。6つの玉が放電するようにオルゴールが F6 E♭6 C6 B♭5 A♭5 F5 と一気に駆け下り、"
     "ぜんまいがほどける加速するカラカラと、両手が飛ぶ風が到着の瞬間まで上がっていく。")
def claw_grasp_warn(s, rng):
    mix = seconds(0.36)
    for i, note in enumerate(("F6", "Eb6", "C6", "Bb5", "Ab5", "F5")):
        place(mix, pan(dsp.box_tine(dsp.hz(note), 0.3, rng, decay=0.12, body=0.2), 0.3 - 0.12 * i), 0.012 * i, -6 - 0.4 * i)
    whirr = reward.ratchet(0.2, rng, 35, 140, low=1800, high=6500)
    place(mix, whirr * np.linspace(0.3, 1, len(whirr))[:, None], 0.06, -12)
    place(mix, reward.sweep(0.2, rng, 700, 4200, shape=lambda t: np.sin(np.pi * np.clip(t / 0.2, 0, 1)) ** 1.5), 0.04, -10)
    return taper(mix[:round(0.3 * RATE)], 0.05)


@cue("ClawGraspFire", -11.5, "Claws", 0.45, 0.95,
     "両手が敵を掴んだ（発動）。磁器の指が5つ重なって当たるカカカッと、重い真鍮の掛け金のガチャン、低い当たり。", glue=3.2)
def claw_grasp_fire(s, rng):
    mix = seconds(0.45)
    hit = CLAW_STRIKE_LEAD
    for i in range(5):
        place(mix, dsp.porcelain_ring(rng.uniform(2400, 4200), 0.08, rng, decay=0.018, side=rng.uniform(-0.5, 0.5)), hit + 0.0035 * i, -6 - i)
    lay(mix, s, "metal_latch", 0.035, 0.24, hit - 0.005, -3, rate=0.92, hp_=250, fade_in=0.0005, fade_out=0.08)  # catch 0.04 s in
    place(mix, pan(dsp.brass_click(rng, 1400, decay=0.01, dur=0.12, thud=0.9), 0.0), hit, -7)
    place(mix, dsp.thump(110, 45, 0.28, rng), hit, -5)
    return room(mix, 0.1, 0.42, 0.1)


@cue("ClawGraspMiss", -13, "Claws", 0.4, 0.9,
     "何もない所を掴んだ（失敗）。掛け金のないうつろな磁器の打ち合わせと空気の破裂、くぐもったオルゴールが A♭5→F5 と落ちる。"
     "成功の音とははっきり別物。")
def claw_grasp_miss(s, rng):
    mix = seconds(0.4)
    hit = CLAW_STRIKE_LEAD
    n = round(0.06 * RATE)
    puff = base.bp(base.noise(n, rng), 500, 1800) * np.exp(-np.arange(n) / (0.012 * RATE))[:, None]
    place(mix, puff / max(1e-9, np.abs(puff).max()), hit, -8)
    place(mix, dsp.porcelain_ring(1250, 0.2, rng, decay=0.05, side=-0.2), hit, -6)
    place(mix, dsp.porcelain_ring(1660, 0.18, rng, decay=0.04, side=0.2), hit + 0.006, -8)
    place(mix, lp(dsp.box_tine(dsp.hz("Ab5"), 0.25, rng, decay=0.08), 2500), hit + 0.05, -12)
    place(mix, lp(dsp.box_tine(dsp.hz("F5"), 0.3, rng, decay=0.1), 2200), hit + 0.11, -11)
    return room(mix, 0.16, 0.38, 0.12)


@cue("ClawCrushWarn", -13, "Claws", 0.38, 0.9,
     "掴んだまま締め上げる（握り潰しの予告）。4回の締め付けに合わせて真鍮の爪車と磁器の軋みが少しずつ高くなり、"
     "オルガンのペダル（F2 と C3）が膨らみ、細かなひびが増えていく。握り潰しの直前で切れる。")
def claw_crush_warn(s, rng):
    length = (CLAW_CRUSH - CLAW_WARN_FROM) * TICK - 0.008
    mix = seconds(length + 0.05)
    for k, beat in enumerate(CLAW_SQUEEZES):
        at = (beat - CLAW_WARN_FROM) * TICK
        place(mix, pan(dsp.brass_click(rng, 1900 * 2 ** (k * 1.5 / 12), decay=0.005, dur=0.06, thud=0.5), -0.15 + 0.1 * k), at, -6 + k)
        place(mix, dsp.porcelain_crack(0.08, rng, count=3 + k, spread=0.01, low=2200, high=5000, decay=(0.005, 0.015)), at + 0.004, -14 + k)
    pedal = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3")], length, rng, attack=length * 0.9, release=0.015, harmonics=8,
                          rolloff=1.2, chiff=0.0, breath=0.25)
    place(mix, pedal, 0.0, -6)
    crackle = reward.sparks(length, rng, 18, low=2500, high=7000, decay=(0.002, 0.006), fall=0.5)
    place(mix, crackle * np.linspace(0.2, 1, len(crackle))[:, None], 0.0, -16)
    return taper(mix[:round(length * RATE)], 0.015)


# The claws' own soft gong (Pale Meridian's soft_gong, Last Witness's brass_gong and the Choir's choir_gong are
# separate blocks): a soft struck bronze plate tuned to F (original modal synthesis, no recording). Per partial:
# (ratio to the strike note, level, decay as a fraction of tau, bloom). The partials sit on the F minor pentatonic
# (F Ab C Eb, a few cents apart so the plate shimmers); the upper ones swell in over the first ~0.1 s as the strike's
# energy spreads, as a gong's do.
CLAW_GONG_MODES = ((1.0, 1.0, 1.0, 0.0), (2.0, 0.42, 0.62, 0.35), (2.378, 0.2, 0.5, 0.8), (2.997, 0.26, 0.45, 0.6),
                   (3.565, 0.12, 0.34, 1.0), (4.762, 0.07, 0.24, 1.0), (5.993, 0.045, 0.18, 1.0))


def claw_gong(freq, dur, rng, tau=1.5, bloom=0.11, mallet=0.006):
    """A felt-mallet strike on a tuned gong: the CLAW_GONG_MODES partials, each beside a quieter twin a fraction of a
    hertz away (slow beating that drifts across the stereo field), a few-millisecond onset with no click, a low
    felt thud and a long exponential tail (the strike note's 1/e time is `tau` seconds)."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for i, (ratio, amp, life, swell) in enumerate(CLAW_GONG_MODES):
        f = freq * ratio * 2 ** (rng.uniform(-4, 4) / 1200 if i else 0)
        if f * 1.01 >= dsp.CEILING_HZ:
            break
        env = np.exp(-t / (tau * life)) * (1 - swell * np.exp(-t / bloom))
        beat = rng.uniform(0.18, 0.55)
        phase = rng.uniform(0, 2 * np.pi)
        for ch, sign in ((0, 1), (1, -1)):
            twin = np.sin(2 * np.pi * (f + sign * beat) * t + phase + ch)
            out[:, ch] += amp * env * (np.sin(2 * np.pi * f * t + phase) + 0.45 * twin)
    out *= (0.5 - 0.5 * np.cos(np.pi * np.clip(t / mallet, 0, 1)))[:, None]
    k = round(0.06 * RATE)
    felt = lp(base.noise(k, rng), 260) * np.exp(-np.arange(k) / (0.014 * RATE))[:, None]
    out[:k] += felt / max(1e-9, np.abs(felt).max()) * 0.35 * np.abs(out).max()
    return out / max(1e-9, np.abs(out).max())


@cue("ClawCrushFire", -10, "Claws", 2.3, 1.0,
     "握り潰し（最大の一撃）。磁器がたくさん砕ける連鎖と、とても低い衝撃・胴鳴りで握り潰した瞬間を出し、"
     "そのあとは F に調律した柔らかい銅鑼（ゴング）の響きが約2秒かけて消えていき、下で静かなオルガン（F マイナー、F2 C3 F3 A♭3）が"
     "息をつくように支える。オルガンは鳴らし切らない。オルゴールの F6 がかすかに残る。")
def claw_crush_fire(s, rng):
    mix = seconds(2.4)
    hit = CLAW_STRIKE_LEAD
    place(mix, dsp.porcelain_crack(0.3, rng, count=11, spread=0.045, low=1800, high=7000, decay=(0.01, 0.04)), hit, -2)
    lay(mix, s, "low_impact", 0.62, 1.4, hit - 0.01, -4, lp_=600, fade_in=0.004, fade_out=0.3)
    place(mix, dsp.thump(95, 36, 0.5, rng), hit, -3)
    lay(mix, s, "chop", 0.035, 0.2, hit - 0.012, -10, hp_=200, fade_in=0.001)
    # The resonance that closes it: a soft low strike on the F gong, its long decaying tail carrying the cue.
    place(mix, pan(claw_gong(dsp.hz("F2"), 2.3, rng), 0.0), hit + 0.004, -6)
    # A quiet organ under it: a slow swell, few and soft harmonics, no chiff, a long release.
    pad = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3", "F3", "Ab3")], 1.9, rng, attack=0.12, release=1.3, harmonics=6,
                        rolloff=1.9, chiff=0.0, breath=0.1)
    place(mix, pad, hit + 0.03, -17)
    place(mix, pan(dsp.box_tine(dsp.hz("F6"), 0.8, rng), 0.2), hit + 0.09, -19)
    return room(mix, 0.1, 2.25, 0.7)


# ---------------------------------------------------------------- render
def seed(name):
    return int.from_bytes(hashlib.sha256((SEED_PREFIX + name).encode("utf-8")).digest()[:8], "little")


def render(name, store):
    raw = CUES[name].build(store, np.random.default_rng(seed(name)))
    if CUES[name].loop:
        # A loop is exact: filtered circularly (three copies, the middle kept), never trimmed or faded.
        n = len(raw)
        x = hp(np.concatenate((raw, raw, raw)), 28)[n:2 * n]
    else:
        x = trim(hp(raw, 28))
    limit = round(CUES[name].max_seconds * RATE)
    if len(x) > limit:
        raise RuntimeError(f"{name}: {len(x) / RATE:.3f} s exceeds its {CUES[name].max_seconds} s budget")
    return x


def cue_path(name, output):
    return output / f"{name}.{'wav' if CUES[name].loop else 'ogg'}"


def export_path(name, output):
    return cue_path(name, output)


def write_loop(path, x):
    """PCM16 WAV (sample-exact for a seamless native loop); returns what a reader decodes."""
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(path), x.astype(np.float32), RATE, subtype="PCM_16")
    decoded, rate = sf.read(str(path), always_2d=True, dtype="float64")
    if rate != RATE or decoded.shape != x.shape:
        raise ValueError(f"Unexpected WAV decode: {path}")
    return decoded


def render_loop(name, store):
    """A loop cue: exactly max_seconds of stereo, periodic by construction; the shared 28 Hz high-pass runs over three
    periods and the middle one is kept, so the seam stays sample-exact."""
    spec = CUES[name]
    n = round(spec.max_seconds * RATE)
    x = spec.build(store, np.random.default_rng(seed(name)))
    if x.shape != (n, 2) or not np.isfinite(x).all():
        raise RuntimeError(f"{name}: a loop must be exactly {n} stereo frames, got {x.shape}")
    return hp(np.concatenate((x, x, x)), 28)[n:2 * n]


def loop_peak_db(x):
    """True peak of a loop played round: oversampled over three periods, the middle one measured."""
    n = len(x)
    return 20 * np.log10(np.abs(signal.resample_poly(np.concatenate((x, x, x)), 4, 1, axis=0)[4 * n:8 * n]).max())


def seam(x):
    """Wrap step against the largest inner step (both channels) and the head/tail 20 ms RMS ratio in dB."""
    inner = np.abs(np.diff(x, axis=0)).max()
    wrap = np.abs(x[0] - x[-1]).max()
    k = round(0.02 * RATE)
    rms = lambda y: np.sqrt(np.mean(y ** 2) + 1e-18)  # noqa: E731
    return {"wrap_step": round(float(wrap), 6), "max_inner_step": round(float(inner), 6),
            "head_tail_rms_db": round(float(20 * np.log10(rms(x[:k]) / rms(x[-k:]))), 2)}


def render_loop_cue(name, store, output):
    """Master a loop to its target over a round trip, write PCM16 WAV and back off if the quantized loop overshoots."""
    store.used = set()
    spec = CUES[name]
    path = output / f"{name}.wav"
    x = soft(render_loop(name, store), spec.glue)
    x = x * 10 ** ((spec.target_lufs - loudness(np.concatenate((x, x)))) / 20)
    peak = loop_peak_db(x)
    if peak > -1.3:
        x = x * 10 ** ((-1.3 - peak) / 20)
    for _ in range(8):
        sf.write(str(path), x, RATE, subtype="PCM_16")
        decoded, rate = sf.read(str(path), always_2d=True, dtype="float64")
        if rate != RATE or decoded.shape != x.shape:
            raise ValueError(f"Unexpected WAV decode: {path}")
        peak = loop_peak_db(decoded)
        if peak <= -1.0:
            return decoded, sorted(store.used)
        x = x * 10 ** (-(peak + 1.1) / 20)
    raise RuntimeError(f"{name}: true peak {peak:.2f} dBTP after retries")


def render_builder_loop(name, store, output):
    """A loop: exactly the builder's samples (a whole number of periods), mastered by gain and the memoryless bus only,
    true peak checked across the seam, written as PCM16 WAV and read back sample for sample."""
    spec = CUES[name]
    x = spec.build(store, np.random.default_rng(seed(name)))
    if len(x) > round(spec.max_seconds * RATE):
        raise RuntimeError(f"{name}: {len(x) / RATE:.3f} s exceeds its {spec.max_seconds} s budget")
    mastered = master(x, spec.target_lufs, spec.glue)
    peak = true_peak_db(np.vstack((mastered, mastered)))
    if peak > -1.0:
        mastered = mastered * 10 ** (-(peak + 1.1) / 20)
    path = export_path(name, output)
    sf.write(str(path), mastered.astype(np.float32), RATE, subtype="PCM_16")
    decoded, rate = sf.read(str(path), always_2d=True, dtype="float64")
    if rate != RATE or decoded.shape != x.shape:
        raise RuntimeError(f"{name}: the loop did not round-trip sample-exactly")
    return decoded, sorted(store.used)


def render_cue(name, store, output):
    """Master to the cue's target, write the Ogg (or the WAV loop) and, if the decoded file overshoots -1 dBTP, back
    the gain off. A "round" loop is mastered by render_loop_cue, a "builder" loop by render_builder_loop."""
    if CUES[name].loop and CUES[name].loop_master == "round":
        return render_loop_cue(name, store, output)
    store.used = set()
    spec = CUES[name]
    if spec.loop and spec.loop_master == "builder":
        return render_builder_loop(name, store, output)
    path = cue_path(name, output)
    mastered = master(render(name, store), spec.target_lufs, spec.glue)
    for _ in range(8):
        decoded = write_loop(path, mastered) if spec.loop else base.write_ogg(path, mastered)
        # A loop's true peak includes the wrap from its last sample to its first.
        peak = true_peak_db(np.concatenate((decoded[-64:], decoded[:64], decoded))) if spec.loop else true_peak_db(decoded)
        if peak <= -1.0:
            return decoded, sorted(store.used)
        mastered = mastered * 10 ** (-(peak + 1.1) / 20)
    raise RuntimeError(f"{name}: true peak {peak:.2f} dBTP after retries")


def ogg_serial(data):
    return int.from_bytes(data[14:18], "little")


def pinned_serial(stem):
    """The serial generate_ebon_sfx.write_ogg pins for a file stem."""
    return int.from_bytes(hashlib.sha256(stem.encode("utf-8")).digest()[:4], "little")


OCTAVES = (63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000)


def character(x):
    """Spectral character: centroid, octave-band levels relative to the loudest band, weighted flatness
    (0 = tonal, 1 = noise), side/mid ratio, 10 ms envelope peak time and -20 dB decay time."""
    m = x.mean(axis=1)
    f, p = signal.welch(m, RATE, nperseg=min(4096, len(m)))
    bands = np.array([p[(f >= c / np.sqrt(2)) & (f < min(c * np.sqrt(2), RATE / 2))].sum() for c in OCTAVES])
    bands = 10 * np.log10(bands / bands.max() + 1e-18)
    _, _, z = signal.stft(m, RATE, nperseg=2048)
    power = np.abs(z) ** 2 + 1e-18
    flat = np.exp(np.mean(np.log(power), axis=0)) / np.mean(power, axis=0)
    weight = power.sum(axis=0)
    side = (x[:, 0] - x[:, 1]) / 2
    frames = np.array([np.sqrt(np.mean(m[i:i + 441] ** 2) + 1e-18) for i in range(0, len(m) - 441, 441)])
    db = 20 * np.log10(frames / frames.max())
    top = int(frames.argmax())
    below = np.nonzero(db[top:] < -20)[0]
    return {
        "centroid_hz": round(float((f * p).sum() / (p.sum() + 1e-18))),
        "octave_bands_db": {str(c): round(float(v), 1) for c, v in zip(OCTAVES, bands)},
        "spectral_flatness": round(float((flat * weight).sum() / weight.sum()), 4),
        "side_to_mid_db": round(float(10 * np.log10((side ** 2).sum() / ((m ** 2).sum() + 1e-18) + 1e-18)), 1),
        "peak_ms": top * 10,
        "decay_20db_ms": int(below[0]) * 10 if len(below) else None,
    }


def measure(x):
    return {"seconds": round(len(x) / RATE, 3), "short_term_lufs": round(loudness(x), 2),
            "true_peak_dbfs": round(true_peak_db(x), 2)} | character(x)


def analyse(name, decoded, path, sources_used):
    data = path.read_bytes()
    spec = CUES[name]
    round_loop = spec.loop and spec.loop_master == "round"
    looped = np.concatenate((decoded, decoded)) if round_loop else decoded
    info = measure(decoded) | {
        "short_term_lufs": round(loudness(looped), 2),
        "true_peak_dbfs": round(loop_peak_db(decoded) if round_loop else true_peak_db(decoded), 2),
        "target_lufs": spec.target_lufs,
        "max_seconds": spec.max_seconds,
        "volume": spec.volume,
        "effective_lufs": round(loudness(looped) + 20 * np.log10(spec.volume), 2),
        "sources": sources_used,
        "bytes": len(data),
        "ogg_serial": None if spec.loop else ogg_serial(data),
        "ogg_sha256": sha256(data),
        "sha256": sha256(data),
    } | ({"loop_frames": len(decoded)} | seam(decoded) if round_loop else {})
    if spec.loop and not round_loop:
        info |= {"loop_samples": len(decoded), "loop_ticks": len(decoded) / (RATE / 60),
                 "loop_seam": round(float(np.abs(decoded[0] - decoded[-1]).max()), 5)}
    if info["seconds"] > spec.max_seconds + 1e-6:
        raise RuntimeError(f"{name}: encoded length {info['seconds']} s exceeds {spec.max_seconds} s")
    return info


# ---------------------------------------------------------------- audition (owner listening page, git-ignored)
# Legacy cue each new cue replaces: (source key, effective in-game volume), for the A/B rows.
LEGACY = {"CompanionSummon": ("doll_summon", 0.65)}
# BGM excerpts to hear the cues in the music: (file, BPM, beat-grid origin s, first beat, beats, cue times within the
# excerpt, label). Whole beats on the measured grid (the cue sheet's tempos), so the bed loops in time.
BGM = (("ObsidianLiturgy", 168.0, -0.008, 268, 44, (2.0, 7.3, 12.1), "第1相（ObsidianLiturgy）"),
       ("DistantLiturgy", 218.0, 0.068, 28, 56, (2.0, 7.3, 12.1), "第3相（DistantLiturgy、全武器の調 F の基準）"))


def load_bgm(name, start, length):
    """Excerpt of a Doll BGM resampled to 44.1 kHz (the masters are 48 kHz); checked against the original
    by cross-correlation so the excerpt keeps its speed and pitch (the audition plays at the real tempo)."""
    x, rate = sf.read(str(ROOT / "Assets" / "Music" / f"{name}.ogg"), always_2d=True, dtype="float64")
    seg = x[round(start * rate):round((start + length) * rate), :2]
    out = signal.resample_poly(seg, RATE // np.gcd(rate, RATE), rate // np.gcd(rate, RATE), axis=0)
    back = signal.resample_poly(out, rate // np.gcd(rate, RATE), RATE // np.gcd(rate, RATE), axis=0)[:len(seg)]
    a, b = seg.mean(axis=1)[rate:rate * 3], back.mean(axis=1)[rate:rate * 3]
    corr = signal.correlate(a, b, mode="full", method="fft")
    lag = int(corr.argmax()) - (len(b) - 1)
    r = float(np.dot(a, b) / np.sqrt(np.dot(a, a) * np.dot(b, b)))
    if lag != 0 or r < 0.999:
        raise RuntimeError(f"BGM resample check failed for {name}: lag {lag}, r {r:.5f}")
    return fades(out, 0.004, 0.004), {"source_rate": rate, "lag": lag, "r": round(r, 6)}


def overlay(bed, cue_x, times, gain):
    mix = bed.copy()
    for t in times:
        place(mix, cue_x * gain, t)
    return mix


# Audition page text per cue group: (window title, heading, intro HTML). The first group of the rendered cues wins.
PAGES = {
    "Companion": ("人形の相棒 召喚音", "人形の相棒 — 新しい召喚音", (
        "<p>変わるのは召喚の瞬間の音だけです（同じきっかけで1回鳴る）。旧 DollSummon と、ゲーム内の音量で聴き比べてください。\n"
        "BGM の行は、実際の Doll の BGM（44.1 kHz に変換し、元の速さと音程のまま）に、ゲーム内の音量で重ねています（効果音と音楽の音量設定はどちらも 100%）。</p>\n"
        "<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）で、BGM に合わせた音程の補正はしていません（0 セント）。\n"
        "第3相の BGM はこの調の基準ですが、第1相は約2半音低い調で鳴っているので、そこでのなじみ方も確かめてください。</small></p>")),
    "Choir": ("未成の聖歌隊 効果音", "未成の聖歌隊 — 新しい効果音（試聴）", (
        "<p>召喚、歌の予兆と音符、オルガンの登場とパイプ、合唱の予兆と光線、成功の終わりと失敗の音です。どれもゲーム内の音量で並べています。"
        "下の「通しの演奏」は、1回の演目（10.8 秒、100 BPM の 18 拍）をゲームと同じ時刻に並べたもので、声の数で合唱の厚みとパイプの数が変わります。"
        "BGM の行は、実際の Doll の BGM（44.1 kHz に変換し、元の速さと音程のまま、拍の頭から切り出し）に重ねています（効果音と音楽の音量設定はどちらも 100%）。</p>\n"
        "<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）。歌は Fm7 Fm7 B♭7sus B♭7sus A♭maj7 E♭sus、合唱は B♭7sus → Fm9 → F の空虚五度"
        "（旧版の D メジャーの終止はやめました）。人形の声は録音ではなく、ソプラノのフォルマントで作った合成音です。"
        "演目は武器自身のテンポ（100 BPM）で、BGM の拍とは同期しません。</small></p>\n"
        "<p><small>締めは、別の Raid の A/B（2026-10-03）で選ばれた「静かなオルガン＋ゴング」の方向です。合唱の最後の F の空虚五度は"
        "鳴らし切らずに引いていき、切れ目で ChoirChorusEnd（柔らかいゴングと静かなオルガン、長い余韻）が締めます。"
        "合唱の途中で標的を倒したときも、失敗の音ではなくこの締めの音で静かに閉じます。</small></p>")),
}

# Build-up-to-release sequences per group: (file stem, label, builder(samples) -> (mix, [(s, cue)])). The first entry
# is also laid over each BGM excerpt.
SEQUENCES = {
    "Choir": (
        ("ChoirConcert4", "通しの演奏・声4本（歌は4声部の分散和音、パイプ9本、合唱4声）", lambda x: choir_concert(x, 4)),
        ("ChoirConcert1", "通しの演奏・声1本（主旋律だけ、パイプ3本、合唱1声）", lambda x: choir_concert(x, 1)),
        ("ChoirConcert6", "通しの演奏・声6本（重なる声部は少し大きく、パイプ11本、合唱6声）", lambda x: choir_concert(x, 6)),
        ("ChoirCancel4", "失敗の例・声4本（予兆の途中、拍10.6 で標的を失う）", lambda x: choir_concert(x, 4, cancel=382)),
        ("ChoirFinish4", "合唱の途中で標的を倒した例・声4本（拍13.3 で倒す。失敗の音ではなく、締めの音で静かに閉じる）",
         lambda x: choir_concert(x, 4, finish=480)),
    ),
}


def preview(directory, samples, report, store, previous=None):
    directory.mkdir(parents=True, exist_ok=True)
    if samples and all(CUES[name].group.startswith("Meridian") for name in samples):
        return meridian_preview(directory, samples, report, previous)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    rows, extra = [], {}
    for name, x in samples.items():
        if CUES[name].group not in PAGES:
            continue  # the other weapons have their own audition pages (e.g. --witness-audition)
        spec = CUES[name]
        clips = {"new": wav(name, x * spec.volume)}
        if name in LEGACY:
            key, volume = LEGACY[name]
            old = store.get(key) * volume
            clips["old"] = wav(name + "-old", old)
            gap = seconds(0.9)
            clips["ab"] = wav(name + "-ab", np.concatenate((old, gap, x * spec.volume, gap, old, gap, x * spec.volume)))
            extra[name] = {"legacy": measure(store.get(key)) | {"volume": volume,
                                                                "effective_lufs": round(loudness(store.get(key)) + 20 * np.log10(volume), 2)}}
            for bgm, bpm, origin, first, beats, times, label in BGM:
                start, length = origin + first * 60 / bpm, beats * 60 / bpm
                bed, check = load_bgm(bgm, start, length)
                new, old = overlay(bed, x, times, spec.volume), overlay(bed, store.get(key), times, volume)
                # One common gain for the bed and both versions: relative levels stay the in-game ones.
                trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(new).max(), np.abs(old).max(), np.abs(bed).max())))
                g = 10 ** (trim_db / 20)
                clips[f"bgm-{bgm}"] = wav(f"{name}-{bgm}", new * g)
                clips[f"bgm-{bgm}-old"] = wav(f"{name}-{bgm}-old", old * g)
                clips[f"bed-{bgm}"] = wav(f"bgm-{bgm}", bed * g)
                extra[name][bgm] = check | {"start": round(start, 4), "length": round(length, 4), "bpm": bpm,
                                            "beats": beats, "cue_times": list(times), "mix_trim_db": round(trim_db, 2)}
        rows.append((name, clips))
    groups = [CUES[name].group for name in samples]
    group = groups[0] if groups else "Companion"
    combos = []
    for stem, label, build in SEQUENCES.get(group, ()):
        try:
            mix, events = build(samples)
        except KeyError:
            continue  # a cue of this sequence was not rendered in this run
        trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(mix).max(), 1e-9)))
        entry = {"stem": stem, "label": label, "file": wav(stem, mix * 10 ** (trim_db / 20)), "events": events,
                 "seconds": round(len(mix) / RATE, 3), "mix_trim_db": round(trim_db, 2), "bgm": []}
        if not combos:
            for bgm, bpm, origin, first, beats, _, bgm_label in BGM:
                start, length = origin + first * 60 / bpm, beats * 60 / bpm
                bed, check = load_bgm(bgm, start, length)
                at = 60 / bpm  # the sequence starts on the excerpt's second beat
                over = bed.copy()
                place(over, mix, at)
                g = min(0.0, 20 * np.log10(0.97 / max(np.abs(over).max(), np.abs(bed).max())))
                entry["bgm"].append({"label": bgm_label, "file": wav(f"{stem}-{bgm}", over * 10 ** (g / 20)),
                                     "bed": wav(f"bgm-{bgm}", bed * 10 ** (g / 20)), "start": round(start, 4),
                                     "length": round(length, 4), "bpm": bpm, "beats": beats, "at": round(at, 4),
                                     "mix_trim_db": round(g, 2)} | check)
        combos.append(entry)
    extra["combos"] = combos
    page(directory, rows, report, extra, group)
    return extra


def page(directory, rows, report, extra, group="Companion"):
    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    def metrics(r):
        bands = " / ".join(f"{v:+.0f}" for v in r["octave_bands_db"].values())
        return (f"{r['seconds']:.2f} s ・ 短時間 {r['short_term_lufs']:.1f} LUFS ・ 実効 {r['effective_lufs']:.1f} LUFS（音量 {r['volume']}）"
                f" ・ トゥルーピーク {r['true_peak_dbfs']:.1f} dBTP ・ 重心 {r['centroid_hz']} Hz<br>"
                f"<small>オクターブ帯 63 Hz〜16 kHz（最大帯比 dB）: {bands} ・ 平坦度 {r['spectral_flatness']}"
                f" ・ ピーク {r['peak_ms']} ms ・ -20 dB 減衰 {r['decay_20db_ms']} ms</small>")

    blocks = []
    for name, clips in rows:
        r = report[name]
        body = [f"<h2>{html.escape(name)}</h2><p>{html.escape(CUES[name].description)}</p>",
                "<table>",
                f"<tr><td><b>新</b> {html.escape(name)}</td><td>{audio(clips['new'])}</td><td>{metrics(r)}</td></tr>"]
        if "old" in clips:
            old = extra[name]["legacy"]
            body.append(f"<tr><td><b>旧</b> DollSummon</td><td>{audio(clips['old'])}</td><td>{metrics(old)}</td></tr>")
            body.append(f"<tr><td><b>A/B</b> 旧→新→旧→新</td><td>{audio(clips['ab'])}</td><td><small>ゲーム内の音量（旧 .65、新 {CUES[name].volume}）で並べています。</small></td></tr>")
        body.append("</table>")
        bgm_rows = [(bgm, label) for bgm, *_, label in BGM if f"bgm-{bgm}" in clips]
        if bgm_rows:
            body.append("<h3>BGM の中で</h3><table>")
            for bgm, label in bgm_rows:
                e = extra[name][bgm]
                times = "、".join(f"{t:g}" for t in e["cue_times"])
                body.append(f"<tr><td>{html.escape(label)}<br><small>{e['start']:.1f} 秒から {e['beats']} 拍（{e['bpm']:g} BPM）。"
                            f"{times} 秒に鳴らす。クリップしないよう3本とも同じだけ {-e['mix_trim_db']:.1f} dB 下げています。</small></td>"
                            f"<td>新 {audio(clips[f'bgm-{bgm}'])}<br>旧 {audio(clips[f'bgm-{bgm}-old'])}</td>"
                            f"<td>BGM だけ（ループ） {audio(clips[f'bed-{bgm}'], loop=True)}</td></tr>")
            body.append("</table>")
        blocks.append("\n".join(body))
    for c in extra.get("combos", ()):
        times = "、".join(f"{at:g} 秒 {html.escape(name)}" for at, name in c["events"])
        level = (f"クリップしないよう {-c['mix_trim_db']:.1f} dB 下げています。" if c["mix_trim_db"] < -0.05
                 else "ゲーム内の音量のまま並べています。")
        body = [f"<h2>{html.escape(c['label'])}</h2>",
                f"<table><tr><td>通し（{c['seconds']:.1f} 秒）</td><td>{audio(c['file'])}</td>"
                f"<td><small>{times}。{level}</small></td></tr>"]
        for b in c["bgm"]:
            body.append(f"<tr><td>{html.escape(b['label'])}<br><small>{b['start']:.1f} 秒から {b['beats']} 拍（{b['bpm']:g} BPM）。"
                        f"演奏は {b['at']:.2f} 秒（2拍目の頭）から。BGM と演奏をそろって {-b['mix_trim_db']:.1f} dB 下げています。"
                        f"変換の確認: ずれ {b['lag']}、相関 {b['r']}</small></td>"
                        f"<td>BGM と一緒に {audio(b['file'])}</td><td>BGM だけ（ループ） {audio(b['bed'], loop=True)}</td></tr>")
        body.append("</table>")
        blocks.append("\n".join(body))
    title, heading, intro = PAGES.get(group, PAGES["Companion"])
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>{title}</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1100px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2,h3{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>{heading}</h1>
{intro}
{''.join(blocks)}
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- weapon audition (one group per page)
# A weapon's page: every cue alone at its game volume, the full build-up-to-release combo on the weapon's own tick
# schedule (cues placed where DollCueClock fires them, tonal cues at their ladder steps, the loop leased with the
# in-game fades), a failing variant, and the combo over beat-aligned Doll BGM excerpts.
def ladder_shift(step, root):
    """SoundStyle.Pitch of DollWeaponAudio.Note as a varispeed factor (pitch and time together, as in game)."""
    semis = dsp.midi(dsp.LADDER[step]) - dsp.midi(dsp.LADDER[root])
    return 2 ** (semis / 12)


def lacuna_events(release, starved=False, target_ticks=14):
    """(tick, cue, ladder step or None) of one held Lacuna channel from the press (tick 0) to `release`; pellets land
    `target_ticks` after they leave (an enemy about 500 px away) and the beam stays on one target."""
    L = LACUNA
    events = []
    for i, birth in enumerate(L["births"]):
        events.append((birth, "LacunaIrisWarn", None))
        first = birth + L["shot_delay"]
        events.append((first, "LacunaIrisFire", None))
        events.append((first, "LacunaIrisTine", i))
        events.append((first + target_ticks, "LacunaPelletHit", None))
        shot = first + L["shot_period"]
        while shot < L["merge"]:
            events.append((shot - L["shot_tell"], "LacunaPelletWarn", None))
            events.append((shot, "LacunaPelletFire", i))
            events.append((shot + target_ticks, "LacunaPelletHit", None))
            shot += L["shot_period"]
    if release > L["merge"]:
        events.append((L["merge"], "LacunaMergeWarn", None))
    if release > L["formed"]:
        events += [(L["formed"], "LacunaMergeFire", None), (L["formed"], "LacunaBeamWarn", None)]
    if release > L["fire"]:
        events.append((L["fire"], "LacunaBeamFire", None))
        events += [(t, "LacunaBeamHit", None) for t in range(L["fire"] + 11, release, 20)]
        events += [(t, f"LacunaWiden{k + 1}", None) for k, t in enumerate(L["widen"]) if t < release]
    events.append((release, "LacunaBeamMiss" if starved else "LacunaBeamEnd", None))
    return sorted(e for e in events if e[0] <= release)


def render_combo(samples, events, release, loop_name=None, loop_from=None):
    """Mix the events at game volume; the loop fades in (+.25/tick) from `loop_from` and out (-.17/tick) at `release`."""
    end = _ticks(release) + 1.6
    mix = seconds(end)
    for tick, name, step in events:
        x = samples[name] * CUES[name].volume
        if step is not None:
            x = speed(x, ladder_shift(step, LACUNA_NOTE_ROOT))
        place(mix, x, _ticks(tick))
    if loop_name and loop_from is not None and release > loop_from:
        loop = samples[loop_name] * CUES[loop_name].volume
        n = len(mix)
        start, stop = round(_ticks(loop_from) * RATE), round(_ticks(release) * RATE)
        bed = np.tile(loop, (int(np.ceil(n / len(loop))) + 1, 1))[:n - start]
        t = np.arange(n - start) / RATE * 60  # ticks since the loop started
        gain = np.clip(t * 0.25, 0, 1)
        after = (np.arange(n - start) + start - stop) / RATE * 60
        gain = np.minimum(gain, np.clip(1 - np.maximum(after, 0) * 0.17, 0, 1))
        mix[start:] += bed * gain[:, None]
    return mix


COMBOS = {
    "Lacuna": (("combo", "押し続けて 800tick（13.3秒）で放す", lambda: lacuna_events(800), "LacunaBeamLoop", LACUNA["fire"] + 4, 800),
               ("combo-miss", "光線中の 600tick でマナが尽きる（失敗）", lambda: lacuna_events(600, starved=True),
                "LacunaBeamLoop", LACUNA["fire"] + 4, 600),
               ("combo-cancel", "光線の前、250tick で放す（開いた虹彩が閉じるだけ）", lambda: lacuna_events(250), None, None, 250)),
}
GROUP_TITLES = {"Lacuna": ("欠落の遺言 — 新しい効果音", "doll-lacuna")}


def weapon_preview(directory, group, samples, report):
    directory.mkdir(parents=True, exist_ok=True)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    clips, extra = {}, {"combos": {}, "bgm": {}}
    for name, x in samples.items():
        if CUES[name].loop:
            clips[name] = wav(name, np.tile(x, (2, 1)) * CUES[name].volume)
        else:
            clips[name] = wav(name, x * CUES[name].volume)
    combos = {}
    for key, label, make, loop_name, loop_from, release in COMBOS[group]:
        mix = render_combo(samples, make(), release, loop_name, loop_from)
        trim_db = min(0.0, 20 * np.log10(0.97 / np.abs(mix).max()))
        combos[key] = (label, mix, trim_db)
        clips[key] = wav(key, mix * 10 ** (trim_db / 20))
        extra["combos"][key] = {"label": label, "seconds": round(len(mix) / RATE, 3), "release_tick": release,
                                "trim_db": round(trim_db, 2), "short_term_lufs": round(loudness(mix), 2)}
    label, mix, _ = combos["combo"]
    for bgm, bpm, origin, first, _, _, bgm_label in BGM:
        beat = 60 / bpm
        lead = 4 * beat                       # the combo starts on the excerpt's fifth beat
        beats = int(np.ceil((lead + len(mix) / RATE + 0.5) / beat))
        start, length = origin + first * beat, beats * beat
        bed, check = load_bgm(bgm, start, length)
        over = bed.copy()
        place(over, mix, lead)
        trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(over).max(), np.abs(bed).max())))
        g = 10 ** (trim_db / 20)
        clips[f"combo-{bgm}"] = wav(f"combo-{bgm}", over * g)
        clips[f"bed-{bgm}"] = wav(f"bgm-{bgm}", bed * g)
        extra["bgm"][bgm] = check | {"label": bgm_label, "start": round(start, 4), "length": round(length, 4), "bpm": bpm,
                                     "beats": beats, "combo_at": round(lead, 4), "mix_trim_db": round(trim_db, 2)}
    weapon_page(directory, group, clips, report, extra)
    return extra


def weapon_page(directory, group, clips, report, extra):
    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    def metrics(r):
        return (f"{r['seconds']:.2f} 秒 ・ 短時間 {r['short_term_lufs']:.1f} LUFS（目標 {r['target_lufs']:g}）・ 実効 {r['effective_lufs']:.1f} LUFS"
                f"（音量 {r['volume']}）・ トゥルーピーク {r['true_peak_dbfs']:.1f} dBTP ・ 重心 {r['centroid_hz']} Hz")

    title, _ = GROUP_TITLES[group]
    rows = []
    for name, spec in CUES.items():
        if spec.group != group or name not in report:
            continue
        r = report[name]
        loop_note = "<br><small>ループ：2周つなげて再生（継ぎ目が聞こえないか確認）。ゲームでは 4tick でフェードイン、6tick でフェードアウト。</small>" if spec.loop else ""
        rows.append(f"<tr><td><b>{html.escape(name)}</b><br><small>{html.escape(spec.description)}</small>{loop_note}</td>"
                    f"<td>{audio(clips[name])}</td><td><small>{metrics(r)}</small></td></tr>")
    combo_rows = []
    for key, info in extra["combos"].items():
        level = (f"クリップしないよう全体を {-info['trim_db']:.1f} dB 下げています（各音の比率はゲーム内の音量のまま）" if info["trim_db"] < -0.05
                 else "ゲーム内の音量のまま")
        combo_rows.append(f"<tr><td><b>{html.escape(info['label'])}</b><br><small>{info['seconds']:.1f} 秒。{level}。</small></td>"
                          f"<td>{audio(clips[key])}</td></tr>")
    bgm_rows = []
    for bgm, e in extra["bgm"].items():
        bgm_rows.append(f"<tr><td>{html.escape(e['label'])}<br><small>{e['start']:.1f} 秒から {e['beats']} 拍（{e['bpm']:g} BPM、"
                        f"44.1 kHz に変換し元の速さと音程を相互相関で確認、r = {e['r']}）。コンボは5拍目の頭から。"
                        f"クリップしないよう両方を {-e['mix_trim_db']:.1f} dB 下げています。</small></td>"
                        f"<td>コンボ＋BGM {audio(clips[f'combo-{bgm}'])}</td><td>BGM だけ（ループ） {audio(clips[f'bed-{bgm}'], loop=True)}</td></tr>")
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>{html.escape(title)}</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1150px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>{html.escape(title)}</h1>
<p>欠落の遺言（魔法武器）の新しい音です。押し続けると虹彩が7つ順に開いて小さな虚無の弾を撃ち、5.7秒で手の前の大きな穴にまとまり、
0.8秒の溜めのあと黒い芯の光線を出し続けます。音はすべて新規で、各段階に予告（Warn）と本番（Fire）の組があります。
マナ切れで失敗したときは別の音（LacunaBeamMiss）です。</p>
<p><small>音量はすべてゲーム内の値（効果音と音楽の音量設定はどちらも 100%）で並べています。調は全武器共通の F マイナー・ペンタトニック
（F A♭ B♭ C E♭）で、BGM に合わせた音程補正はしていません（0 セント）。単音の LacunaIrisTine と LacunaPelletFire は C6 で録り、
ゲームでは虹彩の順に F5 A♭5 B♭5 C6 E♭6 F6 A♭6 の高さで鳴ります（コンボではその高さで並べています）。複数の音を重ねたほかの音は、
どれも録ったままの高さで鳴ります。</small></p>
<h2>通しで（ゲームと同じタイミング）</h2><table>{''.join(combo_rows)}</table>
<h2>BGM に重ねて</h2><table>{''.join(bgm_rows)}</table>
<h2>一つずつ</h2><table>{''.join(rows)}</table>
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- Pale Meridian audition
# The weapon's score, mirrored from PaleMeridianScore.cs / PaleMeridianLattice.cs (tools/tests pins the two), so the
# combo plays every cue on its in-game tick at its call-site volume (MeridianVisuals.cs).
MERIDIAN_SEATS = (108, 180, 228, 264)
MERIDIAN_CADENCE = (24, 18, 12, 9, 6)
MERIDIAN_BUILD_PHRASE = (0, 1, 3, 4, 5, 4, 2, 1, 1, 3, 4, 6, 7, 6, 4, 2, 3, 4, 5, 6, 7, 8)
MERIDIAN_OVERCHARGE_PHRASE = (5, 3, 1, 3, 2, 5, 4, 5, 1, 4, 3, 4, 2, 4, 5, 6)
MERIDIAN_FLIGHT = 16
MERIDIAN_LOOP_VOLUME = 0.5


def meridian_parts(age):
    return sum(1 for seat in MERIDIAN_SEATS if age >= seat)


def meridian_shot(age):
    """'note', 'round', 'heavy' or None for the round fired at this score age."""
    if age < 12 or MERIDIAN_KEY_RISE <= age < MERIDIAN_IGNITE:
        return None
    if age >= MERIDIAN_IGNITE:
        since = age - MERIDIAN_IGNITE
        return None if since % 3 else "heavy" if since % 36 == 0 else "round"
    stage = meridian_parts(age)
    start = 12 if stage == 0 else MERIDIAN_SEATS[stage - 1]
    return "note" if (age - start) % MERIDIAN_CADENCE[stage] == 0 else None


def meridian_note_step(age):
    if age >= MERIDIAN_IGNITE:
        since = age - MERIDIAN_IGNITE
        return MERIDIAN_OVERCHARGE_PHRASE[since // 9 % 16] if since % 9 == 0 else -1
    if meridian_shot(age) != "note":
        return -1
    return MERIDIAN_BUILD_PHRASE[sum(1 for a in range(12, age) if meridian_shot(a) == "note")]


def meridian_lattice_start(node):
    """Ticks from the release to the lattice's split (age -10) and fire (age 0), as PaleMeridianLattice.LatticeStart."""
    span = int(np.ceil(MERIDIAN_MERIDIAN_FIRE - 1 + 8 * node / (node + 640) - 1e-4)) + 10
    return span - 11, span - 1


def meridian_combo(samples, release, fired=True, node=360.0, hits=True):
    """One press on the game's own clock (tick 0 = the press; a cue of score age a starts at tick a - 1), each cue
    at its call-site volume: the build's notes, part flights and seats, the key and the wind, the overcharge loop with
    heavy bars and every third round's note, then the release (meridian, lattice) or the failed release, plus the
    owner's throttled hit ticks."""
    tick = 1 / 60
    tier = 0 if release < 108 else 1 if release < 228 else 2 if release < 348 else 3
    mix = seconds(release * tick + 3.4)  # room for the lattice's 2.7 s closing tail
    events = []

    def at(cue_name, age_tick, volume):
        events.append((cue_name, age_tick * tick, volume))

    at("MeridianAssemble", 0, 0.55)
    last_hit = -99
    for age in range(1, release + 1):
        step = meridian_note_step(age)
        if step >= 0:
            at(f"MeridianNote{step}", age - 1, 0.42 if age >= MERIDIAN_IGNITE else 0.5 + 0.03 * meridian_parts(age))
        shot = meridian_shot(age)
        if shot == "heavy" and age > MERIDIAN_IGNITE:
            at("MeridianHeavy", age - 1, 0.5)
        if hits and shot:
            land = age - 1 + 8
            if shot == "heavy":
                at("MeridianHitHeavy", land, 0.5)
            elif land - last_hit >= 4:
                at("MeridianHit", land, 0.35)
                last_hit = land
        for part, seat in enumerate(MERIDIAN_SEATS):
            if age == seat - MERIDIAN_FLIGHT:
                at("MeridianPartWarn", age - 1, 0.45)
            if age == seat:
                at("MeridianPartFire", age - 1, 0.5 + 0.06 * part)
        if age == MERIDIAN_KEY_RISE:
            at("MeridianIgniteWarn", age - 1, 0.6)
        if age == MERIDIAN_IGNITE:
            at("MeridianIgniteFire", age - 1, 0.75)
    if fired and tier > 0:
        at("MeridianStrikeWarn", release, 0.6)
        at("MeridianStrikeFire", release + MERIDIAN_MERIDIAN_FIRE - 1, (0, 0.6, 0.72, 0.85)[tier])
        if hits:
            at("MeridianHitHeavy", release + MERIDIAN_MERIDIAN_FIRE + 1, 0.5)
        if tier >= 2:
            split, fire = meridian_lattice_start(node)
            at("MeridianLatticeWarn", release + split, 0.6)
            at("MeridianLatticeFire", release + fire, 0.9 if tier >= 3 else 0.75)
            if hits:
                at("MeridianHitHeavy", release + fire + 1, 0.5)
    elif tier > 0:
        at("MeridianStrikeMiss", release, 0.55)
    for cue_name, when, volume in events:
        place(mix, samples[cue_name] * volume, when)
    # The overcharge loop: from age 350 (gain +.25 per tick), tiled, fading over 6 ticks after the release.
    if release >= MERIDIAN_IGNITE + 2 and "MeridianLoop" in samples:
        loop = samples["MeridianLoop"]
        start, end = round((MERIDIAN_IGNITE + 1) * tick * RATE), round((release + 6) * tick * RATE)
        n = end - start
        bed = np.tile(loop, (n // len(loop) + 1, 1))[:n]
        t = np.arange(n) / RATE
        ramp = np.clip(t / (4 * tick), 0, 1) * np.clip((n / RATE - t) / (6 * tick), 0, 1)
        mix[start:end] += bed * (ramp * MERIDIAN_LOOP_VOLUME)[:, None]
    return mix, events


# BGM beds for the combo: (file, BPM, beat-grid origin s, first beat, beats, label), as BGM above.
MERIDIAN_BGM = (("ObsidianLiturgy", 168.0, -0.008, 268, 38, "第1相（ObsidianLiturgy）"),
                ("DistantLiturgy", 218.0, 0.068, 28, 50, "第3相（DistantLiturgy、全武器の調 F の基準）"))


def meridian_preview(directory, samples, report, previous=None):
    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    missing = [n for n in CUES if n.startswith("Meridian") and n not in samples]
    if missing:
        raise RuntimeError("the Meridian audition needs every Meridian cue; missing " + ", ".join(missing))
    extra = {"combos": {}, "bgm": {}}
    combos = (("full", 528, True, "通し：押してから組み上げ（5.8 秒）、3 秒の過充填、放して子午線と格子（3段階）。"),
              ("tier1", 160, True, "早めに放す（部品1〜2個、1段階）：子午線だけ。"),
              ("tier2", 300, True, "巻き鍵が出る直前で放す（2段階）：子午線と小さな格子。"),
              ("miss", 200, False, "弾切れで放す：子午線は出ず、失敗の音（MeridianStrikeMiss）。"))
    clips = {}
    for key, release, fired, label in combos:
        mix, events = meridian_combo(samples, release, fired)
        trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(mix).max(), 1e-9)))
        clips[key] = wav(f"combo-{key}", mix * 10 ** (trim_db / 20))
        extra["combos"][key] = {"release_tick": release, "fired": fired, "events": len(events), "mix_trim_db": round(trim_db, 2),
                                "seconds": round(len(mix) / RATE, 3), "label": label}
    full, _ = meridian_combo(samples, 528, True)
    for bgm, bpm, origin, first, beats, label in MERIDIAN_BGM:
        start, length = origin + first * 60 / bpm, beats * 60 / bpm
        bed, check = load_bgm(bgm, start, length)
        lead = 60 / bpm  # the press lands on the second beat of the excerpt
        mix = bed.copy()
        place(mix, full, lead)
        g = 10 ** (min(0.0, 20 * np.log10(0.97 / max(np.abs(mix).max(), np.abs(bed).max()))) / 20)
        clips[f"bgm-{bgm}"] = wav(f"combo-full-{bgm}", mix * g)
        clips[f"bed-{bgm}"] = wav(f"bgm-{bgm}", bed * g)
        extra["bgm"][bgm] = check | {"start": round(start, 4), "length": round(length, 4), "bpm": bpm, "beats": beats,
                                     "press_at": round(lead, 4), "mix_trim_db": round(20 * np.log10(g), 2), "label": label}
    for name, x in samples.items():
        clips[name] = wav(name, x * CUES[name].volume)
    # Cues that differ from an earlier export (--previous): the earlier file at the same game volume, for A/B.
    extra["changed"] = []
    if previous:
        for name in samples:
            old = previous / cue_path(name, Path(".")).name
            if not old.is_file() or sha256(old.read_bytes()) == report[name]["ogg_sha256"]:
                continue
            y, rate = sf.read(str(old), always_2d=True, dtype="float64")
            if rate != RATE:
                raise RuntimeError(f"{old}: {rate} Hz")
            clips[f"{name}-previous"] = wav(f"{name}-previous", y * CUES[name].volume)
            extra["changed"].append({"cue": name, "previous_sha256": sha256(old.read_bytes()), "previous": measure(y)})
    meridian_page(directory, clips, report, extra)
    return extra


def meridian_page(directory, clips, report, extra):
    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    def metrics(r):
        loop = f" ・ ループ {r['loop_samples']} サンプル（{r['loop_ticks']:g} tick）" if "loop_samples" in r else ""
        return (f"{r['seconds']:.2f} 秒 ・ 短時間 {r['short_term_lufs']:.1f} LUFS ・ ゲーム内の音量 {r['volume']} で実効 "
                f"{r['effective_lufs']:.1f} LUFS ・ トゥルーピーク {r['true_peak_dbfs']:.1f} dBTP ・ 重心 {r['centroid_hz']} Hz{loop}")

    groups = (("Meridian build", "組み上げ"), ("Meridian notes", "1発ごとの音符（はしご F5〜C7）"),
              ("Meridian wind", "巻き上げと過充填"), ("Meridian release", "放す（子午線・格子・失敗）"), ("Meridian hits", "命中"))
    combo_rows = "".join(f"<tr><td>{html.escape(info['label'])}<br><small>{info['seconds']:.1f} 秒、{info['events']} 個の音"
                         f"{'（クリップしないよう ' + format(-info['mix_trim_db'], '.1f') + ' dB 下げています）' if info['mix_trim_db'] < 0 else ''}</small></td>"
                         f"<td>{audio(clips[key])}</td></tr>" for key, info in extra["combos"].items())
    bgm_rows = "".join(f"<tr><td>{html.escape(info['label'])}<br><small>{info['start']:.1f} 秒から {info['beats']} 拍（{info['bpm']:g} BPM）。"
                       f"2拍目で押す。44.1 kHz に変換し、元の速さと音程のまま（相関 {info['r']}、ずれ {info['lag']} サンプル）。"
                       f"BGM と効果音を同じだけ {-info['mix_trim_db']:.1f} dB 下げています。</small></td>"
                       f"<td>通し＋BGM {audio(clips['bgm-' + bgm])}<br>BGM だけ（ループ） {audio(clips['bed-' + bgm], loop=True)}</td></tr>"
                       for bgm, info in extra["bgm"].items())
    changed = "".join(f"<tr><td><b>{html.escape(c['cue'])}</b><br><small>前の版: {c['previous']['seconds']:.2f} 秒・短時間 "
                      f"{c['previous']['short_term_lufs']:.1f} LUFS → 今の版: {report[c['cue']]['seconds']:.2f} 秒・短時間 "
                      f"{report[c['cue']]['short_term_lufs']:.1f} LUFS</small></td>"
                      f"<td>前 {audio(clips[c['cue'] + '-previous'])}<br>今 {audio(clips[c['cue']])}</td></tr>"
                      for c in extra.get("changed", []))
    changed_section = (f"<h2>変えた音（前の版と聴き比べ）</h2><p><small>大きな締めの音は「オルガンを大きく鳴らす」より「静かなオルガン＋"
                       f"柔らかな銅鑼のような響きと長い余韻」が好み、という別のレイドでの A/B の結果に合わせて作り直した音です。"
                       f"どちらもゲーム内の音量です。</small></p><table>{changed}</table>") if changed else ""
    sections = []
    for group, title in groups:
        rows = "".join(f"<tr><td><b>{html.escape(name)}</b><br><small>{html.escape(CUES[name].description)}</small></td>"
                       f"<td>{audio(clips[name], loop=CUES[name].loop)}</td><td><small>{metrics(report[name])}</small></td></tr>"
                       for name in CUES if CUES[name].group == group and name in report)
        sections.append(f"<h3>{html.escape(title)}</h3><table>{rows}</table>")
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>蒼白の子午線 効果音</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1150px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2,h3{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}h3{{color:#d9c7f5}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>蒼白の子午線（Pale Meridian）— 新しい効果音</h1>
<p>オルゴール仕掛けの攻城銃の音です。1発ごとにオルゴールの次の音が鳴り、真鍮の部品が嵌まるたびに連射が速くなり、
巻き鍵が回って過充填に入り、放すと白い子午線が撃ち出されて格子に分かれます。音はすべて新しく作ったもので、旧 Ranged* の音は使いません。</p>
<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）で、音程の補正はしていません（0 セント）。音符は音高ごとに別のファイルで、
ゲーム内で音程を変えて鳴らすことはしません。どの行もゲーム内の音量（効果音の音量設定 100%）で鳴らしています。
「通し」はゲームと同じ tick（1/60 秒）の上に、各音をゲーム内の音量で並べたものです（命中音は約 8tick 後に当たったとして入れています）。</small></p>
{changed_section}
<h2>通しで聴く</h2><table>{combo_rows}</table>
<h2>BGM の中で</h2><p><small>実際の Doll の BGM に「通し」を重ねています。効果音と音楽の音量設定はどちらも 100% の想定です。</small></p>
<table>{bgm_rows}</table>
<h2>ひとつずつ</h2>
{''.join(sections)}
<p><small>過充填のループ（MeridianLoop）は WAV で、2.4 秒 = 144 tick ちょうどで継ぎ目なく回ります。再生ボタンでループ再生されます。</small></p>
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- Lacrimosa's Claws audition
def claw_timeline(ticks=500, grasp_delay=30):
    """The claws' build-up to release at attack speed 1, mirroring LacrimosaClawScore (LacrimosaClawHeart.cs): the kata
    A B C from a cold press with every stroke landing, the beads filling (hits with tempo plus the passive trickle),
    the full-meter cadence, the grasp `grasp_delay` ticks after the beads fill (so the cadence is heard alone), the
    crush, and the kata resuming. Returns (tick, cue,
    step) events at the client's cue ticks (LacrimosaClawVisuals); step is the ladder step of a bead note."""
    base_ticks, live, units = (26, 24, 34), (9, 7, 18), (12, 12, 24)
    events, stroke, age, combo, grasp, readyat = [], -1, 0, 0, -1, -1
    meter, passive, streak, last_land, landed, lit = 0, 0, 0, -10 ** 6, False, 0
    for tick in range(ticks):
        busy = 0 <= grasp < 54
        if meter >= 360 and readyat < 0:
            readyat = tick
        if not busy and meter >= 360 and tick >= readyat + grasp_delay:
            grasp, readyat, meter, passive, streak, landed, stroke, combo = 0, -1, 0, 0, 0, False, -1, 0
            busy = True
            events.append((tick, "ClawGraspWarn", None))
            events.append((tick + 14, "ClawGraspFire", None))
            events.append((tick + 17, "ClawCrushWarn", None))
            events.append((tick + 38, "ClawCrushFire", None))
        elif not busy and (stroke < 0 or age >= base_ticks[stroke]):
            stroke, age, hit = combo % 3, 0, False
            combo += 1
            name = ("ClawRakeDown", "ClawRakeUp", "ClawClap")[stroke]
            events.append((tick, name + "Warn", None))
            events.append((tick + (20 if stroke == 2 else live[stroke] - 4), name + "Fire", None))
        if not busy and meter < 360:
            passive += 1
            if passive >= 4:
                passive, meter = 0, min(360, meter + 1)
        if stroke >= 0 and age < base_ticks[stroke]:
            age += 1
            if age == live[stroke]:
                events.append((tick + (2 if stroke == 2 else 0), "ClawHit", None))
                if landed and tick - last_land > 75:
                    streak = 0
                quarters = 4 if streak < 2 else 5 if streak < 5 else 6
                streak, last_land, landed = streak + 1, tick, True
                meter = min(360, meter + units[stroke] * quarters // 4)
        if grasp >= 0:
            grasp += 1
            if grasp >= 64:
                grasp = -2
        beads = meter // 60
        if beads > lit:
            events.append((tick + 1, "ClawBead", beads - 1))
            if beads == 6:
                events.append((tick + 9, "ClawBeadsFull", None))
        lit = beads
    return events


WEAPON_PAGES = {
    "Claws": {"title": "ラクリモーサの双爪 — 効果音", "folder": "doll-claws", "combo": claw_timeline,
              "intro": "左クリックの三段の型（A 右手の振り下ろし、B 左手の振り上げ、C 両手の打ち合わせ）と、6つの心臓玉、右クリックの掴み・握り潰しの音です。"
                       "すべて新規の音で、F マイナー・ペンタトニック（F A♭ B♭ C E♭）の上にあります。各音はゲーム内の音量（下に表示）で書き出しています。"},
}


def claw_preview(directory, group, samples, report):
    """Owner audition page for one weapon group: every cue alone at its in-game volume, the full build-up-to-release
    combo at the client's cue ticks, and that combo over beat-aligned Doll BGM excerpts (resampled to 44.1 kHz and
    cross-checked by load_bgm). Relative links only; the folder is git-ignored."""
    spec = WEAPON_PAGES[group]
    directory.mkdir(parents=True, exist_ok=True)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    clips = {name: wav(name, x * CUES[name].volume) for name, x in samples.items()}
    events = spec["combo"]()
    end = max(t for t, _, _ in events) * TICK + 1.4
    combo = seconds(end)
    for tick, name, step in events:
        x = samples[name]
        if step:  # single-note bead cue moved up the ladder, as SoundStyle.Pitch does (time and pitch together)
            x = speed(x, dsp.ladder(step) / dsp.ladder(0))
        place(combo, x * CUES[name].volume, tick * TICK)
    combo_trim = min(0.0, 20 * np.log10(0.97 / np.abs(combo).max()))
    extra = {"combo": {"seconds": round(end, 3), "events": len(events), "trim_db": round(combo_trim, 2)}}
    combo_clip = wav(f"{group}Combo", combo * 10 ** (combo_trim / 20))
    beds = []
    for bgm, bpm, origin, first, beats, _, label in BGM:
        beat = 60 / bpm
        start, length = origin + first * beat, max(beats * beat, np.ceil((end + 2 * beat) / beat) * beat)
        bed, check = load_bgm(bgm, start, length)
        mix = bed.copy()
        place(mix, combo, 2 * beat)  # the press on the bed's third beat
        trim_db = min(0.0, 20 * np.log10(0.97 / max(np.abs(mix).max(), np.abs(bed).max())))
        g = 10 ** (trim_db / 20)
        beds.append((bgm, label, wav(f"{group}Combo-{bgm}", mix * g), wav(f"bgm-{bgm}", bed * g)))
        extra[bgm] = check | {"start": round(start, 4), "length": round(length, 4), "bpm": bpm, "press_at": round(2 * beat, 4),
                              "mix_trim_db": round(trim_db, 2)}

    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    rows = []
    for name in samples:
        r = report[name]
        rows.append(f"<tr><td><b>{html.escape(name)}</b><br><small>{r['seconds']:.2f} 秒 ・ 音量 {r['volume']} ・ 実効 {r['effective_lufs']:.1f} LUFS"
                    f" ・ ピーク {r['true_peak_dbfs']:.1f} dBTP</small></td><td>{audio(clips[name])}</td>"
                    f"<td>{html.escape(CUES[name].description)}</td></tr>")
    bed_rows = "".join(
        f"<tr><td>{html.escape(label)}<br><small>{extra[bgm]['bpm']:g} BPM、3拍目で押し始め。クリップしないよう両方 {-extra[bgm]['mix_trim_db']:.1f} dB 下げています。</small></td>"
        f"<td>コンボ＋BGM {audio(src)}</td><td>BGM だけ（ループ） {audio(bed_src, loop=True)}</td></tr>"
        for bgm, label, src, bed_src in beds)
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>{html.escape(spec['title'])}</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1150px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:260px}}p{{line-height:1.6}}</style>
<h1>{html.escape(spec['title'])}</h1>
<p>{html.escape(spec['intro'])}</p>
<h2>通しで聴く（溜めから解放まで）</h2>
<p>押した瞬間から、型を繰り返しながら玉が灯り、6つそろった合図、掴み、締め上げ、握り潰し、型の再開までを、ゲームと同じタイミング（60 tick/秒）と音量で並べています
（全 {extra['combo']['events']} 音、{extra['combo']['seconds']:.1f} 秒。クリップしないよう {-extra['combo']['trim_db']:.1f} dB 下げています）。
玉の音は持ち主にだけ聞こえる音で、1つ目から6つ目まで F5 から F6 へ上がります。</p>
<table><tr><td>コンボ（効果音だけ）</td><td>{audio(combo_clip)}</td><td></td></tr>{bed_rows}</table>
<p><small>BGM は実際の Doll の曲を 44.1 kHz に変換したもの（元の速さと音程のまま、変換の前後を相関で確認済み）。効果音と音楽の音量設定はどちらも 100% の想定です。
調は F マイナー・ペンタトニックで、BGM に合わせた音程の補正はしていません（0 セント）。</small></p>
<h2>ひとつずつ</h2>
<table>{''.join(rows)}</table>
</html>""", encoding="utf-8", newline="\n")
    return extra


# ---------------------------------------------------------------- attribution
# Per group: the records' date stamp and date, their review line and the section's introduction (heading included).
LACUNA_ATTRIBUTION = """### Lacuna Testament cues — 2026-10-03

The seventeen cues of the refreshed Lacuna Testament (the magic Doll reward weapon): sixteen stereo Vorbis one-shots and one sample-exact stereo PCM16 WAV loop of exactly 176,400 frames (4.0 s, eight of the beam's 30-tick visual pulse periods). [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns each cue's recipe, its baked beats against the weapon's tick schedule (mirrored from `LacunaTestamentScore` and pinned by `tools/tests/test_doll_weapon_audio.py`), the loudness tiers and the source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump; the generator adds a gong-like plate tuned into the key, also additive synthesis), and both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. Every layer is original synthesis except one Kenney recording (`metalLatch`, the CC0 1.0 file already recorded in the Ebon Manor reward audio table of this register) under the great aperture's clank; it stays in the local store, is SHA-256 verified before use and is not committed. The loop is periodic by construction (whole cycles on its 0.25 Hz grid, FFT-synthesised noise on its own bins, circularly placed tings, a high-pass over three periods), so its wrap is as smooth as its inside. Loudness follows the Ebon scale (BS.1770 K-weighted maximum 400 ms short-term LUFS; true peak at most -1 dBTP after the Vorbis round trip, or over the loop played round). Nothing is transposed at runtime except the two single-pitch cues (`LacunaIrisTine`, `LacunaPelletFire`, every pitched layer a C, recorded at C6 and played on the ladder step of each iris); every composite cue and the loop play as rendered. The audition page and report stay in the git-ignored `.local`.
"""
GROUP_ATTRIBUTION = {
    "Lacuna": ("20261003", "2026-10-03", "Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam "
               "checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run", LACUNA_ATTRIBUTION),
}


# Register record facts per cue group: (date, asset-id stamp, review line, section title, section prose).
RECORDS = {
    "Choir": ("2026-10-03", "20261003",
              "Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak, pitch and chord checks; the concert "
              "combo rendered in sequence over the Phase I and Phase III music; numerical only, not listened; the chorus close "
              "re-voiced restrained, a quiet organ and a soft gong, after the owner's 2026-10-03 A/B on another raid's "
              "finishers); owner, 2026-10-03 (approved on the audition page); in-game mix not_run",
              "Choir of the Unmade cues — 2026-10-03",
              "The refreshed Choir of the Unmade's 29 cues (summon, count-in and nine sung verse notes, organ rise and six pipes, "
              "the chorus warning, six chorus renders by voice count, the success close and the failure, two hit accents). "
              "[`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns every pitch, time, gain and "
              "loudness target; it adds the Choir's own original synthesis (a faceless doll voice: harmonics through soprano "
              "formants with vibrato and breath, its inhale, a failing-wind organ, a soft gong of beating inharmonic "
              "modes) to the blocks of "
              "[`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) and the unmodified helpers of "
              "[`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and "
              "[`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py). No recording is used. Every "
              "pitch is F minor pentatonic except the fifth chorus line's Fm9 colour G. Loudness follows the Doll weapon "
              "tiers (BS.1770 K-weighted maximum 400 ms short-term LUFS), true peak at most -1 dBTP after the Vorbis round "
              "trip. The audition page and report stay in the git-ignored `.local`."),
}


# Lacrimosa's Claws: header, dates and review line of its generated section (group_attribution_section).
ATTRIBUTION_GROUPS = {
    "Claws": {
        "title": "### Lacrimosa's Claws cues — 2026-10-03",
        "date": "20261003", "made": "2026-10-03",
        "about": "The fifteen cues of the refreshed claws (the kata's three warning/firing pairs, the contact, the six bead notes from one F5 master, the full-meter cadence, the early-click tick, the grasp's warning, success and miss, the squeeze warning and the crush). The crush closes on a soft gong tuned to F, original modal synthesis in the generator itself (`claw_gong`), over a quiet organ.",
        "review": "Claude, 2026-10-03 (deterministic regeneration, length, loudness and true-peak checks); owner audition pending; in-game mix not_run",
    },
}


def group_attribution_section(report, hashes, info):
    table = "\n".join(f"| {k} | {SOURCES[k][0].replace('repo:', '')} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in sorted(hashes))
    head = f"""{info['title']}

{info['about']} [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass pawls, porcelain rings and cracks, additive flue organ, shimmer, low thump), with the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. The recordings below are CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. Loudness follows the Doll weapon tiers (BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip). The audition page stays in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        used = r["sources"]
        external = [k for k in used if not SOURCES[k][0].startswith("repo:")]
        authors = sorted({SOURCES[k][3] for k in external}, key=str.lower)
        creators = (f"recordings by {' and '.join(authors)}; " if authors else "") + \
            "synthesis and layering by Convergence with owner-directed Claude assistance"
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.ogg`
- Asset ID: doll-weapon-sfx-{name.lower()}-{info['date']}
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)
- Creator: {creators}
- Creation/acquisition date: {info['made']}
- Source type: {'public-domain' if external else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} Vorbis at compression level 0.4
- Human modifications: {'trimmed, filtered and layered recordings plus original synthesis' if external else 'original synthesis'}; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS; pinned Ogg serial
- License and redistribution terms: {'CC0 1.0 recordings; the layered cue follows the existing project asset terms' if external else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if external else 'none; retain this provenance'}
- Reviewer and review date: {info['review']}
- SHA256: `{r['ogg_sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


def attribution_section(report, hashes, group="Companion"):
    if group in ATTRIBUTION_GROUPS:
        return group_attribution_section(report, hashes, ATTRIBUTION_GROUPS[group])
    if report and all(CUES[name].group.startswith("Meridian") for name in report):
        return meridian_attribution_section(report, hashes)
    groups = {CUES[name].group for name in report}
    if groups and groups <= set(RECORDS):
        return "\n".join(group_section(group, {k: v for k, v in report.items() if CUES[k].group == group}) for group in sorted(groups))
    table = "\n".join(f"| {k} | {SOURCES[k][0].replace('repo:', '')} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in sorted(hashes))
    stamp, date, review, intro = GROUP_ATTRIBUTION.get(group, ("20261002", "2026-10-02", None, None))
    head = intro + f"""
| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
{table}
""" if intro else f"""### Doll weapon audio foundation and companion summon — 2026-10-02

The shared audio basis of the Doll reward weapon refresh and the companion's new summon cue. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring, additive flue organ, shimmer, low thump); both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. The two Kenney recordings are the CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. The project-owned `DollSummon.wav` (recorded in this register as doll-theater-0253-dollsummon; current bytes from the 0.3.7 weapon articulation revision) is layered thinly at the start so the companion keeps its arrival character. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip. The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        used = r["sources"]
        external = [k for k in used if not SOURCES[k][0].startswith("repo:")]
        masters = [Path(SOURCES[k][0]).stem for k in used if SOURCES[k][0].startswith("repo:")]
        authors = sorted({SOURCES[k][3] for k in external}, key=str.lower)
        creators = "".join((f"recordings by {' and '.join(authors)}; " if authors else "",
                            f"project master{'s' if len(masters) > 1 else ''} {', '.join(masters)} by Convergence; " if masters else "",
                            "synthesis and layering by Convergence with owner-directed Claude assistance"))
        loop = CUES[name].loop
        suffix = "wav" if loop else "ogg"
        kind = (f"stereo 44.1 kHz PCM16 WAV Doll weapon loop ({r['seconds']:.2f} s, {r.get('loop_frames')} frames)" if loop
                else f"stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)")
        encoding = "PCM16 WAV (sample-exact loop)" if loop else "Vorbis at compression level 0.4"
        made = ("periodic original synthesis" if loop else "trimmed, filtered and layered recordings plus original synthesis"
                if used or group == "Companion" else "original synthesis")
        ending = (f"loop wrap step {r.get('wrap_step', 0):.4f} against {r.get('max_inner_step', 0):.4f} inside" if loop
                  else "pinned Ogg serial")
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.{suffix}`
- Asset ID: doll-weapon-sfx-{name.lower()}-{stamp}
- Asset type: {kind}
- Creator: {creators}
- Creation/acquisition date: {date}
- Source type: {'public-domain' if external else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} {encoding}
- Human modifications: {made}; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS; {ending}
- License and redistribution terms: {'CC0 1.0 recordings and project-owned masters; the layered cue follows the existing project asset terms' if external else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if external else 'none; retain this provenance'}
- Reviewer and review date: {review or 'Claude, 2026-10-02 (deterministic regeneration, length, loudness and true-peak checks); owner, 2026-10-03 (approved on the A/B audition page over the Phase I and Phase III music); in-game mix not_run'}
- SHA256: `{r['ogg_sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


def meridian_attribution_section(report, hashes):
    table = "\n".join(f"| {k} | {SOURCES[k][0].replace('repo:', '')} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in sorted(hashes))
    head = f"""### Pale Meridian weapon cues — 2026-10-03

Twenty-three cues of the refreshed Pale Meridian (the music-box siege rifle; [weapon spec](../docs/encounters/first-severance/WEAPONS.md#pale-meridian--refreshed-ranged-2026-10)): twenty-two Vorbis one-shots and one sample-exact PCM16 WAV loop. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, timings (on the weapon's score ticks), loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump), with a few weapon-local blocks in the generator (brass ring, coil-spring twang, band-swept air, a soft-mallet gong resonance for the two closing strikes); the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) are reused unmodified. The three Kenney recordings are CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. The nine notes are pure synthesis, one file per ladder step (never transposed at runtime). Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after encoding (for the loop, including its wrap). The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        used = r["sources"]
        external = [k for k in used if not SOURCES[k][0].startswith("repo:")]
        authors = sorted({SOURCES[k][3] for k in external}, key=str.lower)
        creators = (f"recordings by {' and '.join(authors)}; " if authors else "") + \
            "synthesis and layering by Convergence with owner-directed Claude assistance"
        loop = CUES[name].loop
        kind = (f"stereo 44.1 kHz PCM16 WAV seamless loop, {r['loop_samples']} samples = {r['loop_ticks']:g} game ticks ({r['seconds']:.2f} s)"
                if loop else f"stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)")
        codec = "PCM16 WAV" if loop else "Vorbis at compression level 0.4"
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.{'wav' if loop else 'ogg'}`
- Asset ID: doll-weapon-sfx-{name.lower()}-20261003
- Asset type: {kind}
- Creator: {creators}
- Creation/acquisition date: 2026-10-03
- Source type: {'public-domain' if external else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} {codec}
- Human modifications: {'trimmed, filtered and layered recordings plus original synthesis' if external else 'original synthesis'}; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS; {'circular filtering and an equal-power crossfade of the overhang into the head, no trim or fade' if loop else 'pinned Ogg serial'}
- License and redistribution terms: {'CC0 1.0 recordings; the layered cue follows the existing project asset terms' if external else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if external else 'none; retain this provenance'}
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak{', loop-seam' if loop else ''} and score-tick alignment checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `{r['ogg_sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


# ---------------------------------------------------------------- Last Witness audition (owner listening page, git-ignored)
def witness_settle_curve(x):
    x = np.clip(x, 0, 1)
    return x ** 3 * (10 + x * (-15 + 6 * x))


def witness_schedule(stealth, contact=12, homeward=14):
    """One held score from a cold press to the catch on the WitnessRules ticks: (seconds, cue) events, the contact tick
    and the catch tick. The blade strikes `contact` ticks into its flight and is caught `homeward` ticks after it tears
    free; a stealth score adds the judgement on the struck target."""
    events = [(1, "WitnessSettle")]
    for i, fire in enumerate(W_TESTIMONIES):
        events += [(fire - W_TESTIMONY_LEAD, f"WitnessTestimonyWarn{i + 1}"), (fire, "WitnessTestimonyFire"), (fire + 9, "WitnessShardHit")]
    events += [(W_SEAL, "WitnessSeal"), (W_THROW_WARN, "WitnessThrowWarn"), (W_THROW, "WitnessThrowFire")]
    hit = W_THROW + contact
    catch = hit + W_RETURN + homeward
    events.append((hit, "WitnessAxiomWarn"))
    events += [(hit + bite, f"WitnessAxiomFire{i + 1}") for i, bite in enumerate(W_BITES)]
    events += [(hit + W_RETURN, "WitnessReturnWarn"), (catch, "WitnessReturnFire")]
    if stealth:
        events += [(hit + 1, "VerdictStakeWarn"), (hit + W_LOCK, "VerdictStakeFire"), (hit + W_EXECUTE_WARN, "VerdictExecuteWarn"),
                   (hit + W_EXECUTE, "VerdictExecuteFire"), (hit + W_WITHDRAW, "VerdictWithdraw")]
    return [(tick * W_TICK, name) for tick, name in sorted(events)], hit, catch


def witness_combo(samples, stealth):
    """The score as the client plays it: every cue at its call-site volume on its tick, and the two spin loops under the
    flight with the same gain curves as BladeSource (cruise out and home, the Axiom loop faded in over 4 ticks of the
    turns and out over 6 ticks of the return)."""
    events, hit, catch = witness_schedule(stealth)
    # Long enough for the last tail (the execution rings on for about 2.5 s) plus a short rest.
    mix = seconds(max(at + len(samples[name]) / RATE for at, name in events) + 0.3)
    for at, name in events:
        place(mix, samples[name] * CUES[name].volume, at)
    ticks = np.arange(W_THROW, catch + 1, dtype=float)
    age = ticks - W_THROW
    axiom = np.where(ticks < hit, 0.0, np.where(ticks < hit + W_RETURN, witness_settle_curve((ticks - hit) / 4),
                                                1 - witness_settle_curve((ticks - hit - W_RETURN) / 6)))
    cruise = (1 - axiom) * witness_settle_curve(age / 3)
    start, n = round(W_THROW * W_TICK * RATE), round((catch - W_THROW) * W_TICK * RATE)
    for name, gain in (("WitnessSpinLoop", cruise), ("WitnessAxiomLoop", axiom)):
        loop = samples[name]
        tiled = np.tile(loop, (n // len(loop) + 1, 1))[:n]
        g = np.interp(np.arange(n) / RATE, age * W_TICK, gain) * CUES[name].volume
        mix[start:start + n] += tiled * g[:, None]
    return mix


def witness_audition(directory, samples, report):
    """The owner audition page for Last Witness: every cue alone at its in-game volume (loops looping), the whole score
    from the press to the catch (normal and stealth), and both over a beat-aligned Doll BGM excerpt resampled to 44.1 kHz
    and cross-checked against the original (one common gain, so the relative levels stay the in-game ones)."""
    directory.mkdir(parents=True, exist_ok=True)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    names = [n for n in CUES if CUES[n].group in (WITNESS, VERDICT)]
    clips = {n: wav(n, samples[n] * CUES[n].volume) for n in names}
    combos = {key: witness_combo(samples, key == "stealth") for key in ("normal", "stealth")}
    # Overlapping cues sum past full scale at the in-game volumes: both scores share one trim, so they compare as in game.
    combo_trim = min(0.0, 20 * np.log10(0.97 / max(np.abs(x).max() for x in combos.values())))
    combo_clips = {key: wav(f"combo-{key}", x * 10 ** (combo_trim / 20)) for key, x in combos.items()}
    beds, extra = {}, {}
    for bgm, bpm, origin, first, _, _, label in BGM:
        beat = 60 / bpm
        length = combos["stealth"].shape[0] / RATE
        beats = int(np.ceil(length / beat)) + 2
        start = origin + first * beat
        bed, check = load_bgm(bgm, start, beats * beat)
        outs = {key: overlay(bed, x, (beat,), 1.0) for key, x in combos.items()}
        trim_db = min(0.0, 20 * np.log10(0.97 / max(max(np.abs(o).max() for o in outs.values()), np.abs(bed).max())))
        g = 10 ** (trim_db / 20)
        beds[bgm] = {"label": label, "bed": wav(f"bgm-{bgm}", bed * g),
                     **{key: wav(f"combo-{key}-{bgm}", o * g) for key, o in outs.items()}}
        extra[bgm] = check | {"start": round(start, 4), "beats": beats, "bpm": bpm, "combo_at": round(beat, 4), "mix_trim_db": round(trim_db, 2)}
    extra["combo_trim_db"] = round(combo_trim, 2)
    witness_page(directory, names, clips, combo_clips, beds, report, extra)
    return extra


def witness_page(directory, names, clips, combo_clips, beds, report, extra):
    def audio(src, loop=False):
        return f"<audio controls preload='none' {'loop ' if loop else ''}src='{html.escape(src)}'></audio>"

    def metrics(r):
        return (f"{r['seconds']:.2f} 秒 ・ 短時間 {r['short_term_lufs']:.1f} LUFS ・ 実効 {r['effective_lufs']:.1f} LUFS（音量 {r['volume']}）"
                f" ・ トゥルーピーク {r['true_peak_dbfs']:.1f} dBTP ・ 重心 {r['centroid_hz']} Hz")

    warns = [f"WitnessTestimonyWarn{i}" for i in range(1, 7)]
    bites = [f"WitnessAxiomFire{i}" for i in range(1, 5)]
    sections = (("吊りと6つの証言", ["WitnessSettle", *warns, "WitnessTestimonyFire", "WitnessShardHit"]),
                ("封印と投擲", ["WitnessSeal", "WitnessThrowWarn", "WitnessThrowFire"]),
                ("アクシオムの2回転と帰還", ["WitnessAxiomWarn", *bites, "WitnessAxiomMiss", "WitnessReturnWarn", "WitnessReturnFire",
                                         "WitnessSpinLoop", "WitnessAxiomLoop"]),
                ("ステルスの三角の審判", ["VerdictStakeWarn", "VerdictStakeFire", "VerdictExecuteWarn", "VerdictExecuteFire",
                                    "VerdictExecuteMiss", "VerdictWithdraw"]))
    blocks = []
    for title, members in sections:
        body = [f"<h2>{html.escape(title)}</h2><table>"]
        for n in members:
            if n not in names:
                continue
            loop = CUES[n].loop
            body.append(f"<tr><td><b>{html.escape(n)}</b>{'<br><small>ループ（継ぎ目の確認用に繰り返し再生）</small>' if loop else ''}</td>"
                        f"<td>{audio(clips[n], loop)}</td><td>{html.escape(CUES[n].description)}<br><small>{metrics(report[n])}</small></td></tr>")
        body.append("</table>")
        blocks.append("\n".join(body))
    _, hit, catch = witness_schedule(False)
    combo = (f"<h2>一巡の通し（押してから受け止めまで、ゲーム内の音量）</h2><p>押し始めから {catch / 60:.2f} 秒。"
             f"証言6回（16tick から 29tick ごと）、174tick の封印、196tick からの予兆と 218tick の投擲、"
             f"投擲から {hit - W_THROW}tick で接触、6・12・17・21tick の噛みつき、22tick で引き抜けて {catch - hit - W_RETURN}tick 後に受け止め。"
             f"回転のループは往路・帰路とアクシオムの間で、ゲームと同じ数tickのクロスフェードで入れ替わります。"
             f"重なった音がクリップしないよう、通常とステルスの2本を同じだけ {-extra['combo_trim_db']:.1f} dB 下げています。</p><table>"
             f"<tr><td>通常</td><td>{audio(combo_clips['normal'])}</td></tr>"
             f"<tr><td>ステルス（三角の審判つき）</td><td>{audio(combo_clips['stealth'])}</td></tr></table>")
    bgm_rows = ["<h2>BGM の中で</h2><p>実際の Doll の BGM（48 kHz を 44.1 kHz に変換し、元と相互相関で速さと音程が変わっていないことを確認）に、"
                "拍の頭から一巡を重ねています。効果音と音楽の音量設定はどちらも 100% の想定で、クリップしないよう3本とも同じだけ下げています。</p><table>"]
    for bgm, row in beds.items():
        e = extra[bgm]
        if not isinstance(e, dict):
            continue
        bgm_rows.append(f"<tr><td>{html.escape(row['label'])}<br><small>{e['start']:.1f} 秒から {e['beats']} 拍（{e['bpm']:g} BPM）、"
                        f"{e['combo_at']:.3f} 秒（1拍目）から一巡。{-e['mix_trim_db']:.1f} dB 下げ。</small></td>"
                        f"<td>通常 {audio(row['normal'])}<br>ステルス {audio(row['stealth'])}</td>"
                        f"<td>BGM だけ（ループ） {audio(row['bed'], loop=True)}</td></tr>")
    bgm_rows.append("</table>")
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>最後の証人 効果音</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1200px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>最後の証人（Last Witness）— 新しい効果音</h1>
<p>刷新した最後の証人の全キューです。どれもゲーム内の再生音量（呼び出し側の音量）で書き出しています。予兆（Warn）は発動（Fire）の
直前でちょうど終わる長さで、放つものにはすべて予兆と発動の組があり、外れることのある投擲と処刑には別の外れ音（Miss）があります。</p>
<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）で、実行時の移調はしていません（0 セント）。鐘の音色は使わず、
磁器・真鍮・オルガンで組んでいます。飛んでいる刃のうなりは回転の段ごとの固定ピッチのループ2本（PCM16 WAV、回転数ちょうどで継ぎ目なし）で、
ゲームでは数tickで入れ替えます。</small></p>
{combo}
{''.join(bgm_rows)}
{''.join(blocks)}
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- Last Witness attribution
def witness_attribution_section(report, hashes):
    used = sorted({k for r in report.values() for k in r["sources"]})
    table = "\n".join(f"| {k} | {SOURCES[k][0]} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in used)
    head = f"""### Last Witness v2 audio — 2026-10-03

The cues of the refreshed Last Witness ([weapon spec](../docs/encounters/first-severance/WEAPONS.md#rogue--last-witness)): the hang, six testimonies, the seal, the throw, the Axiom turns, the return and catch, two fixed-pitch spin loops and the stealth Triangle Judgement. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; the music-box teeth, porcelain, brass, organ and shimmer are the original synthesis of [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py), with the Ebon helpers imported unmodified. The recordings are CC0 files already recorded in the Ebon Manor and Ebon reward tables of this register (Kenney RPG Audio on OpenGameArt; Freesound uploads that showed Creative Commons 0 on 2026-10-01, public HQ preview renders); they stay in the local store, are SHA-256 verified before use and are not committed. One-shots are Vorbis; the two loops are PCM16 WAV of a whole number of blade revolutions (`WitnessSpinLoop` 82,100 samples = 8 turns at 0.45 rad/tick, `WitnessAxiomLoop` 98,942 samples = 16 turns at 0.747 rad/tick), built circularly so they repeat sample-exactly. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after encoding. The audition page and report stay in the git-ignored `.local`.

| Key | Store file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        loop = CUES[name].loop
        used = r["sources"]
        authors = sorted({SOURCES[k][3] for k in used}, key=str.lower)
        creators = (f"recordings by {', '.join(authors)}; " if authors else "") + "synthesis and layering by Convergence with owner-directed Claude assistance"
        kind = (f"stereo 44.1 kHz PCM16 WAV Doll weapon loop ({r['seconds']:.3f} s, {round(r['seconds'] * RATE)} samples)" if loop
                else f"stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)")
        encoding = "PCM16 WAV" if loop else "Vorbis at compression level 0.4"
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.{'wav' if loop else 'ogg'}`
- Asset ID: doll-weapon-sfx-{name.lower()}-20261003
- Asset type: {kind}
- Creator: {creators}
- Creation/acquisition date: 2026-10-03
- Source type: {'public-domain' if used else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} {encoding}
- Human modifications: {'trimmed, filtered and layered recordings plus original synthesis' if used else 'original synthesis'}; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS{'; built circularly, so the file repeats sample-exactly' if loop else '; pinned Ogg serial'}
- License and redistribution terms: {'CC0 1.0 recordings; the layered cue follows the existing project asset terms' if used else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if used else 'none; retain this provenance'}
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak{', loop-seam' if loop else ''} checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `{r['sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


def weapon(group):
    """The weapon an audition group belongs to: Pale Meridian's groups are "Meridian build", "Meridian notes", ...;
    Last Witness's are "Last Witness" and its "Last Witness: Triangle Judgement"."""
    return "Witness" if group in (WITNESS, VERDICT) else group.split()[0]


def group_section(group, report):
    """An original-synthesis register section (no recordings) for one cue group."""
    date, stamp, review, title, prose = RECORDS[group]
    blocks = []
    for name, r in report.items():
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/DollWeapons/{name}.ogg`
- Asset ID: doll-weapon-sfx-{name.lower()}-{stamp}
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue ({r['seconds']:.2f} s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: {date}
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} Vorbis at compression level 0.4
- Human modifications: original synthesis and layering; short-term loudness {r['short_term_lufs']:.1f} LUFS (played at volume {r['volume']}: {r['effective_lufs']:.1f} LUFS effective), true peak {r['true_peak_dbfs']:.1f} dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: {review}
- SHA256: `{r['ogg_sha256']}`
""")
    return f"### {title}\n\n{prose}\n\n" + "\n".join(blocks)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--store", type=Path, help="local recording store (default: $CONVERGENCE_AUDIO_STORE or the "
                                                    "nearest ancestor .claude-tools); never committed")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR, help="Ogg output folder")
    parser.add_argument("--preview", type=Path, help="write the owner audition page, WAVs and report here (.local)")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md section here")
    parser.add_argument("--witness-audition", type=Path, help="write the Last Witness owner audition page and WAVs here (.local)")
    parser.add_argument("--witness-attribution", type=Path, help="write the Last Witness Assets/ATTRIBUTION.md section here")
    parser.add_argument("--previous", type=Path, help="folder of an earlier export (.local); cues that changed get an A/B "
                                                      "row with it on the audition page")
    parser.add_argument("--only", help="comma-separated cue names; each cue is seeded by its own name, so the bytes "
                                       "equal a full run")
    parser.add_argument("--group", help="render one weapon's cues (Companion, Lacuna, Meridian: every 'Meridian ...' "
                                        "group, Witness: Last Witness and its Triangle Judgement, Choir, Claws); --preview and "
                                        "--attribution-section describe one weapon")
    args = parser.parse_args()
    names = ([n.strip() for n in args.only.split(",")] if args.only
             else [n for n in CUES if not args.group or weapon(CUES[n].group) == args.group])
    unknown = [n for n in names if n not in CUES]
    if unknown or not names:
        parser.error("unknown cue(s): " + ", ".join(unknown) if unknown else "no cue in that group")
    groups = {weapon(CUES[n].group) for n in names}
    if (args.preview or args.attribution_section) and len(groups) != 1:
        parser.error("--preview and --attribution-section describe one weapon: pass --group")
    store = Store(args.store or find_store(), ATTRIBUTION.read_text(encoding="utf-8"))
    args.output.mkdir(parents=True, exist_ok=True)
    samples, report = {}, {}
    for name in names:
        samples[name], used = render_cue(name, store, args.output)
        report[name] = analyse(name, samples[name], cue_path(name, args.output), used)
    record = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__).read_bytes()),
              "dsp_sha256": sha256(Path(dsp.__file__).read_bytes()),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {k: {"path": SOURCES[k][0], "origin": SOURCES[k][1], "sha256": store.hashes[k]} for k in sorted(store.hashes)},
              "cues": report}
    group = next(iter(groups))
    if args.preview:
        record["audition"] = (claw_preview(args.preview, group, samples, report) if group in WEAPON_PAGES
                              else weapon_preview(args.preview, group, samples, report) if group in COMBOS
                              else witness_audition(args.preview, samples, report) if group == "Witness"
                              else preview(args.preview, samples, report, store, args.previous))
        (args.preview / "doll-weapon-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        section = (witness_attribution_section(report, store.hashes) if group == "Witness"
                   else attribution_section(report, store.hashes, group))
        args.attribution_section.write_text(section, encoding="utf-8", newline="\n")
    witness = {k: v for k, v in report.items() if CUES[k].group in (WITNESS, VERDICT)}
    if args.witness_audition and witness:
        record["witness_audition"] = witness_audition(args.witness_audition, samples, witness)
        (args.witness_audition / "witness-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.witness_attribution and witness:
        args.witness_attribution.write_text(witness_attribution_section(witness, store.hashes), encoding="utf-8", newline="\n")
    print(json.dumps({k: {kk: v[kk] for kk in ("seconds", "short_term_lufs", "effective_lufs", "true_peak_dbfs", "peak_ms",
                                              "centroid_hz", "bytes")} for k, v in report.items()}, indent=1))


if __name__ == "__main__":
    main()
