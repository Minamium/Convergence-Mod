"""Synthesize original Soboro blade and material-hit cues.

No recordings, extracted game audio, or third-party samples are used. A fixed
seed per cue makes the PCM and Vorbis exports reproducible. The preview WAV and
numeric report are local audition aids, not a claim of subjective acceptance.
"""

import argparse
import hashlib
import json
import subprocess
import wave
from pathlib import Path

import numpy as np

RATE = 44100
TAU = 2 * np.pi
SPECS = {
    "CutDown": (0.285, "down", 0.65),
    "CutReverse": (0.255, "reverse", 0.64),
    "CutHeavy": (0.415, "heavy", 0.72),
    "HitOrganic": (0.155, "organic", 0.60),
    "HitMetal": (0.225, "metal", 0.60),
}


def smooth(t, start, end):
    x = np.clip((t - start) / (end - start), 0, 1)
    return x * x * (3 - 2 * x)


def filtered_noise(size, rng, low, high):
    frequencies = np.fft.rfftfreq(size, 1 / RATE)
    response = (1 - np.exp(-(frequencies / low) ** 4)) * np.exp(-(frequencies / high) ** 4)
    samples = np.fft.irfft(np.fft.rfft(rng.standard_normal(size)) * response, n=size)
    return samples / max(1e-9, np.sqrt(np.mean(samples * samples)))


def chirp(t, beginning, end, duration):
    rate = (end - beginning) / duration
    return np.sin(TAU * (beginning * t + 0.5 * rate * t * t))


def resonator(t, frequency, onset, lifetime, phase=0):
    elapsed = np.maximum(0, t - onset)
    return (t >= onset) * np.exp(-elapsed / lifetime) * np.sin(TAU * frequency * elapsed + phase)


def synthesize(name, duration, kind, target_peak):
    seed = int.from_bytes(hashlib.sha256(name.encode("ascii")).digest()[:8], "little")
    rng = np.random.default_rng(seed)
    t = np.arange(round(RATE * duration)) / RATE
    n = len(t)
    air = filtered_noise(n, rng, 580, 5300)
    edge = filtered_noise(n, rng, 1500, 8400)
    grit = filtered_noise(n, rng, 300, 2900)
    stereo = np.zeros((n, 2), dtype=np.float64)

    if kind in ("down", "reverse", "heavy"):
        # A compressed blade rush has a short bite, a moving center, and an
        # inharmonic violet-metal afterglow. Each stroke has its own trajectory.
        if kind == "down":
            center, end, direction = .055, .168, 1
            rush = smooth(t, .002, .017) * (1 - smooth(t, .088, .190))
            electric = chirp(t, 1470, 545, .155) * np.exp(-t / .068)
            scratch = resonator(t, 1710, .038, .039) + .44 * resonator(t, 2587, .047, .031)
            bite = .23 * edge * np.exp(-t / .009)
            body = .37 * air * rush + .15 * grit * rush + .25 * electric + .20 * scratch + bite
        elif kind == "reverse":
            center, end, direction = .039, .152, -1
            rush = smooth(t, .001, .012) * (1 - smooth(t, .073, .173))
            electric = chirp(t, 760, 1970, .135) * np.exp(-t / .054)
            scratch = resonator(t, 2130, .025, .028) + .38 * resonator(t, 3110, .057, .021)
            bite = .28 * edge * np.exp(-t / .006)
            body = .32 * air * rush + .14 * grit * rush + .28 * electric + .17 * scratch + bite
        else:
            center, end, direction = .080, .255, 1
            rush = smooth(t, .003, .023) * (1 - smooth(t, .158, .279))
            electric = chirp(t, 620, 205, .210) * np.exp(-t / .106)
            serrations = (1 + .37 * np.sin(TAU * (73 * t + 190 * t * t)))
            scratch = (resonator(t, 1088, .046, .085)
                       + .62 * resonator(t, 1817, .080, .070)
                       + .35 * resonator(t, 2771, .111, .048))
            bite = .28 * edge * np.exp(-t / .010)
            body = .43 * air * rush * serrations + .19 * grit * rush
            body += .27 * electric + .24 * scratch + bite
        # Short cross-channel delay gives a directional cut without collapsing
        # the center signal or filling the intentional quiet tail with hiss.
        pan = np.clip((t - center) / (end - center), -1, 1) * direction
        stereo[:, 0] = body * (0.91 - .13 * pan)
        stereo[:, 1] = body * (0.91 + .13 * pan)
        delay = round(.009 * RATE)
        stereo[delay:, 1 if direction > 0 else 0] += body[:-delay] * .095
    elif kind == "organic":
        # Fibrous slice: dry split, brief granular rip, no bass thump.
        rip = smooth(t, .001, .006) * (1 - smooth(t, .048, .114))
        body = .35 * edge * np.exp(-t / .011) + .30 * grit * rip
        body += .25 * resonator(t, 740, .013, .025) + .10 * resonator(t, 1190, .029, .022)
        stereo[:, 0] = body
        stereo[:, 1] = body * .88 + .07 * air * rip
    else:
        # Hard target: an instant metallic chip followed by two deliberately
        # inharmonic rings, short enough to clear the next combo stroke.
        strike = .32 * edge * np.exp(-t / .007) + .16 * grit * np.exp(-t / .025)
        ring = (.36 * resonator(t, 1140, .006, .051)
                + .23 * resonator(t, 1769, .009, .072)
                + .12 * resonator(t, 2933, .017, .040))
        stereo[:, 0] = strike + ring
        stereo[:, 1] = strike * .90 + .32 * resonator(t, 1140, .010, .052)
        stereo[:, 1] += .25 * resonator(t, 1769, .015, .070) + .11 * resonator(t, 2933, .019, .041)

    stereo -= np.mean(stereo, axis=0)
    stereo *= (smooth(t, 0, .0015) * (1 - smooth(t, duration - .041, duration - .002)))[:, None]
    stereo *= target_peak / max(1e-9, np.max(np.abs(stereo)))
    return stereo


def write_wav(path, signal):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(np.clip(signal, -1, 1) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())


def metrics(signal):
    peak = float(np.max(np.abs(signal)))
    rms = float(np.sqrt(np.mean(signal * signal)))
    dc = float(np.max(np.abs(np.mean(signal, axis=0))))
    edge = float(np.max(np.abs(signal[0] - signal[-1])))
    if peak >= .8 or rms >= .22 or dc >= .001 or edge >= .003:
        raise ValueError(f"Audio bound exceeded: {peak=}, {rms=}, {dc=}, {edge=}")
    return {
        "seconds": round(len(signal) / RATE, 4),
        "peak_dbfs": round(20 * np.log10(peak), 2),
        "rms_dbfs": round(20 * np.log10(rms), 2),
        "dc": round(dc, 7),
        "boundary_step": round(edge, 7),
        "clipped_samples": int(np.count_nonzero(np.abs(signal) >= 1)),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="Distributable OGG directory")
    parser.add_argument("--preview", type=Path, required=True, help="Local WAV/report directory")
    parser.add_argument("--ffmpeg", type=Path, required=True)
    parser.add_argument("--preview-only", action="store_true", help="Refresh only the local audition/report")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.preview.mkdir(parents=True, exist_ok=True)
    clips = {}
    report = {}
    for name, (duration, kind, peak) in SPECS.items():
        signal = synthesize(name, duration, kind, peak)
        clips[name] = signal
        report[name] = metrics(signal)
        if not args.preview_only:
            wav = args.preview / f"{name}.wav"
            write_wav(wav, signal)
            subprocess.run([str(args.ffmpeg), "-hide_banner", "-loglevel", "error", "-y",
                            "-i", str(wav), "-fflags", "+bitexact", "-flags:a", "+bitexact",
                            "-c:a", "libvorbis", "-q:a", "5",
                            str(args.output / f"{name}.ogg")], check=True)

    # Two ideal 60-tick cycles, with actual release ticks 7/24/45. Contact
    # examples follow by 3/3/6 ticks. This is a dry cadence audition, not
    # measured native contact timing or an in-game recording.
    strokes = (("CutDown", "HitOrganic", 7, 3, .72, .54),
               ("CutReverse", "HitMetal", 24, 3, .68, .52),
               ("CutHeavy", "HitMetal", 45, 6, .86, .62))
    leading = .15
    preview_length = leading + (60 + 45 + 6) / 60 + SPECS["HitMetal"][0] + .5
    preview = np.zeros((round(preview_length * RATE), 2))
    for cycle in range(2):
        for cut, hit, release_tick, contact_delay, cut_gain, hit_gain in strokes:
            for name, tick, gain in ((cut, release_tick, cut_gain),
                                     (hit, release_tick + contact_delay, hit_gain)):
                at = leading + (cycle * 60 + tick) / 60
                start = round(at * RATE)
                signal = clips[name] * gain
                preview[start:start + len(signal)] += signal
    preview_peak = np.max(np.abs(preview))
    if preview_peak > .95:
        preview *= .95 / preview_peak
    write_wav(args.preview / "Soboro-combo-audition.wav", preview)
    report["audition"] = metrics(preview)
    (args.preview / "audio-audit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
