"""Original massive-metal Raid cues: deterministic synthesis, no sampled works.

NumPy only. The generator is the source; keep external audition masters as well
as the exported PCM16 files. Do not overwrite the earlier shared weapon cues.
"""
import argparse
from pathlib import Path
import wave

import numpy as np

RATE = 48000
RNG = np.random.default_rng(2370909)


def bed(size, low, high):
    """Aperiodic pressure/friction, smooth spectral skirts rather than hiss."""
    f = np.fft.rfftfreq(size, 1 / RATE)
    spectrum = np.fft.rfft(RNG.normal(size=size))
    shape = (1 - np.exp(-(f / low) ** 4)) * np.exp(-(f / high) ** 2)
    signal = np.fft.irfft(spectrum * shape, n=size)
    return signal / max(np.sqrt(np.mean(signal * signal)), 1e-9)


def mass(seconds, root=58, weight=1, metal=.5):
    t = np.arange(round(seconds * RATE)) / RATE
    # A dense, inharmonic slab, not a cartoon pitch sweep or isolated sine kick.
    phase = 2 * np.pi * (root * t + root * .014 * (1 - np.exp(-t / .035)))
    body = (.60 * np.sin(phase) + .25 * np.sin(phase * 2.012)
            + .14 * np.sin(phase * 3.09)) * np.exp(-t / .24) * weight
    slab = np.zeros_like(t)
    for i in range(24):
        f = 112 + i * 33.7 + i ** 1.44 * 6.3
        slab += np.sin(2 * np.pi * f * t + .035 * np.sin(2 * np.pi * 13.7 * t)) \
            * np.exp(-t / (.40 / (1 + i * .13))) / (1 + i * .3) ** 1.1
    body += slab * metal * .55
    body += bed(len(t), 28, 280) * np.exp(-t / .16) * .32 * weight
    body += bed(len(t), 160, 2200) * np.exp(-t / .045) * .27
    body *= 1 - np.exp(-t / .0018)
    return body


def place(dest, signal, at, gain=1):
    start = round(at * RATE)
    length = min(len(signal), len(dest) - start)
    if length > 0:
        dest[start:start + length] += signal[:length] * gain


def room(signal, strength=.19):
    result = signal.copy()
    # Diffuse, irregular early reflections; no huge wet tail obscuring timing.
    for i, delay in enumerate((.037, .059, .083, .113, .149, .191, .239, .293)):
        offset = round(delay * RATE)
        result[offset:] += signal[:-offset] * strength * np.exp(-i * .29) * (-1 if i % 3 == 1 else 1)
    return result


def render(name, seconds):
    t = np.arange(round(seconds * RATE)) / RATE
    out = np.zeros_like(t)
    if name == "IronPressure":
        ramp = np.clip(t / .65, 0, 1)
        out += bed(len(t), 35, 710) * (.12 + .4 * ramp ** 2)
        out += .16 * np.sin(2 * np.pi * 62 * t + .16 * np.sin(2 * np.pi * 17 * t)) * ramp
        place(out, mass(.5, 63, .5, .7), .005, .40)
    elif name == "IronDescent":
        # Fast blade intake, then a low struck slab and a restrained edge crack.
        out += bed(len(t), 65, 950) * np.exp(-((t - .027) / .022) ** 2) * .17
        place(out, mass(.9, 53, 1.25, .92), .038)
        place(out, mass(.4, 82, .25, .8), .070, .34)
    elif name == "ShellMassLatch":
        place(out, mass(.8, 67, .80, .90), .008)
        place(out, mass(.45, 48, .50, .6), .062, .42)
    elif name == "ShellMassArc":
        pressure = bed(len(t), 70, 950)
        for at, gain in ((.008, .32), (.065, .25), (.103, .21), (.172, .14)):
            out += pressure * np.exp(-np.maximum(t - at, 0) / .03) * (t >= at) * gain
        place(out, mass(.4, 74, .45, .28), .014, .38)
    elif name == "ShellMassShed":
        # Pressure releases; uneven falling chunks, never the failure implosion.
        out += bed(len(t), 30, 360) * (1 - np.exp(-t / .018)) * np.exp(-t / .38) * .19
        for i, at in enumerate((.018, .09, .187, .31, .487, .69, .88)):
            place(out, mass(.6, 83 - i * 4, .56, .9), at, .65 * np.exp(-i * .30))
    elif name == "ShellMassCollapse":
        out += bed(len(t), 35, 600) * np.exp(-((t - .022) / .018) ** 2) * .32
        place(out, mass(1.5, 46, 1.4, 1), .031)
        place(out, mass(.85, 67, .7, 1), .078, .38)
        place(out, mass(.7, 89, .4, .85), .143, .20)
    elif name == "CrushPressure":
        # 150-tick brace: mechanical loading, a tiny braking hold, rapid final pull.
        ramp = np.clip(t / 2.5, 0, 1)
        tension = .08 + .35 * ramp ** .6 + .5 * np.clip((ramp - .87) / .13, 0, 1) ** 2
        out += bed(len(t), 28, 430) * tension * .28
        for i, f in enumerate((41.3, 63.1, 98.7, 153.2, 221.8)):
            out += np.sin(2 * np.pi * f * t + .19 * np.sin(2 * np.pi * (1.1 + i * .43) * t)) * tension * .085
        for at, gain in ((.008, .7), (.42, .36), (1.07, .32), (1.76, .28), (2.29, .30)):
            place(out, mass(.5, 56, .75, .8), at, gain)
    elif name == "CrushCataclysm":
        place(out, mass(2, 39, 1.65, .85), .006)
        place(out, mass(1.4, 62, .85, 1.3), .029, .50)
        place(out, mass(1.0, 92, .6, .95), .096, .25)
        out += bed(len(t), 24, 210) * (1 - np.exp(-t / .03)) * np.exp(-t / .45) * .3
    else:
        raise ValueError(name)
    out = room(out)
    out -= np.mean(out)
    out = np.tanh(out * 1.15)
    # Remove DC/subsonics, taper endpoints and leave headroom for overlapping cues.
    f = np.fft.rfftfreq(len(out), 1 / RATE)
    out = np.fft.irfft(np.fft.rfft(out) * (1 - np.exp(-(f / 24) ** 4)), n=len(out))
    out *= np.minimum(t / .003, 1) * np.minimum((seconds - t) / .10, 1)
    out *= .78 / max(np.max(np.abs(out)), 1e-9)
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for name, seconds in (("IronPressure", .65), ("IronDescent", 1.1), ("ShellMassLatch", .86), ("ShellMassArc", .5),
                          ("ShellMassShed", 1.55), ("ShellMassCollapse", 1.7),
                          ("CrushPressure", 2.5), ("CrushCataclysm", 2.15)):
        data = render(name, seconds)
        assert np.all(np.isfinite(data))
        path = args.output / f"{name}.wav"
        with wave.open(str(path), "wb") as out:
            out.setnchannels(1)
            out.setsampwidth(2)
            out.setframerate(RATE)
            out.writeframes(np.rint(data * 32767).astype("<i2").tobytes())
        with wave.open(str(path), "rb") as src:
            decoded = np.frombuffer(src.readframes(src.getnframes()), dtype="<i2").astype(float) / 32768
            assert src.getnchannels() == 1 and src.getframerate() == RATE and src.getsampwidth() == 2
        assert np.max(np.abs(decoded)) < .781 and np.max(np.abs(decoded[[0, -1]])) < .002
        power = np.abs(np.fft.rfft(decoded)) ** 2
        f = np.fft.rfftfreq(len(decoded), 1 / RATE)
        bass = np.sum(power[(f >= 30) & (f < 300)]) / np.sum(power)
        print(f"{name}: {seconds:.2f}s, peak={np.max(np.abs(decoded)):.4f}, "
              f"RMS={np.sqrt(np.mean(decoded ** 2)):.4f}, 30-300Hz energy={bass:.1%}, PCM16 mono 48kHz")


if __name__ == "__main__":
    main()
