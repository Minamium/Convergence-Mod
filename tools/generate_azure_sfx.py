"""Original ice fracture and glass resonance cues for Azure Cathedral.

NumPy synthesis only: no external recordings, game assets or sampled sources.
Exports deterministic 44.1 kHz stereo PCM16 masters and a dry event audition.
"""
import argparse
import hashlib
import json
import wave
from pathlib import Path

import numpy as np

RATE = 44100
SPECS = {
    "IceBreak": (0.88, "break", 0.60),
    "GlassArrival": (0.86, "arrival", 0.52),
    "CrystalCharge": (0.38, "charge", 0.38),
    "CrystalCut": (0.48, "cut", 0.52),
    "WormRush": (0.64, "rush", 0.54),
    "DevourFracture": (1.02, "devour", 0.62),
    "ChainMelt": (2.64, "melt", 0.51),
}


def ease(t, a, b):
    u = np.clip((t - a) / (b - a), 0, 1)
    return u * u * (3 - 2 * u)


def band_noise(count, rng, low, high):
    frequencies = np.fft.rfftfreq(count, 1 / RATE)
    response = (1 - np.exp(-(frequencies / low) ** 4)) * np.exp(-(frequencies / high) ** 2)
    result = np.fft.irfft(np.fft.rfft(rng.normal(size=count)) * response, n=count)
    return result / max(1e-9, np.sqrt(np.mean(result * result)))


def glass(t, rng, onsets, gain=1):
    """Short inharmonic shard grains, with a little paired stereo movement."""
    left, right = np.zeros_like(t), np.zeros_like(t)
    ratios = (1.0, 1.427, 2.173, 2.941)
    for index, onset in enumerate(onsets):
        dt = np.maximum(0, t - onset)
        gate = (t >= onset).astype(float)
        base = rng.uniform(760, 1740) * (1 - index / (len(onsets) + 5) * .28)
        ring = np.zeros_like(t)
        for part, ratio in enumerate(ratios):
            ring += np.sin(2 * np.pi * base * ratio * dt + rng.uniform(-np.pi, np.pi)) / (part + 1) ** 1.45
        ring *= gate * np.exp(-dt / rng.uniform(.018, .061)) * gain
        left += ring * (.78 if index % 2 else 1)
        right += ring * (1 if index % 2 else .78)
    return np.column_stack((left, right))


def synth(name, seconds, kind, peak):
    rng = np.random.default_rng(int.from_bytes(hashlib.sha256(name.encode()).digest()[:8], "little"))
    t = np.arange(round(seconds * RATE)) / RATE
    count = len(t)
    low = band_noise(count, rng, 24, 245)
    crack = band_noise(count, rng, 430, 6400)
    air = band_noise(count, rng, 170, 2500)
    stereo = np.zeros((count, 2))

    if kind == "charge":
        rise = ease(t, 0, .22) * (1 - ease(t, .30, .37))
        tone = np.zeros(count)
        for frequency in (490, 731, 1117, 1693):
            tone += np.sin(2 * np.pi * (frequency * t + 37 * t * t)) * (.22 if frequency == 490 else .085)
        body = (air * .12 + crack * .035 + tone) * rise
        stereo[:] = body[:, None]
        stereo += glass(t, rng, (.13, .21, .28), .033)
    elif kind == "rush":
        swell = ease(t, 0, .045) * (1 - ease(t, .41, .61))
        pitch = 75 + 96 * np.exp(-t / .13)
        phase = 2 * np.pi * np.cumsum(pitch) / RATE
        body = (low * .22 + air * .19 + np.sin(phase) * .29) * swell
        body += crack * .13 * np.exp(-t / .027) * ease(t, 0, .002)
        stereo[:] = body[:, None]
        stereo += glass(t, rng, (.09, .16, .31), .065)
    elif kind == "melt":
        # One descending, whole-chain event. Individual segments never trigger voices.
        body = (low * .11 + air * .10) * ease(t, 0, .10) * (1 - ease(t, 1.7, 2.56))
        for index, onset in enumerate(np.linspace(.05, 1.68, 14)):
            dt = np.maximum(0, t - onset)
            body += crack * (.075 - index * .003) * np.exp(-dt / .026) * (t >= onset)
        stereo[:] = body[:, None]
        stereo += glass(t, rng, tuple(np.linspace(.09, 1.92, 22)), .075)
    else:
        # A low splitting sheet starts the large events; the short bright
        # resonators give the ice/glass identity without continuous white hiss.
        mass = {"break": 1.0, "arrival": .63, "cut": .32, "devour": 1.22}[kind]
        pitch = 67 + 71 * np.exp(-t / .057)
        phase = 2 * np.pi * np.cumsum(pitch) / RATE
        impact = (np.sin(phase) * .38 + low * .35) * np.exp(-t / (.17 if kind == "devour" else .11))
        impact += crack * .26 * np.exp(-t / (.046 if kind == "devour" else .024))
        impact *= ease(t, 0, .0018) * mass
        if kind == "arrival":
            impact += air * .18 * ease(t, 0, .09) * (1 - ease(t, .48, .80))
        elif kind == "cut":
            impact += air * .25 * ease(t, 0, .012) * (1 - ease(t, .17, .40))
        elif kind == "devour":
            impact += low * .18 * ease(t, 0, .08) * (1 - ease(t, .49, .91))
        stereo[:] = impact[:, None]
        onsets = {
            "break": (.012, .036, .061, .10, .16, .24, .33, .46),
            "arrival": (.09, .16, .25, .36, .48, .59),
            "cut": (.014, .042, .081, .14),
            "devour": (.014, .035, .065, .11, .19, .28, .41, .55, .68),
        }[kind]
        stereo += glass(t, rng, onsets, .15 if kind != "cut" else .12)

    # Very short cold reflections retain the initial attack and remain inside
    # the file. A separated stereo side avoids a doubled center in mono.
    dry = stereo.copy()
    for channel in range(2):
        for delay, gain in ((.031 + channel * .007, .11), (.074 - channel * .006, .055)):
            offset = round(delay * RATE)
            stereo[offset:, channel] += dry[:-offset, 1 - channel] * gain
    stereo -= stereo.mean(axis=0)
    stereo *= (ease(t, 0, .0015) * (1 - ease(t, seconds - .068, seconds - .002)))[:, None]
    stereo *= peak / max(1e-9, np.abs(stereo).max())
    return stereo


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(np.clip(samples, -1, 1) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())


def audit(path):
    with wave.open(str(path), "rb") as source:
        assert source.getframerate() == RATE and source.getnchannels() == 2 and source.getsampwidth() == 2
        data = np.frombuffer(source.readframes(source.getnframes()), "<i2").reshape(-1, 2) / 32768
    peak = float(np.abs(data).max())
    rms = float(np.sqrt(np.mean(data * data)))
    dc = float(np.abs(data.mean(axis=0)).max())
    edge = float(np.abs(data[0] - data[-1]).max())
    if peak >= .8 or dc >= .001 or edge >= .003 or rms >= .20:
        raise ValueError(f"Audio bounds exceeded for {path.name}: peak={peak}, rms={rms}, dc={dc}, edge={edge}")
    return {"seconds": round(len(data) / RATE, 3), "peak_dbfs": round(20 * np.log10(peak), 2),
            "rms_dbfs": round(20 * np.log10(max(rms, 1e-12)), 2), "dc": round(dc, 7),
            "boundary_step": round(edge, 7), "clipped_samples": int((np.abs(data) >= 1).sum())}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--preview", type=Path, required=True)
    args = parser.parse_args()
    clips = {}
    report = {}
    for name, (seconds, kind, peak) in SPECS.items():
        clip = synth(name, seconds, kind, peak)
        path = args.output / f"{name}.wav"
        write_wav(path, clip)
        clips[name] = clip
        report[name] = audit(path)

    # Dry sequence at the relative cue levels used in-game. This is for a
    # human listening pass; generated metrics do not establish sound quality.
    events = (("IceBreak", .30, .56), ("GlassArrival", 1.75, .48),
              ("CrystalCharge", 3.05, .35), ("CrystalCut", 3.42, .43),
              ("WormRush", 4.55, .44), ("DevourFracture", 5.65, .56),
              ("ChainMelt", 7.15, .43))
    preview = np.zeros((round(10.25 * RATE), 2))
    for name, when, gain in events:
        start = round(when * RATE)
        clip = clips[name] * gain
        preview[start:start + len(clip)] += clip
    preview_path = args.preview / "Azure-audition.wav"
    write_wav(preview_path, preview)
    report["audition"] = audit(preview_path)
    args.preview.mkdir(parents=True, exist_ok=True)
    (args.preview / "audio-audit.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
