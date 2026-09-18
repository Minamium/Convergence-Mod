"""Original Scarlet phrase cues; reuse project-authored beam synthesis, not recordings.

No edits to Doll, chorus or music masters. Emits bounded PCM and an unnormalized
dense-phrase audition to expose overlapping peaks. NumPy is the only dependency.
"""
import argparse
import json
from pathlib import Path
import numpy as np
from generate_beam_sfx import RATE, noise, impulse, resonant_fan, ease, write_wav, audit

SPECS = {"Foretell": (.32, 0), "CrownRupture": (.48, 1),
         "SilkCleave": (.43, 2), "ThornRend": (.46, 3), "ScarletRelease": (.44, 4)}


def make(name, duration, species):
    rng = np.random.default_rng(91826 + species)
    t = np.arange(round(RATE * duration)) / RATE
    if species == 0:
        frequency = 470 + 690 * ease(t, 0, .23)
        phase = 2 * np.pi * np.cumsum(frequency) / RATE
        x = (.3*np.sin(phase) + .13*np.sin(phase*1.507)
             + noise(len(t), rng, 450, 3100)*.16)
        x *= ease(t, 0, .018) * np.exp(-t / .11)
    else:
        # Air-cut and bowed, inharmonic scarlet resonator above the pressure
        # transient: not five copies of a blunt collision/metal hammer.
        air = noise(len(t), rng, 580 if species == 2 else 250, 5300)
        x = air * (.5 if species == 2 else .30) * np.exp(-t/.09)
        x += impulse(t, rng, 1.6 if species == 1 else .85)*.65
        x += resonant_fan(t, "wide" if species == 1 else "beam")*.65
        frequency = (1150 + species*125) * (.45 + .55*np.exp(-t/.055))
        phase = 2*np.pi*np.cumsum(frequency)/RATE
        x += (.20*np.sin(phase)+.09*np.sin(phase*2.083))*np.exp(-t/.105)
        x *= ease(t, 0, .002)
    # Quiet, decorrelated reflections, naturally ending inside half a second.
    stereo = np.column_stack((x, x))
    for channel in range(2):
        for delay, gain in ((.051+channel*.008, .17), (.109-channel*.006, .085)):
            n = round(delay*RATE)
            stereo[n:, channel] += x[:-n]*gain
    stereo -= stereo.mean(axis=0)
    if species:
        stereo = np.tanh(stereo * 1.1)
        stereo -= stereo.mean(axis=0)
    stereo *= (1-ease(t, duration-.085, duration-.002))[:, None]
    stereo *= ease(t, 0, .002)[:, None]
    stereo *= .82/max(1e-9, abs(stereo).max())
    return stereo


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--preview', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.preview.mkdir(parents=True, exist_ok=True)
    clips = {name: make(name, *spec) for name, spec in SPECS.items()}
    report = {}
    for name, clip in clips.items():
        write_wav(args.output/(name+'.wav'), clip)
        report[name] = audit(clip)
    bus = np.zeros((RATE*12, 2))
    for source, name in enumerate(list(SPECS)[1:]):
        for i, offset in enumerate((0, 2, 3, 5, 6, 8)):
            fire = .65 + source*2.8 + offset*7/60
            for cue, when, gain in (("Foretell", fire-.70, .25), (name, fire, .68 if i in (0, 5) else .56)):
                start = max(0, round(when*RATE)); clip = clips[cue]*gain
                bus[start:start+len(clip)] += clip
    if abs(bus).max() >= .99:
        raise ValueError(f"Unnormalized rehearsal bus clips: {abs(bus).max()}")
    report['dense_phrase_bus'] = audit(bus)
    write_wav(args.preview/'Scarlet-phrase-audition.wav', bus)
    (args.preview/'audio-audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, separators=(',', ':')))


if __name__ == '__main__':
    main()
