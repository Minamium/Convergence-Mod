"""Shared layering, loudness and deterministic Vorbis export for recorded SFX.

Each cue is a score entry (seconds, loudness target dB, layers). A layer cuts
a window from a recording, filters it and places it on the cue timeline; a
thump is an original pitch-dropping sine body. Scripts that use this module
own their sources, scores, output folders and auditions. Requires numpy, scipy
and soundfile (local audio tools, not CI).
"""
import hashlib
import io
import math
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

RATE = 44100


def layer(source, start, end, at=0.0, gain_db=0.0, speed=1.0, hp=None, lp=None,
          fade_in=0.004, fade_out=0.03, eq=(), drive=0.0, width=1.0):
    """One cut of a recording placed on the cue timeline.

    start/end are source seconds (before speed); at is the cue time in seconds.
    speed > 1 plays faster and higher. eq entries are (kind, hz, gain_db, q).
    """
    return dict(source=source, start=start, end=end, at=at, gain_db=gain_db, speed=speed,
                hp=hp, lp=lp, fade_in=fade_in, fade_out=fade_out, eq=eq, drive=drive,
                width=width)


def thump(at, gain_db, start_hz, end_hz, seconds):
    """An original pitch-dropping sine body (no recording)."""
    return dict(synth="thump", at=at, gain_db=gain_db, start_hz=start_hz, end_hz=end_hz,
                seconds=seconds)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load(path):
    data, rate = sf.read(str(path), always_2d=True, dtype="float64")
    if data.shape[1] == 1:
        data = np.repeat(data, 2, axis=1)
    data = data[:, :2]
    if rate != RATE:
        g = math.gcd(RATE, rate)
        data = signal.resample_poly(data, RATE // g, rate // g, axis=0)
    return data


def biquad(kind, hz, gain_db, q):
    # RBJ cookbook peaking / shelving sections.
    a = 10 ** (gain_db / 40)
    w = 2 * math.pi * hz / RATE
    alpha = math.sin(w) / (2 * q)
    cos = math.cos(w)
    if kind == "peak":
        b = [1 + alpha * a, -2 * cos, 1 - alpha * a]
        den = [1 + alpha / a, -2 * cos, 1 - alpha / a]
    elif kind in ("low", "high"):
        sign = 1 if kind == "low" else -1
        root = 2 * math.sqrt(a) * alpha
        b = [a * ((a + 1) - sign * (a - 1) * cos + root),
             sign * 2 * a * ((a - 1) - sign * (a + 1) * cos),
             a * ((a + 1) - sign * (a - 1) * cos - root)]
        den = [(a + 1) + sign * (a - 1) * cos + root,
               -sign * 2 * ((a - 1) + sign * (a + 1) * cos),
               (a + 1) + sign * (a - 1) * cos - root]
    else:
        raise ValueError(f"Unknown EQ kind: {kind}")
    return np.array(b) / den[0], np.array(den) / den[0]


def fades(x, fade_in, fade_out):
    n = len(x)
    a = min(n // 2, max(1, round(fade_in * RATE)))
    b = min(n // 2, max(1, round(fade_out * RATE)))
    x[:a] *= (0.5 - 0.5 * np.cos(np.linspace(0, math.pi, a)))[:, None]
    x[-b:] *= (0.5 + 0.5 * np.cos(np.linspace(0, math.pi, b)))[:, None]
    return x


def render_layer(spec, sources):
    if spec.get("synth") == "thump":
        n = round(spec["seconds"] * RATE)
        t = np.arange(n) / RATE
        f = spec["end_hz"] + (spec["start_hz"] - spec["end_hz"]) * np.exp(-t / (spec["seconds"] * 0.25))
        phase = 2 * math.pi * np.cumsum(f) / RATE
        body = np.sin(phase) * np.exp(-t / (spec["seconds"] * 0.32))
        x = np.repeat(body[:, None], 2, axis=1)
        return fades(x, 0.001, spec["seconds"] * 0.35) * 10 ** (spec["gain_db"] / 20) * 0.7
    src = sources[spec["source"]]
    a, b = round(spec["start"] * RATE), round(spec["end"] * RATE)
    if not 0 <= a < b <= len(src):
        raise ValueError(f"Window outside source: {spec}")
    x = src[a:b].copy()
    if spec["speed"] != 1.0:
        up, down = round(1000 / spec["speed"]), 1000
        g = math.gcd(up, down)
        x = signal.resample_poly(x, up // g, down // g, axis=0)
    if spec["hp"]:
        x = signal.sosfilt(signal.butter(2, spec["hp"], "highpass", fs=RATE, output="sos"), x, axis=0)
    if spec["lp"]:
        x = signal.sosfilt(signal.butter(2, spec["lp"], "lowpass", fs=RATE, output="sos"), x, axis=0)
    for kind, hz, gain, q in spec["eq"]:
        x = signal.lfilter(*biquad(kind, hz, gain, q), x, axis=0)
    if spec["drive"]:
        level = max(1e-6, float(np.max(np.abs(x))))
        x = np.tanh(x / level * spec["drive"]) * level
    if spec["width"] != 1.0:
        mid, side = (x[:, 0] + x[:, 1]) / 2, (x[:, 0] - x[:, 1]) / 2 * spec["width"]
        x = np.stack([mid + side, mid - side], axis=1)
    x = fades(x, spec["fade_in"], spec["fade_out"])
    return x * 10 ** (spec["gain_db"] / 20)


def k_weight(x):
    # ITU-R BS.1770 pre-filter (high shelf) and RLB high-pass at 44.1 kHz.
    shelf = biquad("high", 1500, 4.0, 1 / math.sqrt(2))
    x = signal.lfilter(*shelf, x, axis=0)
    return signal.sosfilt(signal.butter(2, 60, "highpass", fs=RATE, output="sos"), x, axis=0)


def loudness(x):
    """Peak 100 ms K-weighted level in dB (short-cue proxy for momentary loudness)."""
    power = np.sum(k_weight(x) ** 2, axis=1)
    window = round(0.1 * RATE)
    if len(power) <= window:
        return 10 * math.log10(max(1e-12, float(np.mean(power))))
    cumulative = np.concatenate([[0.0], np.cumsum(power)])
    return 10 * math.log10(max(1e-12, float(np.max(cumulative[window:] - cumulative[:-window]) / window)))


def true_peak(x):
    return float(np.max(np.abs(signal.resample_poly(x, 4, 1, axis=0))))


def render(entry, sources):
    seconds, target, layers = entry
    n = round(seconds * RATE)
    mix = np.zeros((n, 2))
    for spec in layers:
        x = render_layer(spec, sources)
        at = round(spec["at"] * RATE)
        take = min(len(x), n - at)
        if take <= 0:
            raise ValueError(f"Layer outside cue: {spec}")
        mix[at:at + take] += x[:take]
    mix -= np.mean(mix, axis=0)
    mix = fades(mix, 0.0015, min(0.06, seconds * 0.25))
    mix *= 10 ** ((target - loudness(mix)) / 20)
    # Gentle bus saturation, then restore the loudness target under a -1.5 dBTP ceiling.
    level = 0.85
    mix = np.tanh(mix / level) * level
    mix *= 10 ** ((target - loudness(mix)) / 20)
    peak = true_peak(mix)
    ceiling = 10 ** (-1.5 / 20)
    if peak > ceiling:
        mix *= ceiling / peak
    mix = fades(mix, 0.001, 0.004)
    return np.concatenate([mix, np.zeros((round(0.01 * RATE), 2))])


def metrics(x):
    mono = x.mean(axis=1)
    frames = np.array([math.sqrt(float(np.mean(mono[i:i + 441] ** 2)) + 1e-12)
                       for i in range(0, len(mono) - 441, 441)])
    f, p = signal.welch(mono, RATE, nperseg=min(2048, len(mono)))
    total = float(np.sum(p)) + 1e-18
    return {
        "seconds": round(len(x) / RATE, 4),
        "loudness_k100_db": round(loudness(x), 2),
        "true_peak_dbfs": round(20 * math.log10(true_peak(x)), 2),
        "envelope_peak_ms": int(np.argmax(frames)) * 10,
        "centroid_hz": round(float(np.sum(f * p) / total)),
        "band_share": {label: round(float(np.sum(p[(f >= lo) & (f < hi)]) / total), 3)
                       for label, lo, hi in (("low<200", 0, 200), ("mid200-2k", 200, 2000),
                                             ("presence2k-6k", 2000, 6000), ("air>6k", 6000, RATE / 2))},
        "boundary": round(float(max(np.max(np.abs(x[0])), np.max(np.abs(x[-1])))), 6),
    }


def ogg_crc_table():
    table = []
    for index in range(256):
        value = index << 24
        for _ in range(8):
            value = ((value << 1) ^ 0x04C11DB7) if value & 0x80000000 else value << 1
        table.append(value & 0xFFFFFFFF)
    return table


OGG_CRC = ogg_crc_table()


def fixed_serial(data, serial):
    # libsndfile picks a random Ogg stream serial per write. Pin it (and each
    # page CRC) so an unchanged score reproduces byte-identical files.
    out = bytearray(data)
    position = 0
    while position < len(out):
        if out[position:position + 4] != b"OggS":
            raise ValueError("Unexpected Ogg page layout")
        segments = out[position + 26]
        size = 27 + segments + sum(out[position + 27:position + 27 + segments])
        out[position + 14:position + 18] = serial.to_bytes(4, "little")
        out[position + 22:position + 26] = bytes(4)
        crc = 0
        for byte in out[position:position + size]:
            crc = ((crc << 8) & 0xFFFFFFFF) ^ OGG_CRC[((crc >> 24) & 0xFF) ^ byte]
        out[position + 22:position + 26] = crc.to_bytes(4, "little")
        position += size
    return bytes(out)


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
