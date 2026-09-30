"""Silk, furniture and hall cues for Waltz of the Ebon Manor.

Layers trimmed CC0 recordings from a local store (--sources, verified by
SHA-256, never committed) with original NumPy synthesis: tuned Karplus-Strong
silk strings, modal glass and chain partials, fabric rips and a synthetic
hall tail. Pitched material follows AutoMatador's B minor (B, D, F#) so plucks
landing on the beat sit inside the score. Writes deterministic 44.1 kHz stereo
Vorbis (pinned Ogg serials) plus an optional WAV preview, a cadence combo and
an audition page. Requires numpy, scipy and soundfile (local audio tools).
"""
import argparse
import hashlib
import html
import io
import json
import zipfile
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

from ogg_tools import fixed_serial

RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Sounds" / "EbonManor"
KENNEY = "pack-OGA-Kenney-RPGsounds.zip"

# Every recording the cues read: store path (zip member after '!'), page, SHA-256.
SOURCES = {
    "kenney_zip": (KENNEY, "https://opengameart.org/content/50-rpg-sound-effects (Kenney, RPG Audio)"),
    "cloth1": (KENNEY + "!OGG/cloth1.ogg", "Kenney RPG Audio cloth1.ogg"),
    "cloth4": (KENNEY + "!OGG/cloth4.ogg", "Kenney RPG Audio cloth4.ogg"),
    "book_flip": (KENNEY + "!OGG/bookFlip3.ogg", "Kenney RPG Audio bookFlip3.ogg"),
    "creak1": (KENNEY + "!OGG/creak1.ogg", "Kenney RPG Audio creak1.ogg"),
    "creak2": (KENNEY + "!OGG/creak2.ogg", "Kenney RPG Audio creak2.ogg"),
    "chop": (KENNEY + "!OGG/chop.ogg", "Kenney RPG Audio chop.ogg"),
    "door_close": (KENNEY + "!OGG/doorClose_4.ogg", "Kenney RPG Audio doorClose_4.ogg"),
    "draw_knife": (KENNEY + "!OGG/drawKnife3.ogg", "Kenney RPG Audio drawKnife3.ogg"),
    "knife_slice": (KENNEY + "!OGG/knifeSlice2.ogg", "Kenney RPG Audio knifeSlice2.ogg"),
    "metal_latch": (KENNEY + "!OGG/metalLatch.ogg", "Kenney RPG Audio metalLatch.ogg"),
    "metal_click": (KENNEY + "!OGG/metalClick.ogg", "Kenney RPG Audio metalClick.ogg"),
    "metal_pot": (KENNEY + "!OGG/metalPot1.ogg", "Kenney RPG Audio metalPot1.ogg"),
    "swoosh": ("swing-FS263595-PorkMuncher-swoosh.mp3", "https://freesound.org/s/263595/ (PorkMuncher, HQ preview)"),
    "air_cut": ("wind-FS60030-qubodup-air_cut.mp3", "https://freesound.org/s/60030/ (qubodup, HQ preview)"),
    "woosh": ("wind-FS683096-florianreichelt-woosh.mp3", "https://freesound.org/s/683096/ (florianreichelt, HQ preview)"),
    "low_impact": ("impact-FS541029-AudioPapkin-very_low_impact.mp3", "https://freesound.org/s/541029/ (AudioPapkin, HQ preview)"),
    "rock_tumble": ("impact-FS389618-_stubb-rock_tumble_2.mp3", "https://freesound.org/s/389618/ (_stubb, HQ preview)"),
    "swish": ("swishes/swish-4.wav", "https://opengameart.org/content/swishes-sound-pack (artisticdude, swish-4.wav)"),
}

# B minor: pitch names to Hz (A4 = 440).
def hz(note, octave):
    return 440.0 * 2 ** ((("C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B").index(note) - 9) / 12 + octave - 4)


# ---------------------------------------------------------------- sources
def sha256(data):
    return hashlib.sha256(data).hexdigest()


class Store:
    def __init__(self, root):
        self.root = Path(root)
        self.cache, self.hashes = {}, {}

    def raw(self, key):
        path = SOURCES[key][0]
        if "!" in path:
            archive, member = path.split("!", 1)
            data = zipfile.ZipFile(self.root / archive).read(member)
        else:
            data = (self.root / path).read_bytes()
        self.hashes[key] = sha256(data)
        return data

    def get(self, key):
        if key not in self.cache:
            x, rate = sf.read(io.BytesIO(self.raw(key)), dtype="float64", always_2d=True)
            if x.shape[1] == 1:
                x = np.repeat(x, 2, axis=1)
            if rate != RATE:
                g = np.gcd(rate, RATE)
                x = signal.resample_poly(x, RATE // g, rate // g, axis=0)
            self.cache[key] = x[:, :2]
        return self.cache[key]


# ---------------------------------------------------------------- helpers
def seconds(n):
    return np.zeros((round(n * RATE), 2))


def place(mix, x, at, gain_db=0.0):
    start = round(at * RATE)
    if start >= len(mix):
        return mix
    end = min(len(mix), start + len(x))
    mix[start:end] += x[: end - start] * 10 ** (gain_db / 20)
    return mix


def fades(x, fade_in=0.002, fade_out=0.02):
    x = x.copy()
    a, b = max(1, round(fade_in * RATE)), max(1, round(fade_out * RATE))
    a, b = min(a, len(x)), min(b, len(x))
    x[:a] *= np.linspace(0, 1, a)[:, None]
    x[-b:] *= np.linspace(1, 0, b)[:, None]
    return x


def cut(x, start, end):
    return fades(x[round(start * RATE): round(end * RATE)])


def speed(x, factor):
    """Varispeed (pitch and time together)."""
    n = max(2, round(len(x) / factor))
    t = np.linspace(0, len(x) - 1, n)
    return np.stack([np.interp(t, np.arange(len(x)), x[:, c]) for c in range(2)], axis=1)


def sos(kind, hz_, order=2):
    return signal.butter(order, hz_, btype=kind, fs=RATE, output="sos")


def hp(x, f, order=2):
    return signal.sosfilt(sos("highpass", f, order), x, axis=0)


def lp(x, f, order=2):
    return signal.sosfilt(sos("lowpass", f, order), x, axis=0)


def bp(x, lo, hi, order=2):
    return signal.sosfilt(sos("bandpass", (lo, hi), order), x, axis=0)


def mono(x):
    return np.column_stack((x, x))


def pan(x, position):
    """position -1 (left) .. 1 (right), equal power; x mono or stereo."""
    m = x.mean(axis=1) if x.ndim == 2 else x
    a = (position + 1) * np.pi / 4
    return np.column_stack((m * np.cos(a), m * np.sin(a))) * np.sqrt(2)


def env(n, attack, decay, hold=0.0):
    t = np.arange(n) / RATE
    rise = np.clip(t / max(attack, 1e-4), 0, 1)
    fall = np.exp(-np.maximum(0, t - attack - hold) / max(decay, 1e-4))
    return rise * fall


def noise(n, rng):
    return rng.standard_normal((n, 2))


# ---------------------------------------------------------------- synthesis
def pluck(freq, dur, rng, bright=0.6, sustain=0.9965, pick=0.18, width=0.25):
    """Karplus-Strong silk string: exact tuning via a fractional two-tap loop."""
    n = round(dur * RATE)
    period = RATE / freq
    length = int(np.floor(period - 0.5))
    frac = period - length - 0.5
    burst = rng.uniform(-1, 1, length + 2)
    burst = signal.lfilter([1 - (1 - bright) * 0.9], [1, -(1 - bright) * 0.9], burst)
    delay = max(1, round(pick * length))
    burst[delay:] -= burst[:-delay] * 0.8
    x = np.zeros(n)
    x[: len(burst)] = burst
    # y[n] = x[n] + g/2 * ((1-f)(y[n-L] + y[n-L-1]) + f(y[n-L-1] + y[n-L-2])): delay L + 1/2 + f.
    a = np.zeros(length + 3)
    a[0] = 1
    a[length] = -sustain * 0.5 * (1 - frac)
    a[length + 1] = -sustain * 0.5
    a[length + 2] = -sustain * 0.5 * frac
    y = signal.lfilter([1.0], a, x)
    y /= max(1e-9, np.abs(y).max())
    # A second, slightly detuned string for stereo width.
    period2 = period * (1 + width * 0.0015)
    y2 = np.interp(np.arange(n) * period / period2, np.arange(n), y)
    fade = np.linspace(1, 0, min(n, round(0.02 * RATE)))
    y[-len(fade):] *= fade
    y2[-len(fade):] *= fade
    return np.column_stack((y * (1 - width * 0.5) + y2 * width * 0.5, y2 * (1 - width * 0.5) + y * width * 0.5))


def chime(freq, dur, rng, decay=0.9, partials=((1, 1.0), (2.76, 0.42), (5.40, 0.20), (8.93, 0.08))):
    """Celesta-like struck bar: inharmonic partials with independent decays."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    y = np.zeros(n)
    for ratio, amp in partials:
        y += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6.28)) * np.exp(-t / (decay / ratio ** 0.7))
    y *= np.clip(t / 0.002, 0, 1) * np.clip((dur - t) / 0.08, 0, 1)
    return pan(y, rng.uniform(-0.4, 0.4))


def glass(dur, rng, count=40, spread=0.35, low=1600, high=6200, decay=(0.03, 0.14)):
    """Shattering glass: many short inharmonic shard pings over a noise bed."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for i in range(count):
        onset = spread * (i / count) ** 1.8 + rng.uniform(0, 0.012)
        f = np.exp(rng.uniform(np.log(low), np.log(high)))
        dt = np.maximum(0, t - onset)
        gate = t >= onset
        ring = sum(np.sin(2 * np.pi * f * r * dt + rng.uniform(0, 6.28)) / (k + 1) ** 1.3
                   for k, r in enumerate((1.0, 1.51, 2.27, 3.12)))
        ring = ring * gate * np.exp(-dt / rng.uniform(*decay)) * (1 - i / count * 0.6)
        out += pan(ring, rng.uniform(-0.8, 0.8)) * 0.6
    bed = hp(noise(n, rng), 2500) * env(n, 0.001, 0.09)[:, None] * 0.35
    return out + bed


def chain(dur, rng, count=14, spread=0.5):
    """Small metal links: bright modal clinks with slight pitch scatter."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for i in range(count):
        onset = rng.uniform(0, spread)
        f = rng.uniform(2300, 4200)
        dt = np.maximum(0, t - onset)
        ring = sum(np.sin(2 * np.pi * f * r * dt) * a for r, a in ((1, 1), (2.1, 0.5), (3.9, 0.25)))
        out += pan(ring * (t >= onset) * np.exp(-dt / rng.uniform(0.015, 0.05)), rng.uniform(-0.6, 0.6)) * 0.4
    return out


def rip(dur, rng, density=(80, 900), low=900, high=7000):
    """Fabric tearing: dense crackling fibres through a moving band."""
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    rate = np.interp(t, (0, dur), density)
    clicks = (rng.uniform(0, 1, n) < rate / RATE).astype(float) * rng.uniform(0.4, 1, n)
    grains = signal.lfilter([1], [1, -0.55], clicks)
    body = bp(noise(n, rng), low, high)[:, 0] * 0.25
    y = grains * 1.2 + body * np.interp(t, (0, dur * 0.1, dur), (0.2, 1, 0.8))
    y = bp(np.column_stack((y, np.roll(y, 37))), low, high)
    return y * env(n, 0.01, dur * 0.5, dur * 0.45)[:, None]


def thump(freq_from, freq_to, dur, rng):
    n = round(dur * RATE)
    t = np.arange(n) / RATE
    f = freq_to + (freq_from - freq_to) * np.exp(-t / 0.05)
    y = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / (dur * 0.35)) * np.clip(t / 0.003, 0, 1)
    return mono(y)


def swell(x, dur):
    """Reverse a reverberated sound into a rising swell ending on its source."""
    tail = hall(x, wet=1.0, dry=0.25, seconds_=dur)
    return fades(tail[::-1], 0.03, 0.004)


_IR = None


def hall(x, wet=0.22, dry=1.0, seconds_=None):
    """Synthetic stereo hall: band-split decaying noise, fixed seed."""
    global _IR
    if _IR is None:
        rng = np.random.default_rng(7171)
        n = round(1.4 * RATE)
        t = np.arange(n) / RATE
        bands = ((None, 900, 0.26), (900, 4000, 0.20), (4000, None, 0.10))
        ir = np.zeros((n, 2))
        for low, high, decay in bands:
            z = rng.standard_normal((n, 2))
            if low and high:
                z = bp(z, low, high)
            elif low:
                z = hp(z, low)
            else:
                z = lp(z, high)
            ir += z * np.exp(-t / decay)[:, None]
        ir[: round(0.012 * RATE)] = 0
        ir /= np.sqrt((ir ** 2).sum(axis=0))
        _IR = ir
    ir = _IR if seconds_ is None else _IR[: round(seconds_ * RATE)]
    wet_sig = np.column_stack([signal.fftconvolve(x[:, c], ir[:, c]) for c in range(2)])
    out = np.zeros((len(wet_sig), 2))
    out[: len(x)] += x * dry
    out += wet_sig * wet
    return out


def trim(x, floor_db=-54):
    level = np.abs(x).max(axis=1)
    keep = np.nonzero(level > np.abs(x).max() * 10 ** (floor_db / 20))[0]
    end = keep[-1] + round(0.03 * RATE) if len(keep) else len(x)
    return fades(x[: min(len(x), end)], 0.0005, 0.03)


# ---------------------------------------------------------------- loudness
def k_weight(x):
    shelf = signal.lfilter([1.53512485958697, -2.69169618940638, 1.19839281085285],
                           [1.0, -1.69065929318241, 0.73248077421585], x, axis=0)
    return signal.lfilter([1.0, -2.0, 1.0], [1.0, -1.99004745483398, 0.99007225036621], shelf, axis=0)


def loudness(x):
    """Short-term (400 ms) maximum loudness, BS.1770 K-weighting."""
    y = k_weight(x)
    block = round(0.4 * RATE)
    if len(y) < block:
        y = np.vstack((y, np.zeros((block - len(y), 2))))
    power = np.convolve((y ** 2).sum(axis=1), np.ones(block) / block, mode="valid")
    return -0.691 + 10 * np.log10(max(power.max(), 1e-12))


def master(x, target_lufs, ceiling_db=-1.0):
    x = x * 10 ** ((target_lufs - loudness(x)) / 20)
    peak = np.abs(signal.resample_poly(x, 4, 1, axis=0)).max()
    ceiling = 10 ** (ceiling_db / 20)
    if peak > ceiling:
        x = x * ceiling / peak
    return x


# ---------------------------------------------------------------- cues
def cue_silk_cast(s, rng):
    mix = seconds(1.2)
    place(mix, pluck(hz("B", 5), 1.1, rng, bright=0.75, sustain=0.9955), 0.0, -2)
    place(mix, pluck(hz("F#", 6), 0.8, rng, bright=0.8, sustain=0.994), 0.004, -12)
    place(mix, hp(cut(s.get("cloth4"), 0.02, 0.20), 2500), 0.0, -8)
    return hall(mix, 0.24)


def cue_thread_yank(s, rng):
    mix = seconds(1.3)
    place(mix, pluck(hz("B", 3), 1.2, rng, bright=0.9, sustain=0.9975, pick=0.08), 0.0, -3)
    place(mix, pluck(hz("B", 4), 0.9, rng, bright=0.9, sustain=0.996), 0.0, -9)
    place(mix, hp(speed(cut(s.get("swoosh"), 0.03, 0.62), 1.45), 350), 0.012, -7)
    place(mix, hp(cut(s.get("air_cut"), 0.04, 0.40), 600), 0.0, -10)
    return hall(mix, 0.18)


def cue_prop_crash(s, rng):
    mix = seconds(1.6)
    place(mix, cut(s.get("door_close"), 0.04, 0.6), 0.0, -2)
    place(mix, speed(cut(s.get("chop"), 0.04, 0.24), 0.82), 0.004, -3)
    place(mix, lp(cut(s.get("rock_tumble"), 0.06, 1.1), 3500), 0.03, -10)
    place(mix, thump(140, 48, 0.5, rng), 0.0, -4)
    place(mix, glass(0.5, rng, count=10, spread=0.12, decay=(0.02, 0.07)), 0.015, -16)
    return hall(mix, 0.22)


def cue_chandelier_creak(s, rng):
    mix = seconds(1.8)
    place(mix, speed(cut(s.get("creak2"), 0.0, 0.83), 0.8), 0.0, -4)
    place(mix, chain(1.2, rng, count=12, spread=0.7), 0.1, -10)
    place(mix, glass(1.2, rng, count=12, spread=0.9, low=2600, high=7200, decay=(0.08, 0.25)), 0.2, -18)
    return hall(mix, 0.3)


def cue_thread_snap(s, rng):
    mix = seconds(0.9)
    click = noise(round(0.004 * RATE), rng) * np.linspace(1, 0, round(0.004 * RATE))[:, None]
    place(mix, hp(click, 1800), 0.0, -10)
    place(mix, pluck(hz("F#", 5), 0.5, rng, bright=0.95, sustain=0.990, pick=0.05), 0.0, -4)
    place(mix, hp(cut(s.get("swish"), 0.03, 0.15), 900), 0.006, -6)
    place(mix, hp(cut(s.get("metal_click"), 0.08, 0.2), 1500), 0.0, -14)
    return hall(mix, 0.2)


def cue_chandelier_shatter(s, rng):
    mix = seconds(2.8)
    place(mix, cut(s.get("low_impact"), 0.0, 1.6), 0.0, -3)
    place(mix, glass(2.2, rng, count=70, spread=0.55, low=1400, high=7600), 0.0, -1)
    place(mix, cut(s.get("metal_pot"), 0.08, 1.4), 0.01, -9)
    place(mix, lp(cut(s.get("rock_tumble"), 0.06, 1.1), 2600), 0.05, -12)
    place(mix, chain(1.4, rng, count=20, spread=0.8), 0.04, -9)
    place(mix, thump(110, 38, 0.8, rng), 0.0, -3)
    return hall(mix, 0.3)


def string_tremolo(freqs, dur, rng, rate_hz=15.0, rise=2.0):
    """Re-bowed strings: fast repeated plucks with a crescendo and a small upward glide."""
    mix = seconds(dur)
    hits = int(dur * rate_hz)
    for i in range(hits):
        at = i / rate_hz + rng.uniform(0, 0.006)
        level = -24 + 22 * (i / hits) ** 1.3
        glide = 2 ** ((-rise + rise * i / hits) / 12)
        for f in freqs:
            place(mix, pluck(f * glide, 0.35, rng, bright=0.5, sustain=0.993, width=0.4), at, level)
    return mix


def cue_loom_tighten(s, rng):
    mix = string_tremolo((hz("B", 2), hz("F#", 3), hz("B", 3), hz("D", 4)), 0.95, rng)
    place(mix, speed(cut(s.get("creak1"), 0.15, 0.66), 0.7), 0.25, -16)
    return hall(mix, 0.25)


def cue_loom_twang(s, rng):
    mix = seconds(2.2)
    chord = (hz("B", 2), hz("F#", 3), hz("B", 3), hz("D", 4), hz("F#", 4), hz("B", 4))
    for i, f in enumerate(chord):
        place(mix, pan(pluck(f, 2.0, rng, bright=0.92, sustain=0.9982, pick=0.1), -0.7 + 1.4 * i / 5), i * 0.007, -6)
    place(mix, hp(cut(s.get("metal_latch"), 0.03, 0.2), 2500), 0.0, -12)
    place(mix, thump(90, 55, 0.35, rng), 0.0, -12)
    return hall(mix, 0.26)


def cue_shears_open(s, rng):
    mix = seconds(1.4)
    place(mix, speed(cut(s.get("draw_knife"), 0.06, 0.48), 0.85), 0.0, 0)
    place(mix, hp(cut(s.get("metal_click"), 0.08, 0.3), 1200), 0.28, -6)
    ring = chime(hz("F#", 5), 1.0, rng, decay=0.5, partials=((1, 1), (2.41, 0.5), (4.1, 0.3), (6.9, 0.15)))
    place(mix, ring, 0.29, -16)
    return hall(mix, 0.2)


def cue_shears_snip(s, rng):
    mix = seconds(1.6)
    place(mix, cut(s.get("knife_slice"), 0.01, 0.42), 0.0, -1)
    place(mix, hp(cut(s.get("metal_latch"), 0.03, 0.22), 800), 0.0, -3)
    place(mix, hp(cut(s.get("metal_latch"), 0.03, 0.22), 1100), 0.038, -6)
    place(mix, rip(0.55, rng, density=(600, 120), low=1200, high=8000), 0.02, -5)
    place(mix, thump(160, 60, 0.3, rng), 0.0, -8)
    return hall(mix, 0.2)


def cue_waltz_open(s, rng):
    mix = seconds(2.0)
    flap = cut(s.get("book_flip"), 0.02, 0.23)
    place(mix, lp(speed(flap, 0.7), 5000), 0.0, -3)
    place(mix, cut(s.get("cloth1"), 0.08, 0.66), 0.0, -8)
    for i, (note, octv) in enumerate((("B", 4), ("D", 5), ("F#", 5), ("B", 5))):
        place(mix, chime(hz(note, octv), 1.4, rng, decay=0.8), 0.06 + i * 0.055, -9 - i)
    return hall(mix, 0.32)


def cue_waltz_release(s, rng):
    mix = seconds(2.6)
    chord = (hz("B", 3), hz("D", 4), hz("F#", 4), hz("B", 4))
    for i, f in enumerate(chord):
        place(mix, pan(pluck(f, 2.4, rng, bright=0.85, sustain=0.9988, pick=0.13), -0.6 + 0.4 * i), i * 0.012, -6)
    w = hp(cut(s.get("woosh"), 0.2, 1.6), 250)
    n = len(w)
    angle = np.linspace(0, 2 * np.pi * 1.2, n)
    circled = np.column_stack((w.mean(1) * (0.5 + 0.5 * np.cos(angle)), w.mean(1) * (0.5 - 0.5 * np.cos(angle))))
    place(mix, circled, 0.0, -9)
    for i, (note, octv) in enumerate((("F#", 5), ("B", 5), ("D", 6))):
        place(mix, chime(hz(note, octv), 1.6, rng, decay=1.0), 0.02 + i * 0.09, -15)
    return hall(mix, 0.3)


def cue_stitch_call(s, rng):
    mix = seconds(1.6)
    place(mix, chime(hz("F#", 6), 1.2, rng, decay=0.7, partials=((1, 1), (3.01, 0.3), (5.2, 0.1))), 0.0, -4)
    place(mix, chime(hz("B", 6), 1.0, rng, decay=0.6, partials=((1, 1), (2.99, 0.25))), 0.09, -9)
    draw = hp(noise(round(0.35 * RATE), rng), 3000) * env(round(0.35 * RATE), 0.2, 0.05)[:, None] * 0.25
    place(mix, draw, 0.05, -8)
    return hall(mix, 0.3)


def cue_stitch_bind(s, rng):
    mix = seconds(2.0)
    for i, f in enumerate((hz("B", 3), hz("D", 4), hz("F#", 4), hz("B", 4))):
        place(mix, pluck(f, 1.8, rng, bright=0.55, sustain=0.9985), i * 0.02, -6)
    place(mix, thump(120, 60, 0.4, rng), 0.0, -8)
    place(mix, cut(s.get("cloth1"), 0.1, 0.5), 0.0, -12)
    place(mix, chime(hz("B", 5), 1.4, rng, decay=0.9), 0.08, -14)
    return hall(mix, 0.28)


def cue_stitch_tear(s, rng):
    mix = seconds(1.6)
    place(mix, rip(0.7, rng, density=(900, 200)), 0.0, -2)
    place(mix, pluck(hz("C", 5), 0.8, rng, bright=0.9, sustain=0.993), 0.0, -10)
    place(mix, pluck(hz("B", 4), 0.8, rng, bright=0.9, sustain=0.993), 0.003, -10)
    place(mix, thump(150, 50, 0.35, rng), 0.0, -7)
    return hall(mix, 0.2)


def cue_weave(s, rng):
    mix = seconds(3.2)
    notes = (("B", 4), ("D", 5), ("F#", 5), ("B", 5), ("D", 6), ("F#", 6), ("B", 6))
    for i, (note, octv) in enumerate(notes):
        place(mix, chime(hz(note, octv), 1.6, rng, decay=0.9), i * 0.137, -8 - i * 0.6)
        place(mix, pluck(hz(note, octv - 1), 1.0, rng, bright=0.5, sustain=0.996), i * 0.137 + 0.004, -20)
    place(mix, cut(s.get("cloth1"), 0.1, 0.66), 0.3, -14)
    place(mix, hp(swell(pluck(hz("B", 3), 0.6, rng), 1.2), 200), 0.0, -12)
    return hall(mix, 0.34)


def cue_silk_burst(s, rng):
    mix = seconds(2.2)
    place(mix, hp(cut(s.get("woosh"), 0.25, 1.4), 300), 0.0, -3)
    for i, f in enumerate((hz("B", 4), hz("D", 5), hz("F#", 5), hz("B", 5))):
        place(mix, pan(pluck(f, 1.6, rng, bright=0.95, sustain=0.9975, pick=0.07), -0.5 + i / 3), 0.0, -8)
    place(mix, thump(90, 40, 0.6, rng), 0.0, -9)
    return hall(mix, 0.3)


def cue_act_change(s, rng):
    # The act turns on the cut: every thread snaps, then the hall rings upward.
    mix = seconds(2.4)
    place(mix, cue_thread_snap(s, rng), 0.0, -2)
    place(mix, cut(s.get("low_impact"), 0.0, 1.3), 0.0, -5)
    for i, (note, octv) in enumerate((("B", 4), ("F#", 5), ("B", 5), ("D", 6))):
        place(mix, chime(hz(note, octv), 1.3, rng, decay=0.7), 0.12 + i * 0.09, -12 - i)
    place(mix, hp(cut(s.get("woosh"), 0.25, 1.3), 300), 0.05, -8)
    return hall(mix, 0.3)


def cue_manor_tear(s, rng):
    mix = seconds(3.4)
    place(mix, rip(2.4, rng, density=(60, 1400), low=700, high=6500), 0.0, 0)
    place(mix, speed(cut(s.get("creak1"), 0.12, 0.66), 0.55), 0.1, -8)
    place(mix, speed(cut(s.get("creak2"), 0.0, 0.83), 0.6), 0.8, -9)
    place(mix, lp(speed(cut(s.get("low_impact"), 0.0, 1.8), 0.7), 400), 0.2, -4)
    place(mix, lp(cut(s.get("rock_tumble"), 0.06, 1.1), 3000), 1.6, -12)
    return hall(mix, 0.3)


def cue_curtain_fall(s, rng):
    mix = seconds(3.6)
    scale = (("B", 5), ("A#", 5), ("F#", 5), ("E", 5), ("D", 5), ("C#", 5), ("B", 4), ("F#", 4), ("D", 4), ("B", 3))
    for i, (note, octv) in enumerate(scale):
        place(mix, pan(pluck(hz(note, octv), 1.6, rng, bright=0.8, sustain=0.9982), 0.6 - 1.2 * i / 9), 0.05 + i * 0.075, -7)
    place(mix, hp(cut(s.get("woosh"), 0.2, 1.6), 200), 0.0, -6)
    place(mix, cut(s.get("low_impact"), 0.0, 1.8), 0.0, -4)
    return hall(mix, 0.36)


CUES = {
    # name: (builder, short-term loudness target in LUFS)
    "SilkCast": (cue_silk_cast, -25),
    "ThreadYank": (cue_thread_yank, -21),
    "PropCrash": (cue_prop_crash, -17),
    "ChandelierCreak": (cue_chandelier_creak, -23),
    "ThreadSnap": (cue_thread_snap, -21),
    "ChandelierShatter": (cue_chandelier_shatter, -15),
    "LoomTighten": (cue_loom_tighten, -23),
    "LoomTwang": (cue_loom_twang, -18),
    "ShearsOpen": (cue_shears_open, -21),
    "ShearsSnip": (cue_shears_snip, -16),
    "WaltzOpen": (cue_waltz_open, -20),
    "WaltzRelease": (cue_waltz_release, -19),
    "StitchCall": (cue_stitch_call, -21),
    "StitchBind": (cue_stitch_bind, -19),
    "StitchTear": (cue_stitch_tear, -17),
    "Weave": (cue_weave, -21),
    "SilkBurst": (cue_silk_burst, -18),
    "ActChange": (cue_act_change, -17),
    "ManorTear": (cue_manor_tear, -16),
    "CurtainFall": (cue_curtain_fall, -17),
}


def render(name, store):
    builder, target = CUES[name]
    rng = np.random.default_rng(int.from_bytes(hashlib.sha256(name.encode()).digest()[:8], "little"))
    x = trim(builder(store, rng))
    return master(x, target)


def write_ogg(path, x):
    path.parent.mkdir(parents=True, exist_ok=True)
    buffer = io.BytesIO()
    sf.write(buffer, x.astype(np.float32), RATE, format="OGG", subtype="VORBIS", compression_level=0.4)
    serial = int.from_bytes(hashlib.sha256(path.stem.encode("utf-8")).digest()[:4], "little")
    path.write_bytes(fixed_serial(buffer.getvalue(), serial))
    decoded, rate = sf.read(str(path), always_2d=True, dtype="float64")
    if rate != RATE or decoded.shape[1] != 2 or not np.isfinite(decoded).all():
        raise ValueError(f"Unexpected OGG decode: {path}")
    return decoded


def combo(samples):
    """One Act One bar of four throws and a chandelier at 125 BPM (a cadence aid)."""
    beat = 60 / 125
    mix = seconds(beat * 12 + 3)
    for k in range(4):
        pitch = (0, 3, 7, 12)[k]
        cast = speed(samples["SilkCast"], 2 ** (pitch / 12))
        place(mix, cast, k * beat, -6)
        place(mix, speed(samples["ThreadYank"], 2 ** (pitch / 12)), (k + 2) * beat, -6)
        place(mix, samples["PropCrash"], (k + 2) * beat + 0.55, -6)
    place(mix, samples["ChandelierCreak"], 6 * beat, -5)
    place(mix, samples["ThreadSnap"], 9 * beat, -5)
    place(mix, samples["ChandelierShatter"], 9 * beat + 0.42, -4)
    return mix * 0.8


def page(directory, names, report):
    rows = "\n".join(
        f"<tr><td>{html.escape(n)}</td><td><audio controls preload='none' src='{html.escape(n)}.wav'></audio></td>"
        f"<td>{report[n]['seconds']:.2f}s</td><td>{report[n]['short_term_lufs']:.1f}</td><td>{report[n]['true_peak_dbfs']:.1f}</td></tr>"
        for n in names)
    (directory / "index.html").write_text(f"""<!doctype html><meta charset='utf-8'><title>Ebon Manor SFX</title>
<style>body{{font:15px system-ui;background:#141016;color:#eadfd6;margin:24px}}td{{padding:4px 12px}}audio{{height:30px}}</style>
<h1>Waltz of the Ebon Manor — SFX</h1><p>Combo: Act One throws on the beat, then a chandelier.</p>
<audio controls src='EbonManor-combo.wav'></audio>
<table><tr><th>Cue</th><th></th><th>Length</th><th>LUFS (short)</th><th>Peak dBFS</th></tr>{rows}</table>""", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sources", type=Path, required=True, help="local CC0 recording store (never committed)")
    parser.add_argument("--preview", type=Path, help="write WAV previews, combo and index.html here")
    args = parser.parse_args()
    store = Store(args.sources)
    samples, report = {}, {}
    for name in CUES:
        x = render(name, store)
        decoded = write_ogg(OUTPUT_DIR / f"{name}.ogg", x)
        samples[name] = decoded
        report[name] = {"seconds": len(decoded) / RATE, "short_term_lufs": round(loudness(decoded), 2),
                        "true_peak_dbfs": round(20 * np.log10(np.abs(signal.resample_poly(decoded, 4, 1, axis=0)).max()), 2),
                        "ogg_sha256": sha256((OUTPUT_DIR / f"{name}.ogg").read_bytes())}
    store.raw("kenney_zip")
    record = {"recipe_sha256": sha256(Path(__file__).read_bytes()),
              "sources": {k: {"path": SOURCES[k][0], "page": SOURCES[k][1], "sha256": store.hashes[k]} for k in sorted(store.hashes)},
              "cues": report}
    if args.preview:
        args.preview.mkdir(parents=True, exist_ok=True)
        for name, x in samples.items():
            sf.write(str(args.preview / f"{name}.wav"), x.astype(np.float32), RATE, subtype="PCM_16")
        sf.write(str(args.preview / "EbonManor-combo.wav"), combo(samples).astype(np.float32), RATE, subtype="PCM_16")
        page(args.preview, list(CUES), report)
        (args.preview / "ebon-sfx-report.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in record.items() if k != "sources"}, indent=1)[:3000])


if __name__ == "__main__":
    main()
