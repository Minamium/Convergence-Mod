"""Original periodic synthesis for weapon sustain beds (no samples/recordings).

All oscillator and modulation frequencies are multiples of 1/4 Hz: each four
second stereo loop is phase-continuous. Not a launch sound repeated on hit ticks.
"""
import argparse
from pathlib import Path
import wave
import numpy as np


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    rate = 44100
    time = np.arange(rate * 4, dtype=np.float64) / rate
    rng = np.random.default_rng(3426)
    for name, root, pulse in (("LacunaSustain", 62.5, 0.5), ("MeridianSustain", 47.5, 10), ("ChoirSustain", 82.5, 0.75)):
        stereo = []
        phases = rng.uniform(0, np.pi * 2, 72)
        for side in (0, 1):
            t = time
            carrier = np.zeros_like(t)
            for harmonic in range(1, 13):
                f = root * harmonic + (0.75 if harmonic % 3 == 0 else 0)
                fm = (0.17 + harmonic * 0.023) * np.sin(2 * np.pi * .5 * t + side * .2)
                carrier += np.sin(2 * np.pi * f * t + fm + harmonic * .43) / harmonic ** 1.35
            air = np.zeros_like(t)
            for index in range(72):
                f = round((310 + index * 43.25 + index ** 1.23) * 4) / 4
                air += np.sin(2 * np.pi * f * t + phases[index] + side * .31) / 72
            breath = .76 + .12 * np.sin(2 * np.pi * pulse * t) + .10 * np.sin(2 * np.pi * .25 * t)
            if name == "MeridianSustain":
                breath *= .7 + .3 * np.sin(2 * np.pi * pulse * t) ** 4
            if name == "ChoirSustain":
                carrier += .26 * np.sin(2 * np.pi * 371.25 * t + .18 * np.sin(2 * np.pi * .75 * t))
                carrier += .12 * np.sin(2 * np.pi * 495 * t + side * .1)
            stereo.append((carrier * .62 + air * .9) * breath)
        data = np.stack(stereo, axis=1)
        data *= .68 / np.max(np.abs(data))
        pcm = (data * 32767).astype("<i2")
        path = args.output / f"{name}.wav"
        with wave.open(str(path), "wb") as out:
            out.setnchannels(2); out.setsampwidth(2); out.setframerate(rate); out.writeframes(pcm.tobytes())
        print(f"{path.name}: 4 s / stereo / 44.1 kHz, peak={np.max(np.abs(data)):.3f}, seam={np.max(np.abs(data[0]-data[-1])):.5f}")


if __name__ == "__main__":
    main()
