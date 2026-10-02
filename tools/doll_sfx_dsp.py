"""Original synthesis building blocks for the Doll reward weapon cues (tools/generate_doll_weapon_sfx.py).

Every function returns a stereo float array (n, 2) at 44.1 kHz in the convention of
generate_ebon_sfx.py, whose helpers are imported unmodified. No recordings are read here.

- box_tine: a music-box comb tooth by modal synthesis (cantilever partials 1 / 6.267 / 17.55 / 34.39),
  a 0.18 % detuned twin tooth, the pin pluck and the wooden box body; tuned exactly, so ladder notes
  land on the F minor pentatonic ladder below.
- brass_click / ratchet: a brass pawl click (inharmonic 1 / 1.62 / 2.41 / 3.73 partials, 2.5-6 ms
  decays, a 1 ms tick and a gear thud) and a run of them with a 4-tooth pitch pattern.
- porcelain_ring / porcelain_crack: a dry ceramic ring (1 / 2.32 / 4.25 / 6.63 / 9.38) and a crack
  of shard ticks.
- organ_pad: an additive flue-pipe chord (no samples) with a chiff and its wind breath.
- shimmer: a short cloud of tiny high tines over airy noise.
- thump: the pitch-dropping low body of generate_ebon_sfx (re-exported).

Tonal home (binding for every Doll weapon cue): F minor pentatonic F Ab Bb C Eb; ladder F5 Ab5 Bb5
C6 Eb6 F6 Ab6 Bb6 C7, mirrored by Content/Encounters/FirstSeverance/Rewards/DollWeaponTuning.cs.
Requires numpy and scipy (local audio tools, not CI).
"""
import numpy as np
from scipy import signal

from generate_ebon_sfx import RATE, hp, lp, mono, noise, pan, place, thump  # noqa: F401  (thump re-exported)

# Partials above this are dropped so nothing aliases at 44.1 kHz.
CEILING_HZ = 0.45 * RATE

# ---------------------------------------------------------------- tonal home
PENTATONIC = (0, 3, 5, 7, 10)  # semitones above F: F Ab Bb C Eb
LADDER = ("F5", "Ab5", "Bb5", "C6", "Eb6", "F6", "Ab6", "Bb6", "C7")
_LETTERS = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(name):
    """'Ab5' -> 80 (scientific pitch, C4 = 60; 'b' flat, '#' sharp)."""
    letter, rest, shift = name[0], name[1:], 0
    while rest and rest[0] in "b#":
        shift += -1 if rest[0] == "b" else 1
        rest = rest[1:]
    return 12 * (int(rest) + 1) + _LETTERS[letter] + shift


def hz(name, cents=0.0):
    """Equal temperament, A4 = 440 Hz."""
    return 440.0 * 2 ** ((midi(name) - 69 + cents / 100) / 12)


def in_scale(name):
    return (midi(name) - 65) % 12 in PENTATONIC


def ladder(step):
    """Frequency of ladder step 0..8 (clamped), F5 .. C7."""
    return hz(LADDER[max(0, min(len(LADDER) - 1, step))])


# ---------------------------------------------------------------- small helpers
def _t(n):
    return np.arange(n) / RATE


def _norm(x):
    return x / max(1e-12, np.abs(x).max())


def resonator(x, freq, tau):
    """Two-pole resonance at freq with a 1/e decay of tau seconds (unity peak gain at freq, roughly)."""
    r = np.exp(-1.0 / (tau * RATE))
    w = 2 * np.pi * freq / RATE
    return signal.lfilter([1 - r], [1, -2 * r * np.cos(w), r * r], x, axis=0)


def tail_fade(x, seconds_=0.02):
    k = min(len(x), max(1, round(seconds_ * RATE)))
    x = x.copy()
    x[-k:] *= np.linspace(1, 0, k)[:, None] if x.ndim == 2 else np.linspace(1, 0, k)
    return x


# ---------------------------------------------------------------- music box
COMB_MODES = ((1.0, 1.0), (6.267, 0.34), (17.547, 0.08), (34.386, 0.02))
BOX_BODY = ((330, 0.030, 1.0), (720, 0.022, 0.7), (1500, 0.014, 0.45))  # wooden box resonances: Hz, decay s, gain


def box_tine(freq, dur, rng, decay=None, twin=0.0018, width=0.35, pick=0.0025, body=0.5, modes=COMB_MODES):
    """One plucked comb tooth.

    The fundamental decays over tau = 0.42 * (440 / f) ** 0.5 s (clamped 0.10-0.55 s), each higher
    cantilever mode faster (tau / ratio ** 0.75), so the 'plink' fades first and the pure tone rings.
    A quieter neighbour tooth `twin` sharper (left) and flatter (right), plucked by the same pin and so
    starting in phase, beats gently against it: a slow shimmer of about +-3 dB that also widens the note
    (`width` is its level against the main tooth). The main tooth sits exactly on `freq`.
    """
    n = round(dur * RATE)
    t = _t(n)
    tau = decay if decay is not None else float(np.clip(0.42 * (440.0 / freq) ** 0.5, 0.10, 0.55))
    a, up, down = np.zeros(n), np.zeros(n), np.zeros(n)
    for ratio, amp in modes:
        if freq * ratio * (1 + twin) >= CEILING_HZ:
            break
        env = np.exp(-t / (tau / ratio ** 0.75))
        phase = rng.uniform(0, 2 * np.pi)
        a += amp * np.sin(2 * np.pi * freq * ratio * t + phase) * env
        up += amp * np.sin(2 * np.pi * freq * (1 + twin) * ratio * t + phase) * env
        down += amp * np.sin(2 * np.pi * freq * (1 - twin) * ratio * t + phase) * env
    rise = np.clip(t / 0.0004, 0, 1)  # the pin lifts the tooth: a 0.4 ms onset, no click
    out = np.column_stack((a + width * up, a + width * down)) * rise[:, None]
    k = max(8, round(pick * RATE))
    tick = hp(noise(k, rng), 2500) * (np.linspace(1, 0, k) ** 2)[:, None] * 0.22
    out[:k] += tick[:n]
    if body:
        m = round(0.06 * RATE)
        exc = rng.standard_normal(m) * np.exp(-_t(m) / 0.003)
        wood = sum(g * resonator(exc, f, d) for f, d, g in BOX_BODY)
        out[:min(n, m)] += mono(_norm(wood) * 0.16 * body)[:min(n, m)]
    return _norm(tail_fade(out))


# ---------------------------------------------------------------- brass
PAWL_MODES = ((1.0, 1.0), (1.62, 0.7), (2.41, 0.45), (3.73, 0.25))


def brass_click(rng, freq=2600.0, decay=0.0045, dur=0.06, tick=0.5, thud=0.3):
    """One brass pawl dropping into a ratchet tooth: an inharmonic metal ping, a 1 ms tick and a gear thud."""
    n = round(dur * RATE)
    t = _t(n)
    y = np.zeros(n)
    for ratio, amp in PAWL_MODES:
        if freq * ratio >= CEILING_HZ:
            break
        y += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / (decay / ratio ** 0.3))
    y *= np.clip(t / 0.0001, 0, 1)
    k = round(0.001 * RATE)
    y[:k] += hp(noise(k, rng), 3000)[:, 0] * np.linspace(1, 0, k) * tick
    y += np.sin(2 * np.pi * 180 * t) * np.exp(-t / 0.008) * thud * np.clip(t / 0.0005, 0, 1)
    return mono(_norm(tail_fade(y, 0.005)))


def ratchet(times, rng, freq=2600.0, pattern=(1.0, 1.06, 0.97, 1.03), gains_db=None, side=0.0, **click):
    """Pawl clicks at `times` (s); the 4-tooth pattern varies their pitch like a real ratchet wheel."""
    end = max(times) + click.get("dur", 0.06) + 0.01
    out = np.zeros((round(end * RATE), 2))
    for i, at in enumerate(times):
        x = pan(brass_click(rng, freq * pattern[i % len(pattern)], **click), side)
        place(out, x, at, gains_db[i] if gains_db else 0.0)
    return out


# ---------------------------------------------------------------- porcelain
PORCELAIN_MODES = ((1.0, 1.0), (2.32, 0.55), (4.25, 0.30), (6.63, 0.16), (9.38, 0.08))


def porcelain_ring(freq, dur, rng, decay=0.06, side=0.0, modes=PORCELAIN_MODES):
    """A dry ceramic ring (porcelain chime or clink); short, never a long glassy sustain."""
    n = round(dur * RATE)
    t = _t(n)
    y = np.zeros(n)
    for ratio, amp in modes:
        if freq * ratio >= CEILING_HZ:
            break
        y += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 2 * np.pi)) * np.exp(-t / (decay / ratio ** 0.5))
    y *= np.clip(t / 0.0002, 0, 1)
    k = round(0.0006 * RATE)
    y[:k] += hp(noise(k, rng), 4000)[:, 0] * np.linspace(1, 0, k) * 0.3
    return pan(_norm(tail_fade(y, 0.01)), side)


def porcelain_crack(dur, rng, count=8, spread=0.045, low=2200, high=6500, decay=(0.008, 0.025)):
    """A porcelain crack: `count` shard ticks within `spread` s, each a tiny ceramic ring, over a dry snap."""
    out = np.zeros((round(dur * RATE), 2))
    for i in range(count):
        at = spread * (i / max(1, count - 1)) ** 1.4 + rng.uniform(0, 0.002)
        f = float(np.exp(rng.uniform(np.log(low), np.log(high))))
        ring = porcelain_ring(f, min(dur, 0.12), rng, decay=rng.uniform(*decay), side=rng.uniform(-0.6, 0.6))
        place(out, ring, at, -2.5 * i / count + rng.uniform(-2, 0))
    k = round(0.004 * RATE)
    place(out, hp(noise(k, rng), 3000) * np.linspace(1, 0, k)[:, None] ** 2 * 0.5, 0.0)
    return _norm(out)


# ---------------------------------------------------------------- organ
def organ_pad(freqs, dur, rng, attack=0.25, release=0.4, harmonics=10, rolloff=1.3, chiff=0.05, breath=0.08,
              spread=0.6, detune_cents=0.8):
    """An additive flue-pipe chord: each pipe sums harmonics 1..n at 1/h**rolloff, slightly detuned against the
    others; a raised-cosine swell of `attack` s, a cosine release over the last `release` s, a short chiff
    at the speech of each pipe and the organ's wind (breath) under the whole chord."""
    n = round(dur * RATE)
    t = _t(n)
    a = np.clip(t / max(attack, 1e-3), 0, 1)
    env = (0.5 - 0.5 * np.cos(np.pi * a)) * np.clip((dur - t) / max(release, 1e-3), 0, 1) ** 1.5
    out = np.zeros((n, 2))
    for i, f0 in enumerate(freqs):
        f = f0 * 2 ** (rng.uniform(-detune_cents, detune_cents) / 1200)
        y = np.zeros(n)
        for h in range(1, harmonics + 1):
            if f * h >= CEILING_HZ:
                break
            y += np.sin(2 * np.pi * f * h * t + rng.uniform(0, 2 * np.pi)) / h ** rolloff
        k = round(0.035 * RATE)
        ch = signal.lfilter(*signal.butter(2, (min(3 * f, CEILING_HZ * 0.9), min(6 * f, CEILING_HZ)), "bandpass", fs=RATE),
                            rng.standard_normal(k)) * np.sin(np.linspace(0, np.pi, k)) * chiff * 4
        y[:k] += ch
        side = 0.0 if len(freqs) == 1 else -spread + 2 * spread * i / (len(freqs) - 1)
        out += pan(_norm(y) * env, side) / len(freqs)
    wind = lp(hp(noise(n, rng), 300), 2600) * env[:, None] * breath
    return out + wind


# ---------------------------------------------------------------- shimmer
def shimmer(dur, rng, notes=("F7", "Ab7", "C8", "Eb8"), count=9, air=0.12, decay=(0.06, 0.16)):
    """A brief sparkle: tiny high comb tines (first two modes only) thinning out over `dur`, over airy noise
    with a fast flutter, spread wide."""
    n = round(dur * RATE)
    out = np.zeros((n, 2))
    for i in range(count):
        at = dur * 0.7 * (i / max(1, count - 1)) ** 1.6 + rng.uniform(0, 0.01)
        x = box_tine(hz(notes[int(rng.integers(0, len(notes)))]), min(0.3, dur), rng, decay=rng.uniform(*decay),
                     modes=COMB_MODES[:2], body=0, pick=0.0008, width=0.4)
        place(out, pan(x, rng.uniform(-0.8, 0.8)), at, -1.5 * i - rng.uniform(0, 3))
    t = _t(n)
    flutter = 0.6 + 0.4 * np.sin(2 * np.pi * rng.uniform(13, 17) * t + rng.uniform(0, 6.28))
    hiss = signal.sosfilt(signal.butter(2, (6000, 11000), "bandpass", fs=RATE, output="sos"), noise(n, rng), axis=0)
    out += _norm(hiss) * (flutter * np.exp(-t / (dur * 0.35)) * np.clip(t / 0.01, 0, 1))[:, None] * air
    return out
