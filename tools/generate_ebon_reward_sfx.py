"""Weapon cues for the Waltz of the Ebon Manor reward set (docs/encounters/ebon-manor/REWARDS.md).

Renders HatboxOpen, Note0-7 (B3..D6 silk plucks), the Moonshear / Moonloom Harp /
Ebon Thimble / Ballroom Chandelier / Severing Silk cues and WaltzOpen into
Assets/Sounds/Weapons/EbonRewards/. Layers trimmed CC0 recordings (the Ebon Manor
and Soboro source tables in Assets/ATTRIBUTION.md, read from the local --sources
store, SHA-256 checked before use, never committed) with original synthesis:
Karplus-Strong silk strings tuned to B minor (AutoMatador's key), modal glass,
chain links, a struck-string piano, fabric rips, band-swept air and thread ratchets.
Shared helpers come from generate_ebon_sfx.py (unmodified). Loudness is that
script's scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak
at most -1 dBTP after the Vorbis round trip. Deterministic: cue seeds derive from
the cue name and the Ogg stream serials are pinned. Requires numpy, scipy and
soundfile (local audio tools, not CI).
Run: py -3.12 tools/generate_ebon_reward_sfx.py --sources <cc0 store> [--preview DIR] [--attribution-section FILE]
"""
import argparse
import hashlib
import html
import json
import sys
import zipfile
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_ebon_sfx as base  # noqa: E402  (shared helpers; its CLI only runs as __main__)
from generate_ebon_sfx import (RATE, chain, chime, env, fades, glass, hall, hp, hz, lp,  # noqa: E402
                               mono, noise, pan, place, pluck, rip, seconds, sha256, speed, thump, trim)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "Weapons" / "EbonRewards"
ATTRIBUTION = ROOT / "Assets" / "ATTRIBUTION.md"
BEAT, SIXTEENTH, TICK = 60 / 125, 60 / 125 / 4, 1 / 60  # AutoMatador's 125 BPM; 7.2 ticks = one sixteenth

# Store path (zip member after '!'), origin and SHA-256 of every recording the cues read. All are
# listed in Assets/ATTRIBUTION.md (Ebon Manor table; Soboro table for energy_wave .. zap).
KENNEY = "pack-OGA-Kenney-RPGsounds.zip"
SOURCES = {
    "air_cut": ("wind-FS60030-qubodup-air_cut.mp3", "https://freesound.org/s/60030/ (qubodup, HQ preview)",
                "0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0"),
    "book_flip": (KENNEY + "!OGG/bookFlip3.ogg", "Kenney RPG Audio bookFlip3.ogg",
                  "c85db5dceb3f1df073e960630277eaa88a5afda0477c1ddd68dad707621767be"),
    "chop": (KENNEY + "!OGG/chop.ogg", "Kenney RPG Audio chop.ogg",
             "d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486"),
    "cloth1": (KENNEY + "!OGG/cloth1.ogg", "Kenney RPG Audio cloth1.ogg",
               "ddb93a3671233f95da0e0b10367f082f7eb42fa6caaddcf776410aa8833c747d"),
    "cloth4": (KENNEY + "!OGG/cloth4.ogg", "Kenney RPG Audio cloth4.ogg",
               "e7ab9a6c4466dea874196c61f59bf1da05cfe58748f42fadd695d441a154a99b"),
    "creak1": (KENNEY + "!OGG/creak1.ogg", "Kenney RPG Audio creak1.ogg",
               "8a346186fd297254248cab8e8117060a52a5cf2a84f603153a762108550ea95e"),
    "creak2": (KENNEY + "!OGG/creak2.ogg", "Kenney RPG Audio creak2.ogg",
               "8a990afdc03aebb91d528f5385e2f95582dbfa8e2c12c71098ab01be9142294a"),
    "door_close": (KENNEY + "!OGG/doorClose_4.ogg", "Kenney RPG Audio doorClose_4.ogg",
                   "fd21c0e7a9d0317375d2561590f0770dd3380ee35507d064862cb44d6f71595b"),
    "draw_knife": (KENNEY + "!OGG/drawKnife3.ogg", "Kenney RPG Audio drawKnife3.ogg",
                   "a11ae62fb1a628425769d11a9de394980ad8909c31f4c9a4316f226963e21caf"),
    "knife_slice": (KENNEY + "!OGG/knifeSlice2.ogg", "Kenney RPG Audio knifeSlice2.ogg",
                    "6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def"),
    "low_impact": ("impact-FS541029-AudioPapkin-very_low_impact.mp3", "https://freesound.org/s/541029/ (AudioPapkin, HQ preview)",
                   "73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a"),
    "metal_click": (KENNEY + "!OGG/metalClick.ogg", "Kenney RPG Audio metalClick.ogg",
                    "9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3"),
    "metal_latch": (KENNEY + "!OGG/metalLatch.ogg", "Kenney RPG Audio metalLatch.ogg",
                    "ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708"),
    "metal_pot": (KENNEY + "!OGG/metalPot1.ogg", "Kenney RPG Audio metalPot1.ogg",
                  "159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd"),
    "rock_tumble": ("impact-FS389618-_stubb-rock_tumble_2.mp3", "https://freesound.org/s/389618/ (_stubb, HQ preview)",
                    "199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a"),
    "swish": ("swishes/swish-4.wav", "https://opengameart.org/content/swishes-sound-pack (artisticdude, swish-4.wav)",
              "0060f4a7040edce4cc50d1daa10a9cb76764128a942e4688339e69cd1d5d784c"),
    "swoosh": ("swing-FS263595-PorkMuncher-swoosh.mp3", "https://freesound.org/s/263595/ (PorkMuncher, HQ preview)",
               "5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058"),
    "woosh": ("wind-FS683096-florianreichelt-woosh.mp3", "https://freesound.org/s/683096/ (florianreichelt, HQ preview)",
              "3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265"),
    "energy_wave": ("swing-FS724716-greyfeather-sword_slash_energy_wave.mp3", "https://freesound.org/s/724716/ (greyfeather, HQ preview)",
                    "5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59"),
    "samurai_slash": ("swing-FS370204-nekoninja-samurai_slash.mp3", "https://freesound.org/s/370204/ (nekoninja, HQ preview)",
                      "283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2"),
    "stick_woosh": ("swing-FS352719-Dalesome-woosh_stick.mp3", "https://freesound.org/s/352719/ (Dalesome, HQ preview)",
                    "5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016"),
    "anime_shing": ("ring-FS529019-Euphrosyyn-anime_shing_sword_2.mp3", "https://freesound.org/s/529019/ (Euphrosyyn, HQ preview)",
                    "a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f"),
    "anime_ring": ("ring-FS706204-xkeril-nice_anime_sword_hit.mp3", "https://freesound.org/s/706204/ (xkeril, HQ preview)",
                   "2a28c06b3674240e46bbf79516f87e5fbbe9522a1d272b55448f0af2c8599137"),
    "sword_hit": ("metal-FS442769-qubodup-sword_hit.mp3", "https://freesound.org/s/442769/ (qubodup, HQ preview)",
                  "93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167"),
    "armor_strike": ("metal-FS568170-Merrick079-sword_sound_1.mp3", "https://freesound.org/s/568170/ (Merrick079, HQ preview)",
                     "5f9ab16b7a74a205b1490001d4c913d0d55f561df796cb2d43c1a30b97c351b1"),
}
AUTHORS = {"air_cut": "qubodup", "low_impact": "AudioPapkin", "rock_tumble": "_stubb", "swish": "artisticdude",
           "swoosh": "PorkMuncher", "woosh": "florianreichelt", "energy_wave": "greyfeather", "samurai_slash": "nekoninja",
           "stick_woosh": "Dalesome", "anime_shing": "Euphrosyyn", "anime_ring": "xkeril", "sword_hit": "qubodup",
           "armor_strike": "Merrick079"}


class Store(base.Store):
    """base.Store plus this script's source table: the SHA-256 must match and be attributed."""

    def __init__(self, root, attribution_text):
        super().__init__(root)
        self.attribution = attribution_text
        self.used = set()  # keys read by the cue being rendered

    def get(self, key):
        self.used.add(key)
        return super().get(key)

    def raw(self, key):
        path, _, expected = SOURCES[key]
        if "!" in path:
            archive, member = path.split("!", 1)
            data = zipfile.ZipFile(self.root / archive).read(member)
        else:
            data = (self.root / path).read_bytes()
        found = sha256(data)
        if found != expected:
            raise RuntimeError(f"Source hash mismatch for {key}: {found} != {expected}")
        if expected not in self.attribution:
            raise RuntimeError(f"Source {key} ({expected}) is not listed in Assets/ATTRIBUTION.md")
        self.hashes[key] = found
        return data


# ---------------------------------------------------------------- helpers
NOTES = (("B", 3), ("D", 4), ("F#", 4), ("B", 4), ("D", 5), ("F#", 5), ("B", 5), ("D", 6))
SPARKLE = ((1, 1.0), (2.76, 0.30), (5.40, 0.10))  # chime partials safe up to F#7 (no aliasing)


def lay(mix, s, key, start, end, at=0.0, gain=0.0, rate=1.0, hp_=None, lp_=None,
        fade_in=0.002, fade_out=0.02, side=None, reverse=False, orbit_turns=None):
    """Cut [start, end] s of a recording, varispeed/filter/pan it and add it to the mix at `at` s."""
    x = s.get(key)[round(start * RATE):round(end * RATE)].copy()
    if len(x) < 8:
        raise ValueError(f"Window outside {key}: {start}..{end}")
    if reverse:
        x = x[::-1].copy()
    if rate != 1.0:
        x = speed(x, rate)
    if hp_:
        x = hp(x, hp_)
    if lp_:
        x = lp(x, lp_)
    if orbit_turns is not None:
        x = orbit(x, orbit_turns)
    elif side is not None:
        x = pan(x, side)
    return place(mix, fades(x, fade_in, fade_out), at, gain)


def orbit(x, turns=1.0, phase=0.0):
    """Circle a sound around the listener: the pan follows a rotating angle."""
    m = x.mean(axis=1)
    a = phase + np.linspace(0, 2 * np.pi * turns, len(m))
    return np.column_stack((m * (0.5 + 0.5 * np.cos(a)), m * (0.5 - 0.5 * np.cos(a)))) * np.sqrt(2) * 0.7


def pl(mix, freq, dur, rng, at, gain, side=None, **kw):
    x = pluck(freq, dur, rng, **kw)
    return place(mix, pan(x, side) if side is not None else x, at, gain)


def strum(mix, freqs, dur, rng, at, gain, spread=0.008, width=0.7, **kw):
    """Chord strummed low to high, spread across the stereo field."""
    for i, f in enumerate(freqs):
        side = -width + 2 * width * i / max(1, len(freqs) - 1)
        pl(mix, f, dur, rng, at + i * spread, gain - 0.25 * i, side, **kw)
    return mix


def reverse(x, fade_in=0.01, fade_out=0.003):
    return fades(x[::-1].copy(), fade_in, fade_out)


def sweep(dur, rng, f0, f1, spread=1.3, bands=14, shape=None):
    """Band-limited air whose centre glides from f0 to f1 (rising or falling), peak-normalised."""
    n = round(dur * RATE)
    z = noise(n, rng)
    pos = np.linspace(0, bands - 1, n)
    out = np.zeros((n, 2))
    for i, c in enumerate(np.geomspace(f0, f1, bands)):
        out += bp_safe(z, c) * np.exp(-(((pos - i) / spread) ** 2))[:, None]
    out /= max(1e-9, np.abs(out).max())
    return out * shape(np.arange(n) / RATE)[:, None] if shape is not None else out


def bp_safe(x, centre, ratio=1.35):
    return base.bp(x, centre / ratio, min(centre * ratio, RATE * 0.45))


def glide(f0, f1, dur, harmonics=(1.0, 0.30, 0.10), curve=2.0, attack=0.02):
    """A taut-thread tone gliding from f0 to f1."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    f = f0 * (f1 / f0) ** ((t / dur) ** curve)
    phase = 2 * np.pi * np.cumsum(f) / RATE
    y = sum(a * np.sin((k + 1) * phase) for k, a in enumerate(harmonics))
    y *= np.clip(t / attack, 0, 1) * np.clip((dur - t) / 0.03, 0, 1)
    return mono(y / max(1e-9, np.abs(y).max()))


def ratchet(dur, rng, rate0, rate1, low=2600, high=9000):
    """Clicks at a gliding rate, like thread paying out over a spool or a reel taking it up."""
    n = round(dur * RATE)
    out = np.zeros((n, 2))
    band = base.sos("bandpass", (low, high))
    t = 0.0
    while t < dur:
        k = round(rng.uniform(0.0015, 0.003) * RATE)
        click = signal.sosfilt(band, rng.standard_normal(k)) * np.exp(-np.arange(k) / (k * 0.28))
        click *= rng.uniform(0.5, 1.0)
        place(out, pan(click, rng.uniform(-0.5, 0.5)), t, 0)
        t += (1.0 / (rate0 + (rate1 - rate0) * t / dur)) * rng.uniform(0.85, 1.15)
    return out / max(1e-9, np.abs(out).max())


def sparks(dur, rng, count, low=3500, high=9000, decay=(0.004, 0.014), fall=1.6):
    """Candle and crystal glints: tiny bright pings, denser at the start."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for _ in range(count):
        onset = dur * rng.uniform(0, 1) ** fall
        f = rng.uniform(low, high)
        dt = np.maximum(0, t - onset)
        ring = np.sin(2 * np.pi * f * dt) * (t >= onset) * np.exp(-dt / rng.uniform(*decay))
        out += pan(ring, rng.uniform(-0.8, 0.8)) * rng.uniform(0.25, 0.6)
    return out


def piano(freq, dur, rng, decay=1.5, bright=1.0):
    """Struck piano string: inharmonic partials, hammer position notch, beating unison pair."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    y = np.zeros(n)
    for detune, weight in ((1.0, 1.0), (1.0011, 0.7)):
        for k in range(1, 18):
            fk = freq * detune * k * np.sqrt(1 + 0.00035 * k * k)
            if fk > 9000:
                break
            amp = weight / k ** 1.05 * (abs(np.sin(np.pi * k / 7.5)) + 0.18) * (bright if k > 4 else 1)
            y += amp * np.sin(2 * np.pi * fk * t + rng.uniform(0, 6.28)) * np.exp(-t / (decay / (1 + 0.5 * (k - 1))))
    y *= np.clip(t / 0.0015, 0, 1) * np.clip((dur - t) / 0.05, 0, 1)
    hammer = lp(hp(noise(round(0.012 * RATE), rng), 200), 2200)[:, 0] * np.exp(-np.arange(round(0.012 * RATE)) / (0.003 * RATE))
    y[:len(hammer)] += hammer * 0.35
    return y / max(1e-9, np.abs(y).max())


def finish_window(x, seconds_, fade_out):
    return taper(x[:round(seconds_ * RATE)], fade_out)


def taper(x, fade_out, fade_in=0.0005):
    """Half-cosine fades (the cue ends where its designed length says, not where the reverb tail dies)."""
    x = x.copy()
    a, b = max(1, round(fade_in * RATE)), min(len(x), max(1, round(fade_out * RATE)))
    x[:a] *= (0.5 - 0.5 * np.cos(np.linspace(0, np.pi, a)))[:, None]
    x[-b:] *= (0.5 + 0.5 * np.cos(np.linspace(0, np.pi, b)))[:, None]
    return x


def room(mix, wet, length, fade):
    """Hall-coloured mix cut to its designed length."""
    return taper(hall(mix, wet)[:round(length * RATE)], fade)


def loudness(x):
    """generate_ebon_sfx.loudness (BS.1770 K-weighted maximum 400 ms short-term LUFS), with a cumulative sum
    instead of a 17640-tap convolution so long cues render in milliseconds."""
    power = (base.k_weight(x) ** 2).sum(axis=1)
    block = round(0.4 * RATE)
    if len(power) < block:
        power = np.concatenate((power, np.zeros(block - len(power))))
    c = np.concatenate(([0.0], np.cumsum(power)))
    return -0.691 + 10 * np.log10(max(((c[block:] - c[:-block]) / block).max(), 1e-12))


def soft(x, k):
    """Peak-preserving tanh bus: shaves the transient so the cue can sit at its LUFS target under the peak ceiling."""
    peak = np.abs(x).max()
    return np.tanh(x / peak * k) / np.tanh(k) * peak if k > 0 else x


def master(x, target_lufs, glue=0.0, ceiling_db=-1.3):
    x = soft(x, glue)
    x = x * 10 ** ((target_lufs - loudness(x)) / 20)
    peak = np.abs(signal.resample_poly(x, 4, 1, axis=0)).max()
    ceiling = 10 ** (ceiling_db / 20)
    return x * ceiling / peak if peak > ceiling else x


# ---------------------------------------------------------------- cues
def note_builder(i):
    def build(s, rng):
        f = hz(*NOTES[i])
        body = pluck(f, 0.5, rng, bright=0.9, sustain=float(np.exp(-1.6 / f)), pick=0.14, width=0.3)  # equal decay time at every pitch
        t = np.arange(len(body)) / RATE
        body *= (np.exp(-t / 0.14) * np.clip((0.40 - t) / 0.12, 0, 1))[:, None]
        mix = seconds(0.5)
        place(mix, body, 0.0, 0)
        k = round(0.0035 * RATE)  # bright pick attack
        place(mix, pan(hp(noise(k, rng), 2800) * np.linspace(1, 0, k)[:, None], 0.25 * (1 if i % 2 else -1)), 0.0, -15)
        return finish_window(hall(mix, 0.12), 0.36, 0.09)
    return build


def cue_hatbox_open(s, rng):
    mix = seconds(2.8)
    # The ribbon bow unties and its two streamers fly off to either side.
    lay(mix, s, "cloth1", 0.08, 0.50, 0.00, -6, hp_=400, lp_=6000, fade_out=0.08)
    lay(mix, s, "cloth4", 0.02, 0.30, 0.05, -10, hp_=1500, side=-0.5)
    lay(mix, s, "swoosh", 0.03, 0.24, 0.18, -8, rate=1.15, hp_=600, side=-0.8, fade_out=0.08)
    lay(mix, s, "swoosh", 0.50, 0.72, 0.24, -9, rate=1.05, hp_=600, side=0.8, fade_out=0.08)
    # The lid unlatches, pops and spins away.
    lay(mix, s, "metal_latch", 0.03, 0.20, 0.30, -4, hp_=900)
    place(mix, thump(150, 62, 0.28, rng), 0.31, -13)
    lay(mix, s, "air_cut", 0.05, 0.40, 0.32, -9, rate=1.3, hp_=500, orbit_turns=1.5, fade_out=0.1)
    # A rising run of silk plucks (B3..D6) lands on a B minor chord.
    for i, (name, octave) in enumerate(NOTES):
        pl(mix, hz(name, octave), 0.8, rng, 0.40 + i * 0.062, -15 + 0.9 * i, -0.55 + 1.1 * i / 7,
           bright=0.88, sustain=0.995, pick=0.12)
    strum(mix, [hz(n, o) for n, o in (("B", 3), ("F#", 4), ("B", 4), ("D", 5), ("F#", 5), ("B", 5))], 2.0, rng,
          0.90, -6.5, spread=0.009, bright=0.9, sustain=0.9984, pick=0.1)
    place(mix, chime(hz("B", 6), 1.4, rng, decay=0.7, partials=SPARKLE), 0.92, -14)
    # Lace and moonlight motes.
    for _ in range(12):
        f = hz(*(("F#", 6), ("B", 6), ("D", 7), ("F#", 7))[int(rng.integers(0, 4))])
        place(mix, chime(f, 0.9, rng, decay=0.35, partials=SPARKLE), 0.55 + rng.uniform(0, 1.6), -25 + rng.uniform(-3, 3))
    return room(mix, 0.3, 2.7, 0.7)


def cue_shear_swing(s, rng):
    mix = seconds(0.40)
    lay(mix, s, "stick_woosh", 0.06, 0.40, 0.0, -3, hp_=250, lp_=7500, fade_in=0.006, fade_out=0.11)
    lay(mix, s, "swoosh", 0.04, 0.21, 0.005, -3, rate=1.15, hp_=500, fade_out=0.07)
    lay(mix, s, "samurai_slash", 0.005, 0.20, 0.0, -9, hp_=900, lp_=9000, fade_out=0.08)
    lay(mix, s, "cloth4", 0.04, 0.22, 0.0, -9, hp_=1500, fade_out=0.08)
    lay(mix, s, "anime_shing", 0.05, 0.26, 0.012, -11, hp_=3500, fade_in=0.002, fade_out=0.12)
    return room(mix, 0.08, 0.4, 0.12)


def cue_shear_swing_rise(s, rng):
    # Blades open behind and drive forward: a band-swept rise, a reversed ring and a thread tightening up
    # to B6, peaking about 0.25 s in (the closing ShearSnip follows it).
    mix = seconds(0.50)
    rise = sweep(0.30, rng, 500, 6500, shape=lambda t: np.clip(t / 0.22, 0, 1) ** 1.6 * np.clip((0.30 - t) / 0.05, 0, 1))
    place(mix, rise, 0.0, -2)
    lay(mix, s, "anime_shing", 0.0, 0.36, 0.0, -11, hp_=2500, reverse=True, fade_in=0.01, fade_out=0.004)
    lay(mix, s, "energy_wave", 0.12, 0.44, 0.15, -4, rate=1.3, hp_=250, fade_in=0.01, fade_out=0.09)
    lay(mix, s, "swoosh", 0.04, 0.22, 0.17, -8, rate=1.2, hp_=600, fade_out=0.08)
    place(mix, glide(hz("B", 5), hz("B", 6), 0.26), 0.0, -15)
    return room(mix, 0.1, 0.5, 0.1)


def cue_shear_snip(s, rng):
    mix = seconds(0.55)
    lay(mix, s, "knife_slice", 0.0, 0.20, 0.0, -5, hp_=700, fade_out=0.06)
    lay(mix, s, "metal_latch", 0.03, 0.20, 0.0, -4, hp_=700)
    lay(mix, s, "metal_latch", 0.03, 0.20, 0.028, -7, rate=1.25, hp_=1100)
    lay(mix, s, "armor_strike", 0.065, 0.30, 0.0, -1, hp_=300, lp_=6000, fade_out=0.1)
    lay(mix, s, "metal_pot", 0.08, 0.28, 0.0, -13, hp_=700, fade_out=0.1)
    lay(mix, s, "anime_shing", 0.05, 0.40, 0.006, -14, hp_=3500, fade_out=0.15)
    pl(mix, hz("F#", 6), 0.35, rng, 0.0, -14, bright=0.98, sustain=0.985, pick=0.06)
    place(mix, thump(170, 70, 0.18, rng), 0.0, -17)
    return room(mix, 0.14, 0.5, 0.18)


def cue_shear_cut(s, rng):
    mix = seconds(1.5)
    place(mix, rip(0.6, rng, density=(1100, 200), low=900, high=9000), 0.0, -4)
    lay(mix, s, "draw_knife", 0.06, 0.48, 0.0, -4, hp_=400, fade_out=0.1)
    lay(mix, s, "knife_slice", 0.18, 0.50, 0.0, -3, hp_=1000, fade_out=0.1)
    lay(mix, s, "samurai_slash", 0.005, 0.40, 0.0, -7, hp_=300, lp_=9000, fade_out=0.12)
    lay(mix, s, "anime_ring", 0.08, 0.90, 0.03, -16, hp_=1800, fade_in=0.01, fade_out=0.3)
    strum(mix, [hz(n, o) for n, o in (("B", 4), ("F#", 5), ("B", 5), ("D", 6))], 1.0, rng, 0.0, -12, spread=0.006,
          bright=0.97, sustain=0.99, pick=0.06)
    place(mix, thump(150, 55, 0.35, rng), 0.0, -16)
    return room(mix, 0.22, 1.1, 0.4)


def cue_harp_loose(s, rng):
    mix = seconds(0.60)
    pl(mix, hz("F#", 3), 0.5, rng, 0.0, -3, -0.2, bright=0.7, sustain=0.997, pick=0.1)
    pl(mix, hz("B", 3), 0.45, rng, 0.0, -9, 0.2, bright=0.8, sustain=0.995, pick=0.1)
    lay(mix, s, "swish", 0.0, 0.15, 0.004, -9, hp_=1500)
    lay(mix, s, "chop", 0.042, 0.12, 0.0, -12, lp_=2500, fade_out=0.04)
    lay(mix, s, "air_cut", 0.10, 0.30, 0.02, -14, hp_=500, fade_out=0.08)
    return room(mix, 0.1, 0.55, 0.2)


def cue_harp_chord(s, rng):
    mix = seconds(2.0)
    pl(mix, hz("B", 2), 1.8, rng, 0.0, -8, 0.0, bright=0.85, sustain=0.9988, pick=0.12)
    strum(mix, [hz(n, o) for n, o in (("B", 3), ("F#", 4), ("B", 4), ("D", 5), ("F#", 5), ("B", 5))], 1.9, rng, 0.0, -6,
          spread=0.007, bright=0.92, sustain=0.9985, pick=0.1)
    place(mix, chime(hz("B", 6), 1.5, rng, decay=0.8, partials=SPARKLE), 0.02, -14)
    lay(mix, s, "metal_latch", 0.03, 0.20, 0.0, -14, hp_=2500)
    place(mix, thump(90, 55, 0.35, rng), 0.0, -18)
    return room(mix, 0.32, 1.6, 0.6)


def cue_thimble_lift(s, rng):
    mix = seconds(0.75)
    lay(mix, s, "cloth1", 0.10, 0.55, 0.0, -8, rate=1.3, hp_=500, lp_=5000, fade_in=0.05, fade_out=0.15)
    lay(mix, s, "creak1", 0.42, 0.66, 0.06, -16, rate=1.25, hp_=800, fade_in=0.03, fade_out=0.1)
    place(mix, glide(hz("F#", 5), hz("B", 5), 0.36, curve=1.5), 0.0, -17)
    pl(mix, hz("B", 5), 0.45, rng, 0.03, -12, bright=0.8, sustain=0.992, pick=0.12)
    place(mix, chime(hz("F#", 6), 0.7, rng, decay=0.3, partials=SPARKLE), 0.25, -15)
    lay(mix, s, "cloth4", 0.04, 0.30, 0.02, -13, hp_=2500, fade_in=0.04, fade_out=0.1)
    return room(mix, 0.2, 0.62, 0.2)


def cue_furniture_yank(s, rng):
    mix = seconds(0.70)
    lay(mix, s, "swoosh", 0.04, 0.25, 0.0, -3, rate=1.3, hp_=350, fade_out=0.09)
    lay(mix, s, "stick_woosh", 0.05, 0.38, 0.01, -4, rate=1.2, hp_=200, lp_=6000, fade_out=0.12)
    place(mix, sweep(0.30, rng, 300, 4500, shape=lambda t: np.clip(t / 0.25, 0, 1) * np.clip((0.30 - t) / 0.04, 0, 1)), 0.0, -12)
    lay(mix, s, "cloth4", 0.03, 0.28, 0.0, -14, hp_=2000, fade_out=0.1)
    pl(mix, hz("B", 3), 0.5, rng, 0.0, -6, bright=0.9, sustain=0.996, pick=0.08)
    pl(mix, hz("B", 4), 0.4, rng, 0.0, -11, bright=0.9, sustain=0.994)
    return room(mix, 0.12, 0.5, 0.15)


def cue_furniture_crash(s, rng):
    mix = seconds(1.0)
    lay(mix, s, "door_close", 0.09, 0.50, 0.0, -5, fade_out=0.15)
    lay(mix, s, "chop", 0.04, 0.24, 0.004, -3, rate=0.78, lp_=3500, fade_out=0.06)
    lay(mix, s, "rock_tumble", 0.06, 0.80, 0.03, -9, lp_=3500, fade_out=0.2)
    place(mix, glass(0.5, rng, count=14, spread=0.2, low=2200, high=7000, decay=(0.02, 0.09)), 0.02, -9)
    place(mix, thump(130, 50, 0.45, rng), 0.0, -11)
    pl(mix, hz("B", 2), 0.4, rng, 0.0, -17, bright=0.6, sustain=0.995)  # a cello string bumped
    return room(mix, 0.18, 0.75, 0.25)


def cue_piano_crash(s, rng):
    mix = seconds(3.2)
    lay(mix, s, "low_impact", 0.0, 1.9, 0.0, -6, rate=0.9, fade_out=0.4)
    cluster = (("B", 1), ("F#", 2), ("B", 2), ("C#", 3), ("D", 3), ("F#", 3), ("B", 3), ("C#", 4))
    for i, (name, octave) in enumerate(cluster):
        x = pan(piano(hz(name, octave), 2.2, rng, decay=1.5), -0.5 + 1.0 * i / 7)
        place(mix, x, i * 0.004, -7 - 0.5 * i)
    lay(mix, s, "door_close", 0.09, 0.60, 0.0, -2, rate=0.72, fade_out=0.2)
    lay(mix, s, "chop", 0.04, 0.24, 0.0, -3, rate=0.6, lp_=3000, fade_out=0.08)
    lay(mix, s, "creak2", 0.0, 0.83, 0.22, -12, rate=0.7, fade_out=0.2)
    lay(mix, s, "rock_tumble", 0.06, 1.10, 0.08, -8, lp_=3200, fade_out=0.3)
    lay(mix, s, "metal_pot", 0.08, 1.40, 0.01, -11, hp_=300, fade_out=0.4)
    place(mix, glass(1.5, rng, count=55, spread=0.55, low=1500, high=7000), 0.03, -6)
    place(mix, chain(1.0, rng, count=16, spread=0.6), 0.05, -13)
    place(mix, thump(95, 36, 0.9, rng), 0.0, -7)
    return room(mix, 0.3, 2.2, 0.8)


def cue_chandelier_snip(s, rng):
    mix = seconds(0.55)
    lay(mix, s, "knife_slice", 0.0, 0.14, 0.0, -4, hp_=2000, fade_out=0.05)
    lay(mix, s, "metal_latch", 0.03, 0.15, 0.0, -5, rate=1.1, hp_=1500)
    lay(mix, s, "metal_click", 0.07, 0.20, 0.004, -7, hp_=1500, fade_out=0.06)
    pl(mix, hz("B", 5), 0.35, rng, 0.0, -5, 0.3, bright=0.98, sustain=0.988, pick=0.05)
    pl(mix, hz("F#", 4), 0.3, rng, 0.004, -12, -0.3, bright=0.9, sustain=0.99, pick=0.05)
    lay(mix, s, "creak1", 0.40, 0.62, 0.06, -16, rate=0.8, lp_=3000, fade_in=0.02, fade_out=0.1)
    place(mix, chain(0.3, rng, count=6, spread=0.15), 0.02, -14)
    return room(mix, 0.16, 0.45, 0.15)


def cue_chandelier_shatter(s, rng):
    mix = seconds(2.4)
    lay(mix, s, "low_impact", 0.0, 1.2, 0.0, -8, lp_=2500, fade_out=0.35)
    place(mix, glass(1.9, rng, count=80, spread=0.45, low=1500, high=7000, decay=(0.04, 0.2)), 0.0, 0)
    for i in range(8):  # crystal drops ring in B minor
        f = hz(*(("B", 6), ("D", 7), ("F#", 6), ("F#", 7), ("D", 6))[i % 5])
        place(mix, chime(f, 0.8, rng, decay=0.45, partials=SPARKLE), 0.01 + rng.uniform(0, 0.5) * (i / 7), -13 - i * 0.6)
    lay(mix, s, "metal_pot", 0.08, 1.10, 0.01, -9, hp_=500, fade_out=0.4)
    place(mix, chain(1.2, rng, count=22, spread=0.7), 0.05, -9)
    place(mix, sparks(0.9, rng, 34), 0.02, -13)
    place(mix, thump(110, 38, 0.8, rng), 0.0, -7)
    return room(mix, 0.3, 1.6, 0.6)


def cue_chandelier_reel(s, rng):
    # Shards fly back together (a reversed glass burst ends on a catch), the chain is reeled up and the
    # candles relight one by one.
    mix = seconds(1.6)
    shards = glass(0.42, rng, count=40, spread=0.3, low=2000, high=6500, decay=(0.03, 0.1))
    place(mix, reverse(shards, 0.02, 0.004), 0.0, -8)
    lay(mix, s, "metal_click", 0.07, 0.20, 0.405, -7, hp_=2000, fade_out=0.06)
    place(mix, chime(hz("B", 6), 0.6, rng, decay=0.3, partials=SPARKLE), 0.405, -11)
    place(mix, ratchet(0.7, rng, 18, 32), 0.0, -6)
    lay(mix, s, "creak1", 0.38, 0.66, 0.03, -14, rate=1.3, hp_=700, fade_in=0.04, fade_out=0.1)
    for i, (name, octave) in enumerate((("B", 5), ("D", 6), ("F#", 6), ("B", 6))):
        at = 0.50 + i * 0.12
        place(mix, chime(hz(name, octave), 0.9, rng, decay=0.5, partials=SPARKLE), at, -9 - i)
        lay(mix, s, "air_cut", 0.10, 0.19, at, -24, hp_=1500, fade_out=0.04)
    return room(mix, 0.3, 1.3, 0.4)


def cue_spool_throw(s, rng):
    mix = seconds(0.60)
    lay(mix, s, "swoosh", 0.04, 0.22, 0.0, -4, hp_=300, fade_out=0.08)
    lay(mix, s, "air_cut", 0.08, 0.34, 0.0, -9, hp_=500, fade_out=0.1)
    place(mix, ratchet(0.40, rng, 58, 18), 0.0, -4)
    place(mix, hp(noise(round(0.4 * RATE), rng), 4500) * env(round(0.4 * RATE), 0.01, 0.1)[:, None] * 0.07, 0.0, -2)
    pl(mix, hz("B", 4), 0.4, rng, 0.0, -17, bright=0.8, sustain=0.99)
    return room(mix, 0.1, 0.45, 0.15)


def cue_silk_pin(s, rng):
    mix = seconds(0.80)
    lay(mix, s, "chop", 0.045, 0.12, 0.0, -6, lp_=2500, fade_out=0.04)
    lay(mix, s, "metal_click", 0.07, 0.20, 0.0, -8, hp_=2500, fade_out=0.06)
    place(mix, thump(120, 70, 0.1, rng), 0.0, -24)
    pl(mix, hz("F#", 5), 0.6, rng, 0.008, -4, 0.15, bright=0.95, sustain=0.992, pick=0.06)
    pl(mix, hz("B", 5), 0.45, rng, 0.02, -11, -0.15, bright=0.95, sustain=0.99, pick=0.06)
    place(mix, chime(hz("F#", 6), 0.6, rng, decay=0.25, partials=SPARKLE), 0.03, -22)
    return room(mix, 0.18, 0.55, 0.2)


def cue_scissors_snip(s, rng):
    mix = seconds(0.80)
    lay(mix, s, "knife_slice", 0.0, 0.18, 0.0, -3, hp_=2500, fade_out=0.05)
    lay(mix, s, "metal_latch", 0.03, 0.22, 0.0, -3, hp_=1800)
    lay(mix, s, "metal_latch", 0.03, 0.22, 0.045, -5, rate=1.35, hp_=1800)
    place(mix, chime(hz("B", 6), 0.6, rng, decay=0.35, partials=((1, 1), (2.76, 0.4), (4.1, 0.2))), 0.0, -9)
    place(mix, chime(hz("F#", 7), 0.4, rng, decay=0.2, partials=SPARKLE), 0.04, -13)
    for k, f in enumerate((3000, 3300)):  # the bird's two chirps
        place(mix, glide(f, f * 1.8, 0.07, harmonics=(1.0, 0.2), curve=1.0, attack=0.008), 0.09 + k * 0.1, -14)
    return room(mix, 0.2, 0.5, 0.2)


def cue_sever_all(s, rng):
    mix = seconds(2.8)
    strum(mix, [hz(n, o) for n, o in (("B", 3), ("F#", 4), ("B", 4), ("D", 5), ("F#", 5), ("B", 5), ("D", 6), ("F#", 6))],
          1.7, rng, 0.0, -6.5, spread=0.005, width=0.85, bright=0.98, sustain=0.9982, pick=0.06)
    scale = [hz(n, o) for n, o in (("B", 5), ("D", 6), ("F#", 6), ("C#", 6), ("E", 6), ("A", 5), ("B", 6))]
    for i in range(16):  # strands part one after another, faster and faster (the weapon adds its own per-strand notes)
        at = 0.02 + 0.4 * (i / 16) ** 1.5
        f = scale[int(rng.integers(0, len(scale)))]
        pl(mix, f, 0.22, rng, at, -13 - 0.12 * i + rng.uniform(-2, 1), (-1) ** i * rng.uniform(0.3, 0.9),
           bright=0.98, sustain=0.972, pick=0.05)
    place(mix, rip(0.5, rng, density=(1500, 150), low=1500, high=9000), 0.0, -6)
    lay(mix, s, "knife_slice", 0.01, 0.42, 0.0, -4, hp_=1000, fade_out=0.12)
    lay(mix, s, "sword_hit", 0.0, 0.30, 0.0, -9, hp_=2500, lp_=10000, fade_in=0.001, fade_out=0.1)
    lay(mix, s, "anime_ring", 0.08, 1.30, 0.02, -15, hp_=1600, fade_in=0.01, fade_out=0.4)
    lay(mix, s, "low_impact", 0.0, 0.90, 0.0, -14, lp_=800, fade_out=0.3)
    place(mix, thump(140, 45, 0.6, rng), 0.0, -12)
    return room(mix, 0.3, 1.6, 0.6)


def cue_waltz_open(s, rng):
    mix = seconds(2.0)
    # The parasol snaps open: canopy, ribs and the catch.
    lay(mix, s, "cloth4", 0.05, 0.30, 0.0, 1, hp_=700, fade_out=0.08)
    lay(mix, s, "book_flip", 0.02, 0.20, 0.0, 0, rate=0.8, lp_=6000)
    lay(mix, s, "metal_click", 0.07, 0.20, 0.1, -3, hp_=1200, fade_out=0.06)
    place(mix, thump(110, 60, 0.15, rng), 0.12, -17)
    lay(mix, s, "woosh", 1.30, 1.72, 0.12, -18, hp_=300, orbit_turns=1.0, fade_in=0.12, fade_out=0.2)
    # A lilting oom-pah-pah in B minor, then a lace of chimes.
    pl(mix, hz("B", 3), 0.6, rng, 0.14, -12, -0.4, bright=0.8, sustain=0.9965, pick=0.1)
    strum(mix, (hz("F#", 4), hz("B", 4)), 0.5, rng, 0.31, -15, spread=0.006, width=0.3, bright=0.85, sustain=0.994)
    strum(mix, (hz("D", 5), hz("F#", 5)), 0.6, rng, 0.47, -14, spread=0.006, width=0.5, bright=0.85, sustain=0.995)
    for i, (name, octave) in enumerate((("B", 5), ("D", 6), ("F#", 6), ("B", 6))):
        place(mix, chime(hz(name, octave), 1.0, rng, decay=0.55, partials=SPARKLE), 0.66 + i * 0.07, -20 - i)
    return room(mix, 0.3, 1.6, 0.5)


# name: (builder, short-term loudness target LUFS (generate_ebon_sfx scale), weapon group, audition description)
CUES = {"HatboxOpen": (cue_hatbox_open, -17, "Hatbox", "リボンがほどけて流れ、蓋が跳ねて回り、B3からD6への撥弦ランがBマイナーの和音に着地する。")}
CUES.update({f"Note{i}": (note_builder(i), -20, "Notes", f"絹の撥弦 {NOTES[i][0]}{NOTES[i][1]}。ヒットを重ねるたびに1段ずつ上る。") for i in range(8)})
CUES.update({
    "ShearSwing": (cue_shear_swing, -17, "Moonshear", "A/B/C の振り。短く、早めにピークが来る。糸の擦れとブレードの輝き。"),
    "ShearSwingRise": (cue_shear_swing_rise, -16, "Moonshear", "D の開いて踏み込む動き、および切り線を走る鋏。上昇する風と逆再生のきらめき。ピーク約0.25 s。"),
    "ShearSnip": (cue_shear_snip, -14, "Moonshear", "D の閉じる瞬間。金属の二段クリックと短い輝き、F#6 の糸の張り。"),
    "ShearCut": (cue_shear_cut, -13, "Moonshear", "切り線が裂ける帯。絹の裂け目、斬撃、残響する金属の輪、B のきらめき和音。"),
    "HarpLoose": (cue_harp_loose, -18, "Moonloom Harp", "矢を放つ弦のはじき。F#3 と B3 の低い糸鳴りと風切り。"),
    "HarpChord": (cue_harp_chord, -14, "Moonloom Harp", "グリッサンドの着地。Bマイナーの6弦ストラム、B6 の輝き、低い胴鳴り。"),
    "ThimbleLift": (cue_thimble_lift, -19, "Ebon Thimble", "指ぬきの糸で家具が浮く。布の滑り、軋み、上昇する細い音。拍ごとに鳴る小さな音。"),
    "FurnitureYank": (cue_furniture_yank, -16, "Ebon Thimble", "家具を引き寄せ投げる。加速する風と B3/B4 の糸のはじき。"),
    "FurnitureCrash": (cue_furniture_crash, -14, "Ebon Thimble", "家具の衝突。木の割れ、ガラス片、破片が転がる。0.12 s 間隔の連打でも濁りにくい。"),
    "PianoCrash": (cue_piano_crash, -12, "Ebon Thimble", "グランドピアノの落下。低いクラスター和音、木の砕け、ガラスの雨、弦枠の響き。"),
    "ChandelierSnip": (cue_chandelier_snip, -16, "Ballroom Chandelier", "吊り糸を鋏で断つ。金属の切断音と B5 の弾け、鎖のたるみ。"),
    "ChandelierShatter": (cue_chandelier_shatter, -13, "Ballroom Chandelier", "クリスタルの砕け。重い衝撃、ガラスの雨、Bマイナーに鳴る水晶、蝋燭の火花。"),
    "ChandelierReel": (cue_chandelier_reel, -19, "Ballroom Chandelier", "破片が集まり巻き上げられ、蝋燭が一つずつ灯る。逆再生ガラスとラチェット。"),
    "SpoolThrow": (cue_spool_throw, -17, "Severing Silk", "糸巻きを投げる。風切りと糸が繰り出されるラチェット。"),
    "SilkPin": (cue_silk_pin, -18, "Severing Silk", "糸が壁や敵に留まる。針の刺さる音と F#5/B5 の張った弦。"),
    "ScissorsSnip": (cue_scissors_snip, -14, "Severing Silk", "鳥の刺繍鋏の一裁ち。二度の金属クリックと小さな鳥のさえずり。"),
    "SeverAll": (cue_sever_all, -12, "Severing Silk", "全ての糸が一斉に断たれる。和音、加速する弾けの連なり、裂け目、重い衝撃。"),
    "WaltzOpen": (cue_waltz_open, -17, "The Last Waltz", "日傘が開く。傘布と骨の音、円を描く風、ブンチャッチャのBマイナーと鈴の連なり。"),
})


# ---------------------------------------------------------------- render
GLUE_DEFAULT = 1.6  # tanh bus drive (see soft); notes are sharper plucks and get more
GLUE = {f"Note{i}": 2.2 for i in range(8)} | {"ShearSnip": 2.4}


def render(name, store):
    builder, target = CUES[name][:2]
    rng = np.random.default_rng(int.from_bytes(hashlib.sha256(("EbonReward:" + name).encode()).digest()[:8], "little"))
    return trim(hp(builder(store, rng), 28)), target


def true_peak_db(x):
    return 20 * np.log10(np.abs(signal.resample_poly(x, 4, 1, axis=0)).max())


def render_cue(name, store, output):
    """Master to the cue's target, write the Ogg and, if the Vorbis round trip overshoots, back the gain off."""
    store.used = set()
    x, target = render(name, store)
    path = output / f"{name}.ogg"
    mastered = master(x, target, GLUE.get(name, GLUE_DEFAULT))
    for _ in range(8):
        decoded = base.write_ogg(path, mastered)
        peak = true_peak_db(decoded)
        if peak <= -1.0:
            return decoded, sorted(store.used)
        mastered = mastered * 10 ** (-(peak + 1.1) / 20)
    raise RuntimeError(f"{name}: true peak {peak:.2f} dBTP after retries")


def analyse(name, decoded, path, sources_used):
    mono_ = decoded.mean(axis=1)
    frames = np.array([np.sqrt(np.mean(mono_[i:i + 441] ** 2) + 1e-18) for i in range(0, len(mono_) - 441, 441)])
    f, p = signal.welch(mono_, RATE, nperseg=min(4096, len(mono_)))
    info = {
        "seconds": round(len(decoded) / RATE, 3),
        "short_term_lufs": round(loudness(decoded), 2),
        "target_lufs": CUES[name][1],
        "true_peak_dbfs": round(true_peak_db(decoded), 2),
        "peak_ms": int(frames.argmax()) * 10,
        "centroid_hz": round(float((f * p).sum() / (p.sum() + 1e-18))),
        "sources": sources_used,
        "bytes": path.stat().st_size,
        "ogg_sha256": sha256(path.read_bytes()),
    }
    if name.startswith("Note"):
        info["pitch_cents_error"] = round(pitch_error_cents(decoded, hz(*NOTES[int(name[4:])])), 2)
    return info


def pitch_error_cents(decoded, target):
    seg = decoded.mean(axis=1)[round(0.02 * RATE):round(0.22 * RATE)]
    n = 1 << 18
    spectrum = np.abs(np.fft.rfft(seg * np.hanning(len(seg)), n))
    freqs = np.fft.rfftfreq(n, 1 / RATE)
    band = (freqs > target * 0.9) & (freqs < target * 1.1)
    k = np.argmax(np.where(band, spectrum, 0))
    return 1200 * np.log2(freqs[k] / target)


# ---------------------------------------------------------------- audition combos
def build_mix(events):
    """events: (sample, time_seconds, gain_db, rate). Overlay into one stereo mix."""
    end = max(t + len(x) / RATE / r for x, t, _, r in events)
    mix = seconds(end + 0.2)
    for x, t, gain, rate in events:
        place(mix, speed(x, rate) if rate != 1.0 else x, t, gain)
    return mix * min(1.0, 0.95 / max(1e-9, np.abs(mix).max()))


def combos(samples):
    S = samples
    out = {}
    # Moonshear: four-stroke kata on the 60-tick clock, then Cut Line and its mark pops.
    ev = []
    for k, (start, live) in enumerate(((0, 7), (18, 6), (34, 7))):  # A, B, C strokes
        ev.append((S["ShearSwing"], (start + live) * TICK - 0.07, -4, 1.0 + 0.04 * k))
        ev.append((S[f"Note{k}"], (start + live + 3) * TICK, -6, 1.0))
    ev.append((S["ShearSwingRise"], 52 * TICK, -4, 1.0))
    ev.append((S["ShearSnip"], (52 + 20) * TICK, -3, 1.0))
    cut_at = 110 * TICK
    ev.append((S["ShearSwingRise"], cut_at, -6, 1.32))
    ev.append((S["ShearCut"], cut_at + 12 * TICK, -3, 1.0))
    for j in range(5):  # every mark pops, one per 4 ticks, climbing; the last rings the chord
        ev.append((S[f"Note{j}"], cut_at + (12 + 10 + 4 * j) * TICK, -5, 1.0))
    ev.append((S["HarpChord"], cut_at + (12 + 10 + 16) * TICK, -3, 1.0))
    out["Moonshear-combo"] = build_mix(ev)
    # Moonloom Harp: an arrow volley (16-tick use time), then the glissando at one pluck per sixteenth.
    ev = []
    for k in range(6):
        t = k * 16 * TICK
        ev.append((S["HarpLoose"], t, -5, 1.0 + 0.02 * (k % 3)))
        ev.append((S[f"Note{k}"], t + 0.22, -9, 1.0))
    g0 = 6 * 16 * TICK + 0.45
    for k in range(8):
        ev.append((S[f"Note{k}"], g0 + k * SIXTEENTH, -5, 1.0))
    ev.append((S["HarpChord"], g0 + 7 * SIXTEENTH, -3, 1.0))
    out["Moonloom-combo"] = build_mix(ev)
    ev = [(S[f"Note{k}"], k * SIXTEENTH, -4, 1.0) for k in range(8)] + [(S["HarpChord"], 7 * SIXTEENTH, -3, 1.0)]
    out["Harp-glissando"] = build_mix(ev)
    # Ebon Thimble: eight lifts on the beat clock, the yank volley and the grand piano.
    ev = []
    for k in range(8):
        t = (6 + k * 28.8) * TICK
        ev.append((S["ThimbleLift"], t, -3, 1.0))
        ev.append((S[f"Note{k}"], t, -9, 1.0))
    r0 = (6 + 7 * 28.8) * TICK + 0.5
    for k in range(8):
        ev.append((S["FurnitureYank"], r0 + k * SIXTEENTH, -5, 1.0 + 0.03 * (k % 3)))
        ev.append((S["FurnitureCrash"], r0 + k * SIXTEENTH + 0.30, -4, 1.0 + 0.02 * (k % 4)))
    drop = r0 + (7 + 2) * SIXTEENTH
    ev.append((S["PianoCrash"], drop + 29 * TICK, -2, 1.0))
    out["Thimble-combo"] = build_mix(ev)
    # Ballroom Chandelier: four chandeliers, one drop per beat; the thread snips, 20 ticks later it lands.
    ev = []
    for k in range(8):
        t = 0.1 + k * BEAT
        pitch = (1.0, 1.04, 0.97, 1.03)[k % 4]
        ev.append((S["ChandelierSnip"], t, -5, pitch))
        ev.append((S["ChandelierShatter"], t + 20 * TICK, -4, pitch))
        ev.append((S["ChandelierReel"], t + 20 * TICK + 0.12, -6, 1.0))
    out["Chandelier-cascade"] = build_mix(ev)
    # Severing Silk: six throws and pins, then the scissors and the sever.
    ev = []
    for k in range(6):
        t = k * 18 * TICK
        ev.append((S["SpoolThrow"], t, -4, 1.0 + 0.03 * (k % 3)))
        ev.append((S["SilkPin"], t + 0.22, -5, 1.0 + 0.04 * (k % 4)))
    s0 = 6 * 18 * TICK + 0.4
    ev.append((S["SpoolThrow"], s0 - 0.35, -4, 1.1))
    ev.append((S["ScissorsSnip"], s0, -3, 1.0))
    ev.append((S["SeverAll"], s0 + 6 * TICK, -2, 1.0))
    out["Severing-combo"] = build_mix(ev)
    # The Last Waltz: six flung pieces on beats 1-6, then the parasol opens on beat 7.
    ev = []
    for k in range(6):
        t = 0.1 + k * BEAT
        ev.append((S["FurnitureYank"], t, -8, 1.0 + 0.03 * (k % 3)))
        ev.append((S["FurnitureCrash"], t + 0.28, -10, 1.0))
        ev.append((S[f"Note{k}"], t + 0.28, -8, 1.0))
    ev.append((S["WaltzOpen"], 0.1 + 6 * BEAT, -3, 1.0))
    out["LastWaltz-combo"] = build_mix(ev)
    return out


COMBO_NOTES = {
    "Moonshear-combo": "月裁ちの大鋏：A/B/C の振りと音階、D の踏み込みと閉じ、切り線の走り、裂け目、5つの印が4 tick ごとに弾けて最後に和音。",
    "Moonloom-combo": "月光の糸竪琴：矢の連射（16 tick 間隔）と命中の撥弦、続けて7.2 tick 間隔のグリッサンドが和音に着地。",
    "Harp-glissando": "グリッサンドだけ：Note0..7 を 7.2 tick（0.12 s）間隔で、最後に HarpChord。",
    "Thimble-combo": "黒絹の指ぬき：拍ごとに8つ持ち上げ、離すと十六分で引き寄せられ衝突、最後にグランドピアノが落ちる。",
    "Chandelier-cascade": "舞踏会のシャンデリア：4台が1拍ずつ落ち（吊り糸を断ち、砕け、巻き上げ）、2サイクル。",
    "Severing-combo": "断ち糸：6投と糸の留まり、続けて鳥の鋏と全断ち。",
    "LastWaltz-combo": "最後の円舞曲：拍1〜6で家具を投げ、拍7で日傘が開く。",
}


# ---------------------------------------------------------------- audition page and attribution
def page(directory, report, combo_names):
    def row(name):
        r = report[name]
        desc = html.escape(CUES[name][3])
        return (f"<tr><td><b>{html.escape(name)}</b><br><small>{desc}</small></td>"
                f"<td><audio controls preload='none' src='{html.escape(name)}.wav'></audio></td>"
                f"<td>{r['seconds']:.2f}s<br><small>peak {r['peak_ms']} ms</small></td>"
                f"<td>{r['short_term_lufs']:.1f}<br><small>target {r['target_lufs']}</small></td>"
                f"<td>{r['true_peak_dbfs']:.1f}</td>"
                f"<td><small>{html.escape(', '.join(r['sources']) or 'synthesis only')}</small></td></tr>")

    groups = {}
    for name, (_, _, group, _) in CUES.items():
        groups.setdefault(group, []).append(name)
    sections = "\n".join(
        f"<h2>{html.escape(g)}</h2>\n<table><tr><th>Cue</th><th></th><th>Length</th><th>LUFS (short)</th><th>True peak dBTP</th><th>Sources</th></tr>\n"
        + "\n".join(row(n) for n in names) + "\n</table>" for g, names in groups.items())
    combos_html = "\n".join(
        f"<tr><td><b>{html.escape(n)}</b><br><small>{html.escape(COMBO_NOTES[n])}</small></td>"
        f"<td><audio controls preload='none' src='{html.escape(n)}.wav'></audio></td></tr>" for n in combo_names)
    (directory / "index.html").write_text(f"""<!doctype html><html lang='ja'><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'><title>Ebon Rewards SFX</title>
<style>body{{font:15px system-ui,'Yu Gothic UI',sans-serif;background:#141016;color:#eadfd6;margin:24px;max-width:1100px}}
td,th{{padding:6px 12px;text-align:left;vertical-align:top;border-bottom:1px solid #2b2330}}small{{color:#a99caa}}
h1,h2{{font-weight:600}}h2{{margin-top:2em;color:#e9d7c0}}audio{{height:32px;width:300px}}table{{border-collapse:collapse;width:100%}}</style>
<h1>黒絹の舞踏会 — 報酬武器の効果音</h1>
<p>125 BPM の拍（1拍 28.8 tick、十六分 7.2 tick）に合わせた試聴用の組み合わせと、全27キューの単体です。
ラウドネスは Ebon の尺度（BS.1770 K 重み付け 400 ms 短時間 LUFS）で、ノートは約 -20、武器は -19 から -12 です。</p>
<h2>組み合わせ</h2>
<table>{combos_html}</table>
{sections}
</html>""", encoding="utf-8", newline="\n")


def attribution_section(report, hashes):
    table = "\n".join(
        f"| {k} | {SOURCES[k][0]} | {SOURCES[k][1]} | `{hashes[k]}` |" for k in sorted(hashes))
    head = f"""### Waltz of the Ebon Manor reward weapon audio — 2026-10-02

Twenty-seven weapon cues for the five Ebon Hatbox weapons, the hatbox and The Last Waltz ([rewards spec](../docs/encounters/ebon-manor/REWARDS.md#art-and-audio)). [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; it reuses the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py). Each cue layers trimmed CC0 recordings with original synthesis (tuned Karplus-Strong silk strings in B minor, a struck-string piano, modal glass and chain partials, fabric rips, band-swept air, thread ratchets, a synthetic hall tail); Note0 to Note7 are pure synthesis. The recordings are the ones already attributed above in the Ebon Manor and Soboro tables (Kenney RPG Audio and the artisticdude Swishes pack on OpenGameArt; Freesound uploads that showed Creative Commons 0 on their pages on 2026-10-01, public HQ preview renders). They stay in the local store, are SHA-256 verified before use and are not committed; the audition WAVs and report stay in the git-ignored `.local`. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip.

| Key | Store file | Source | Source SHA256 |
|---|---|---|---|
{table}
"""
    blocks = []
    for name, r in report.items():
        used = r["sources"]
        authors = sorted({AUTHORS.get(k, "Kenney") for k in used}, key=str.lower)
        creators = "recordings by " + (", ".join(authors[:-1]) + " and " + authors[-1] if len(authors) > 1 else authors[0] if authors else "") + "; synthesis and layering by Convergence with owner-directed Claude assistance"
        mod = ("original synthesis only (Karplus-Strong silk string, tuned to " + f"{NOTES[int(name[4:])][0]}{NOTES[int(name[4:])][1]}" + ")") if name.startswith("Note") else "trimmed, filtered and layered recordings plus original synthesis"
        blocks.append(f"""- Runtime file: `Assets/Sounds/Weapons/EbonRewards/{name}.ogg`
- Asset ID: ebon-reward-sfx-{name.lower()}-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue ({r['seconds']:.2f} s)
- Creator: {creators if used else 'original synthesis by Convergence with owner-directed Claude assistance'}
- Creation/acquisition date: 2026-10-02
- Source type: {'public-domain' if used else 'original'}
- Source work and URL: {', '.join(used) + ' in the table above as selected by the cue recipe; remaining layers original synthesis' if used else 'none; original NumPy synthesis'}
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy {np.__version__}, SciPy {__import__('scipy').__version__}, soundfile {sf.__version__}/libsndfile {sf.__libsndfile_version__} Vorbis at compression level 0.4
- Human modifications: {mod}; short-term loudness {r['short_term_lufs']:.1f} LUFS, true peak {r['true_peak_dbfs']:.1f} dBFS; pinned Ogg serial
- License and redistribution terms: {'CC0 1.0 recordings; the layered cue follows the existing project asset terms' if used else 'original project asset under the existing project terms'}
- Required attribution: {'none required by CC0; retain the table above as courtesy credit' if used else 'none; retain this provenance'}
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks{'; pitch checked against the tuned note within 3 cents' if name.startswith('Note') else ''}; subjective listening and in-game mix not_run
- SHA256: `{r['ogg_sha256']}`
""")
    return head + "\n" + "\n".join(blocks)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--sources", type=Path, required=True, help="local CC0 recording store (never committed)")
    parser.add_argument("--preview", type=Path, help="write WAV previews, combos, report and index.html here")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR, help="Ogg output folder")
    parser.add_argument("--attribution-section", type=Path, help="write the Assets/ATTRIBUTION.md section here")
    args = parser.parse_args()
    store = Store(args.sources, ATTRIBUTION.read_text(encoding="utf-8"))
    samples, report = {}, {}
    for name in CUES:
        samples[name], used = render_cue(name, store, args.output)
        report[name] = analyse(name, samples[name], args.output / f"{name}.ogg", used)
    record = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__).read_bytes()),
              "libraries": {"numpy": np.__version__, "scipy": __import__("scipy").__version__,
                            "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "sources": {k: {"path": SOURCES[k][0], "origin": SOURCES[k][1], "sha256": store.hashes[k]} for k in sorted(store.hashes)},
              "total_ogg_bytes": sum(r["bytes"] for r in report.values()), "cues": report}
    if args.preview:
        args.preview.mkdir(parents=True, exist_ok=True)
        for name, x in samples.items():
            sf.write(str(args.preview / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        mixes = combos(samples)
        for name, x in mixes.items():
            sf.write(str(args.preview / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        page(args.preview, report, list(mixes))
        (args.preview / "ebon-reward-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8", newline="\n")
    if args.attribution_section:
        args.attribution_section.write_text(attribution_section(report, store.hashes), encoding="utf-8", newline="\n")
    print(json.dumps({k: {kk: vv for kk, vv in v.items() if kk in ("seconds", "short_term_lufs", "true_peak_dbfs", "peak_ms", "bytes")}
                      for k, v in report.items()}, indent=1))
    print("total ogg bytes", record["total_ogg_bytes"])


if __name__ == "__main__":
    main()
