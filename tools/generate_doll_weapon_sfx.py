"""Doll reward weapon cues and the companion summon (shared audio basis of the Doll weapon refresh).

Renders Assets/Sounds/Weapons/DollWeapons/<Cue>.ogg (a loop: <Cue>.wav, PCM16, sample-exact). Every cue is one builder function registered
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
     [--preview DIR] [--attribution-section FILE] [--previous DIR]
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
    "metal_pot": (KENNEY + "!OGG/metalPot1.ogg",
                  "Kenney RPG Audio metalPot1.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0)",
                  "159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd", "Kenney"),
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
    loop: bool = False   # a sample-exact PCM16 WAV loop, not an Ogg one-shot (never trimmed or faded; filtered circularly)
    # How a loop is mastered: "round" (render_loop_cue: exactly max_seconds, loudness and true peak over the loop
    # played round) or "period" (master() on one period like a one-shot, true peak including the wrap).
    loop_master: str = "round"


CUES = {}


def cue(name, target_lufs, group, max_seconds, volume, description, glue=1.6, loop_master="round", loop=False):
    def register(build):
        if name in CUES:
            raise ValueError(f"duplicate cue {name}")
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


def render_cue(name, store, output):
    """Master to the cue's target, write the Ogg (or the WAV loop) and, if the decoded file overshoots -1 dBTP, back
    the gain off. A "round" loop is mastered by render_loop_cue."""
    if CUES[name].loop and CUES[name].loop_master == "round":
        return render_loop_cue(name, store, output)
    store.used = set()
    spec = CUES[name]
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


# ---------------------------------------------------------------- attribution
# Per group: the records' date stamp and date, their review line and the section's introduction (heading included).
LACUNA_ATTRIBUTION = """### Lacuna Testament cues — 2026-10-03

The seventeen cues of the refreshed Lacuna Testament (the magic Doll reward weapon): sixteen stereo Vorbis one-shots and one sample-exact stereo PCM16 WAV loop of exactly 176,400 frames (4.0 s, eight of the beam's 30-tick visual pulse periods). [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns each cue's recipe, its baked beats against the weapon's tick schedule (mirrored from `LacunaTestamentScore` and pinned by `tools/tests/test_doll_weapon_audio.py`), the loudness tiers and the source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump; the generator adds a gong-like plate tuned into the key, also additive synthesis), and both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. Every layer is original synthesis except one Kenney recording (`metalLatch`, the CC0 1.0 file already recorded in the Ebon Manor reward audio table of this register) under the great aperture's clank; it stays in the local store, is SHA-256 verified before use and is not committed. The loop is periodic by construction (whole cycles on its 0.25 Hz grid, FFT-synthesised noise on its own bins, circularly placed tings, a high-pass over three periods), so its wrap is as smooth as its inside. Loudness follows the Ebon scale (BS.1770 K-weighted maximum 400 ms short-term LUFS; true peak at most -1 dBTP after the Vorbis round trip, or over the loop played round). Nothing is transposed at runtime except the two single-pitch cues (`LacunaIrisTine`, `LacunaPelletFire`, every pitched layer a C, recorded at C6 and played on the ladder step of each iris); every composite cue and the loop play as rendered. The audition page and report stay in the git-ignored `.local`.
"""
GROUP_ATTRIBUTION = {
    "Lacuna": ("20261003", "2026-10-03", "Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam "
               "checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run", LACUNA_ATTRIBUTION),
}


def attribution_section(report, hashes, group="Companion"):
    if report and all(CUES[name].group.startswith("Meridian") for name in report):
        return meridian_attribution_section(report, hashes)
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


def weapon(group):
    """The weapon an audition group belongs to: Pale Meridian's groups are "Meridian build", "Meridian notes", ..."""
    return group.split()[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--store", type=Path, help="local recording store (default: $CONVERGENCE_AUDIO_STORE or the "
                                                    "nearest ancestor .claude-tools); never committed")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR, help="Ogg output folder")
    parser.add_argument("--preview", type=Path, help="write the owner audition page, WAVs and report here (.local)")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md section here")
    parser.add_argument("--previous", type=Path, help="folder of an earlier export (.local); cues that changed get an A/B "
                                                      "row with it on the audition page")
    parser.add_argument("--only", help="comma-separated cue names; each cue is seeded by its own name, so the bytes "
                                       "equal a full run")
    parser.add_argument("--group", help="render one weapon's cues (Companion, Lacuna, Meridian: every 'Meridian ...' "
                                        "group); --preview and --attribution-section describe one weapon")
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
        record["audition"] = (weapon_preview(args.preview, group, samples, report) if group in COMBOS
                              else preview(args.preview, samples, report, store, args.previous))
        (args.preview / "doll-weapon-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        args.attribution_section.write_text(attribution_section(report, store.hashes, group), encoding="utf-8", newline="\n")
    print(json.dumps({k: {kk: v[kk] for kk in ("seconds", "short_term_lufs", "effective_lufs", "true_peak_dbfs", "peak_ms",
                                              "centroid_hz", "bytes")} for k, v in report.items()}, indent=1))


if __name__ == "__main__":
    main()
