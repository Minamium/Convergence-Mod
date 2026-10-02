"""Doll reward weapon cues and the companion summon (shared audio basis of the Doll weapon refresh).

Renders Assets/Sounds/Weapons/DollWeapons/<Cue>.ogg. Every cue is one builder function registered
with @cue in CUES (short-term loudness target, group, maximum length, in-game playback volume and
the owner audition text); later weapon changes add their cues here and reuse the building blocks
of tools/doll_sfx_dsp.py (music-box comb tooth on the F minor pentatonic ladder, brass ratchet,
porcelain ring and crack, additive organ pad, shimmer, low thump).

Recordings come only from the pinned SOURCES table: CC0 recordings already attributed for the
Ebon and Cathedral cues, read from the local store (--store, never committed) and project-owned
masters (repo:). Each is SHA-256 checked before use; a store recording must also be listed with
that hash in Assets/ATTRIBUTION.md, a project master by its runtime path.

Loudness is the Ebon scale (generate_ebon_reward_sfx helpers, imported unmodified): BS.1770
K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip.
Deterministic: a cue's random stream is seeded from its name and the Ogg stream serial is pinned
from the file stem (generate_ebon_sfx.write_ogg), so a rerun, or --only, is byte-identical.
Requires numpy, scipy and soundfile (local audio tools, not CI).

Run: py -3.12 tools/generate_doll_weapon_sfx.py [--store DIR] [--only CUE,...] [--output DIR]
     [--preview DIR] [--attribution-section FILE]
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
    "doll_summon": ("repo:Assets/Sounds/Weapons/DollTheater/DollSummon.wav",
                    "Convergence project asset doll-theater-0253-dollsummon (0.3.7 articulation revision)",
                    "f4be1a0733e06ad0262d6703a44fc00a0a01dd9a17a3fb1a3381831d634e8534", "Convergence"),
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
    loop: bool = False   # a sample-exact PCM16 WAV loop of exactly max_seconds (render_loop), not an Ogg one-shot


CUES = {}


def cue(name, target_lufs, group, max_seconds, volume, description, glue=1.6, loop=False):
    def register(build):
        if name in CUES:
            raise ValueError(f"duplicate cue {name}")
        CUES[name] = Cue(build, target_lufs, group, max_seconds, volume, description, glue, loop)
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
# The single-note cues (LacunaIrisFire, LacunaPelletFire) are recorded at ladder step 3 (C6) and played at the
# iris's step through DollWeaponAudio.Note: F5 Ab5 Bb5 C6 Eb6 F6 Ab6 as the seven irises join.
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


@cue("LacunaIrisWarn", -17, "Lacuna", 0.45, 0.7,
     "虹彩が一つ生まれる（第1〜7の虹彩それぞれの誕生の瞬間）。遺言書の穴から磁器の花弁が滑り出す細いこすれ音に、"
     "+8tick（0.13秒）で真鍮の留め金が一段、+11tickでもう半段かかる。開く準備の予告音で、開く瞬間は LacunaIrisFire。")
def lacuna_iris_warn(s, rng):
    mix = seconds(0.5)
    place(mix, pan(_sweep(0.12, 2200, 5200, rng, bands=8) * np.linspace(0.3, 1, round(0.12 * RATE))[:, None], -0.15), 0.0, -14)
    for k, at in enumerate((0.012, 0.05, 0.088)):
        place(mix, dsp.porcelain_ring(3900 + 520 * k, 0.05, rng, decay=0.008, side=-0.3 + 0.25 * k), at, -16 - 2 * k)
    place(mix, pan(dsp.brass_click(rng, 2350, decay=0.005, thud=0.45), -0.1), _ticks(LACUNA["frame_parting"]), -4)
    place(mix, dsp.porcelain_ring(4400, 0.06, rng, decay=0.01, side=0.1), _ticks(LACUNA["frame_parting"]) + 0.002, -13)
    place(mix, pan(dsp.brass_click(rng, 2600, decay=0.004, thud=0.25), 0.05), _ticks(LACUNA["frame_half"]), -9)
    return room(mix, 0.1, 0.42, 0.08)


@cue("LacunaIrisFire", -17, "Lacuna", 0.7, 0.8,
     "虹彩が開ききって最初の弾を撃つ（誕生から15tick）。磁器の開放音「カッ」とオルゴールの爪一音（C6 で録り、"
     "ゲームでは虹彩の順に F5 A♭5 B♭5 C6 E♭6 F6 A♭6 と一段ずつ上げて鳴らす）、下に虚無の小さな「ぽっ」。")
def lacuna_iris_fire(s, rng):
    mix = seconds(0.75)
    place(mix, dsp.porcelain_ring(3150, 0.12, rng, decay=0.018, side=-0.1), 0.0, -6)
    place(mix, dsp.porcelain_ring(4380, 0.1, rng, decay=0.012, side=0.15), 0.003, -10)
    place(mix, pan(dsp.brass_click(rng, 1900, decay=0.006, thud=0.7), 0.0), 0.0, -9)
    place(mix, pan(dsp.box_tine(dsp.hz("C6"), 0.7, rng), 0.05), 0.006, -2)
    place(mix, _puff(0.14, rng, 420), 0.004, -15)
    return room(mix, 0.12, 0.66, 0.12)


@cue("LacunaPelletWarn", -20, "Lacuna", 0.2, 0.55,
     "次の弾の4tick前、花弁が半分閉じる合図。真鍮のシャッターの小さな「トッ」。直後の LacunaPelletFire とで一組。")
def lacuna_pellet_warn(s, rng):
    mix = seconds(0.2)
    place(mix, pan(dsp.brass_click(rng, 2950, decay=0.0032, thud=0.2, tick=0.7), 0.1), 0.0, -2)
    place(mix, dsp.porcelain_ring(5200, 0.04, rng, decay=0.006, side=-0.1), 0.004, -14)
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
    place(mix, dsp.porcelain_ring(930, 0.12, rng, decay=0.02, side=0.0), 0.0, -3)
    place(mix, dsp.porcelain_ring(2150, 0.06, rng, decay=0.008, side=0.2), 0.001, -12)
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


@cue("LacunaBeamFire", -10, "Lacuna", 1.6, 0.9,
     "黒い芯の光線が開く（410tick、溜めの静寂のあと）。60ミリ秒の吸い込みから、深いオルガンの和音 Fm7（F2 C3 E♭3 A♭3 C4）"
     "の一撃、60→35 Hz の沈み込み、磁器のきらめき。最大の山場（T4）。")
def lacuna_beam_fire(s, rng):
    mix = seconds(1.8)
    inhale = 0.06
    place(mix, pan(_inhale(inhale, rng), 0.0), 0.0, -10)
    stab = dsp.organ_pad([dsp.hz(n) for n in ("F2", "C3", "Eb3", "Ab3", "C4")], 1.25, rng, attack=0.012, release=0.75,
                         harmonics=14, rolloff=1.05, chiff=0.08, breath=0.18)
    place(mix, stab, inhale, -1)
    place(mix, dsp.thump(60, 35, 0.9, rng), inhale, -2)
    place(mix, dsp.porcelain_crack(0.12, rng, count=7, spread=0.035, low=2400, high=7200), inhale, -12)
    place(mix, dsp.shimmer(0.8, rng, count=11), inhale + 0.02, -11)
    place(mix, dsp.porcelain_ring(dsp.hz("F6"), 0.9, rng, decay=0.25, side=0.2), inhale + 0.004, -12)
    return room(mix, 0.2, 1.55, 0.4)


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
    place(mix, dsp.porcelain_ring(430, 0.2, rng, decay=0.05, side=0.0), 0.0, -3)
    place(mix, dsp.porcelain_ring(1290, 0.1, rng, decay=0.02, side=0.15), 0.002, -11)
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
     "マナが尽きて儀式が崩れる（失敗）。オルガンが途切れ途切れに3回つまずき、磁器がひび割れ、音程が F3 から C3 へ"
     "沈みながら消える。放したときの LacunaBeamEnd より低く暗い。")
def lacuna_beam_miss(s, rng):
    mix = seconds(0.95)
    for k, (at, dur) in enumerate(((0.0, 0.07), (0.1, 0.06), (0.19, 0.09))):
        blip = dsp.organ_pad([dsp.hz("F3") * 2 ** (-k * 1.5 / 12), dsp.hz("C4") * 2 ** (-k * 1.5 / 12)], dur, rng, attack=0.006,
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


# ---------------------------------------------------------------- render
def seed(name):
    return int.from_bytes(hashlib.sha256((SEED_PREFIX + name).encode("utf-8")).digest()[:8], "little")


def render(name, store):
    x = trim(hp(CUES[name].build(store, np.random.default_rng(seed(name))), 28))
    limit = round(CUES[name].max_seconds * RATE)
    if len(x) > limit:
        raise RuntimeError(f"{name}: {len(x) / RATE:.3f} s exceeds its {CUES[name].max_seconds} s budget")
    return x


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


def render_cue(name, store, output):
    """Master to the cue's target, write the Ogg and, if the Vorbis round trip overshoots -1 dBTP, back the gain off."""
    if CUES[name].loop:
        return render_loop_cue(name, store, output)
    store.used = set()
    spec = CUES[name]
    path = output / f"{name}.ogg"
    mastered = master(render(name, store), spec.target_lufs, spec.glue)
    for _ in range(8):
        decoded = base.write_ogg(path, mastered)
        peak = true_peak_db(decoded)
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
    looped = np.concatenate((decoded, decoded)) if spec.loop else decoded
    info = measure(decoded) | {
        "short_term_lufs": round(loudness(looped), 2),
        "true_peak_dbfs": round(loop_peak_db(decoded) if spec.loop else true_peak_db(decoded), 2),
        "target_lufs": spec.target_lufs,
        "max_seconds": spec.max_seconds,
        "volume": spec.volume,
        "effective_lufs": round(loudness(looped) + 20 * np.log10(spec.volume), 2),
        "sources": sources_used,
        "bytes": len(data),
        "ogg_serial": None if spec.loop else ogg_serial(data),
        "ogg_sha256": sha256(data),
    } | ({"loop_frames": len(decoded)} | seam(decoded) if spec.loop else {})
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


def preview(directory, samples, report, store):
    directory.mkdir(parents=True, exist_ok=True)

    def wav(name, x):
        peak = np.abs(x).max()
        if peak > 0.999:
            raise RuntimeError(f"audition clip {name} clips ({peak:.3f})")
        sf.write(str(directory / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        return f"{name}.wav"

    rows, extra = [], {}
    for name, x in samples.items():
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
    page(directory, rows, report, extra)
    return extra


def page(directory, rows, report, extra):
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
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>人形の相棒 召喚音</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#15121a;color:#ece4dc;margin:24px;max-width:1100px}}
td{{padding:8px 12px;vertical-align:top;border-bottom:1px solid #2c2633}}small{{color:#a99fb0}}table{{border-collapse:collapse;width:100%}}
h1,h2,h3{{font-weight:600}}h2{{margin-top:1.6em;color:#eed9c4}}audio{{height:32px;width:280px}}p{{line-height:1.6}}</style>
<h1>人形の相棒 — 新しい召喚音</h1>
<p>変わるのは召喚の瞬間の音だけです（同じきっかけで1回鳴る）。旧 DollSummon と、ゲーム内の音量で聴き比べてください。
BGM の行は、実際の Doll の BGM（44.1 kHz に変換し、元の速さと音程のまま）に、ゲーム内の音量で重ねています（効果音と音楽の音量設定はどちらも 100%）。</p>
<p><small>調は全武器共通の F マイナー・ペンタトニック（F A♭ B♭ C E♭）で、BGM に合わせた音程の補正はしていません（0 セント）。
第3相の BGM はこの調の基準ですが、第1相は約2半音低い調で鳴っているので、そこでのなじみ方も確かめてください。</small></p>
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
        events.append((first, "LacunaIrisFire", i))
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
（F A♭ B♭ C E♭）で、BGM に合わせた音程補正はしていません（0 セント）。一音だけの LacunaIrisFire と LacunaPelletFire は C6 で録り、
ゲームでは虹彩の順に F5 A♭5 B♭5 C6 E♭6 F6 A♭6 の高さで鳴ります（コンボではその高さで並べています）。</small></p>
<h2>通しで（ゲームと同じタイミング）</h2><table>{''.join(combo_rows)}</table>
<h2>BGM に重ねて</h2><table>{''.join(bgm_rows)}</table>
<h2>一つずつ</h2><table>{''.join(rows)}</table>
</html>""", encoding="utf-8", newline="\n")


# ---------------------------------------------------------------- attribution
# Per group: the records' date stamp and date, their review line and the section's introduction (heading included).
LACUNA_ATTRIBUTION = """### Lacuna Testament cues — 2026-10-03

The sixteen cues of the refreshed Lacuna Testament (the magic Doll reward weapon): fifteen stereo Vorbis one-shots and one sample-exact stereo PCM16 WAV loop of exactly 176,400 frames (4.0 s, eight of the beam's 30-tick visual pulse periods). [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns each cue's recipe, its baked beats against the weapon's tick schedule (mirrored from `LacunaTestamentScore` and pinned by `tools/tests/test_doll_weapon_audio.py`), the loudness tiers and the source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump), and both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. Every layer is original synthesis except one Kenney recording (`metalLatch`, the CC0 1.0 file already recorded in the Ebon Manor reward audio table of this register) under the great aperture's clank; it stays in the local store, is SHA-256 verified before use and is not committed. The loop is periodic by construction (whole cycles on its 0.25 Hz grid, FFT-synthesised noise on its own bins, circularly placed tings, a high-pass over three periods), so its wrap is as smooth as its inside. Loudness follows the Ebon scale (BS.1770 K-weighted maximum 400 ms short-term LUFS; true peak at most -1 dBTP after the Vorbis round trip, or over the loop played round). Nothing is transposed at runtime except the two single-note cues (`LacunaIrisFire`, `LacunaPelletFire`, recorded at C6 and played on the ladder step of each iris). The audition page and report stay in the git-ignored `.local`.
"""
GROUP_ATTRIBUTION = {
    "Lacuna": ("20261003", "2026-10-03", "Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam "
               "checks); owner audition pending; in-game mix not_run", LACUNA_ATTRIBUTION),
}


def attribution_section(report, hashes, group="Companion"):
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


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--store", type=Path, help="local recording store (default: $CONVERGENCE_AUDIO_STORE or the "
                                                    "nearest ancestor .claude-tools); never committed")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR, help="Ogg output folder")
    parser.add_argument("--preview", type=Path, help="write the owner audition page, WAVs and report here (.local)")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md section here")
    parser.add_argument("--only", help="comma-separated cue names; each cue is seeded by its own name, so the bytes "
                                       "equal a full run")
    parser.add_argument("--group", help="render one group's cues (Companion, Lacuna); --preview and --attribution-section "
                                        "describe one group")
    args = parser.parse_args()
    names = ([n.strip() for n in args.only.split(",")] if args.only
             else [n for n in CUES if not args.group or CUES[n].group == args.group])
    unknown = [n for n in names if n not in CUES]
    if unknown or not names:
        parser.error("unknown cue(s): " + ", ".join(unknown) if unknown else "no cue in that group")
    groups = {CUES[n].group for n in names}
    if (args.preview or args.attribution_section) and len(groups) != 1:
        parser.error("--preview and --attribution-section describe one group: pass --group")
    store = Store(args.store or find_store(), ATTRIBUTION.read_text(encoding="utf-8"))
    args.output.mkdir(parents=True, exist_ok=True)
    samples, report = {}, {}
    for name in names:
        samples[name], used = render_cue(name, store, args.output)
        report[name] = analyse(name, samples[name], args.output / f"{name}.{'wav' if CUES[name].loop else 'ogg'}", used)
    record = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__).read_bytes()),
              "dsp_sha256": sha256(Path(dsp.__file__).read_bytes()),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {k: {"path": SOURCES[k][0], "origin": SOURCES[k][1], "sha256": store.hashes[k]} for k in sorted(store.hashes)},
              "cues": report}
    group = next(iter(groups))
    if args.preview:
        record["audition"] = (weapon_preview(args.preview, group, samples, report) if group in COMBOS
                              else preview(args.preview, samples, report, store))
        (args.preview / "doll-weapon-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        args.attribution_section.write_text(attribution_section(report, store.hashes, group), encoding="utf-8", newline="\n")
    print(json.dumps({k: {kk: v[kk] for kk in ("seconds", "short_term_lufs", "effective_lufs", "true_peak_dbfs", "peak_ms",
                                              "centroid_hz", "bytes")} for k, v in report.items()}, indent=1))


if __name__ == "__main__":
    main()
