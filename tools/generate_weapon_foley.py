"""Historical 0.2.53 weapon-only synthesis (not the current runtime recipe).

Use remix_weapon_foley.py for the accepted-Raid-derived 0.2.57 weapon set.
This predecessor is retained for reproducibility of archived original masters.
No sampled/extracted Terraria or Calamity audio.

Transient / material resonance / air are separate layers. Short shots fit their
cadence; charge is explicitly timed; four-second beds use periodic synthesis.
Requires numpy. Writes PCM assets plus an optional external audition sequence.
"""
import argparse
import json
from pathlib import Path
import wave
import numpy as np

RATE = 44100
TAU = 2 * np.pi
RNG = np.random.default_rng(53260912)


def noise(n, low, high):
    freq = np.fft.rfftfreq(n, 1 / RATE)
    spectrum = np.fft.rfft(RNG.normal(size=n))
    filt = (1 - np.exp(-(freq / low) ** 4)) * np.exp(-(freq / high) ** 4)
    sound = np.fft.irfft(spectrum * filt, n)
    return sound / max(.0001, np.std(sound))


def tone(t, root, decay, metal=False):
    ratios = (1, 1.481, 2.087, 3.719, 5.137) if metal else (1, 2, 3, 4, 6)
    return sum(np.sin(TAU * root * ratio * t + .8 * np.exp(-70*t))
               * np.exp(-t * decay * (1 + i * .19)) / (1 + i) ** 1.12
               for i, ratio in enumerate(ratios))


def master(x, loop=False):
    x -= np.mean(x, axis=0)
    # Soft saturation adds audible midrange to the low impact without clipping.
    x = np.tanh(x * 1.3)
    x *= .87 / max(.001, np.max(np.abs(x)))
    if not loop:
        edge = min(int(RATE * .004), len(x) // 8)
        x[:edge] *= np.linspace(0, 1, edge)
        tail = min(int(RATE * .024), len(x) // 4)
        x[-tail:] *= np.linspace(1, 0, tail) ** 2
    return x


def impact(duration, root, hardness, kind):
    t = np.arange(round(duration * RATE)) / RATE
    envelope = (1-np.exp(-t*900)) * np.exp(-t * (12 if kind == 'heavy' else 27))
    body = tone(t, root, 14 if kind == 'heavy' else 25, metal=kind in ('metal', 'slice'))
    thump = np.sin(TAU * (root*.42*t + 2.8*(1-np.exp(-t*70)))) * np.exp(-t*32)
    crack = noise(len(t), 850, 7200) * np.exp(-t*180)
    air = noise(len(t), 250, 3200) * envelope
    if kind == 'slice':
        envelope = np.exp(-((t-.038)/.025)**2) + .33*np.exp(-((t-.076)/.02)**2)
        air = noise(len(t), 650, 7200) * envelope
        body *= .2
    return master(body*.55 + thump*.75 + crack*hardness + air*.28)


def charge(duration, root, kind):
    t = np.arange(round(duration * RATE)) / RATE
    a = t/duration
    lift = np.sin(np.pi*a/2)**3
    # A rushed onset, tiny held tension, accelerating compression into release.
    phase = TAU * (root*t + root*.20 * t**3/duration**2)
    throat = np.sin(phase + 1.4*np.sin(phase*.503))
    scrape = noise(len(t), 340, 2400) * (.3 + .25*np.sin(TAU*(7*t+9*t*t))**2)
    clack = tone(t, root*2.7, 34, metal=True)*np.exp(-a*18)
    return master((throat*.35 + scrape*.24)*lift + clack*.48)


def bed(root, rate, kind):
    t = np.arange(RATE * 4) / RATE
    stereo = []
    # Every frequency/modulator is an integer multiple of 0.25 Hz.
    for side in (0, 1):
        f = root
        phase = TAU*f*t + .8*np.sin(TAU*.5*t + side*.12)
        body = sum(np.sin(phase*h + .18*h*np.sin(TAU*.75*t)) / h**1.25 for h in range(1, 14))
        texture = sum(np.sin(TAU*(335+i*47.25)*t + side*.2+i*.81) for i in range(40))/24
        if kind == 'choir':
            body += .28*np.sin(TAU*root*1.5*t) + .17*np.sin(TAU*root*2.5*t)
        pulse = .78 + .15*np.cos(TAU*rate*t) + .07*np.sin(TAU*.25*t)
        stereo.append(np.tanh((body*.55+texture*.32)*pulse))
    x = np.stack(stereo, axis=1)
    return x * (.79 / np.max(np.abs(x)))


def save(path, x):
    pcm = (np.clip(x, -.99, .99)*32767).astype('<i2')
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(1 if x.ndim == 1 else 2)
        out.setsampwidth(2); out.setframerate(RATE); out.writeframes(pcm.tobytes())
    return {'seconds': round(len(x)/RATE, 4), 'peak_dbfs': round(20*np.log10(np.max(np.abs(x))), 2),
            'rms_dbfs': round(20*np.log10(np.sqrt(np.mean(x*x))), 2),
            'seam': round(float(np.max(np.abs(x[0]-x[-1]))), 6)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--audition', required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True); args.audition.mkdir(parents=True, exist_ok=True)
    samples = {}
    specs = (
        ('ClawSwipe',.14,260,.25,'slice'), ('ClawGrip',.20,135,.35,'metal'),
        ('ClawCrush',.30,110,.85,'heavy'), ('ClawHit',.11,310,.6,'metal'),
        ('MagicSigil',.14,430,.3,'metal'), ('MagicBolt',.12,580,.42,'energy'),
        ('MagicMerge',.34,180,.3,'heavy'), ('MagicFire',.42,118,.8,'heavy'),
        ('RangedLatch',.11,275,.65,'metal'), ('RangedShot',.105,160,1.0,'gun'),
        ('RangedFire',.34,85,1.1,'heavy'), ('ChoirNote',.19,330,.38,'energy'),
        ('ChoirFire',.40,148,.7,'heavy'), ('WitnessDraw',.16,210,.36,'slice'),
        ('WitnessFire',.38,96,.95,'heavy'), ('WeaponHit',.10,390,.6,'metal'),
        ('DollSummon',.50,246,.3,'heavy'), ('DollThread',.16,470,.38,'slice'),
        ('DollVerdict',.32,170,.7,'heavy'),
    )
    for name, length, root, hardness, kind in specs:
        samples[name] = impact(length, root, hardness, kind)
    for name, seconds, root in (('MagicCharge',.8,160),('RangedCharge',.8,80),
                                ('ChoirCharge',.8,196),('WitnessLock',.36,110),('DollCharge',.6,246)):
        samples[name] = charge(seconds, root, name)
    for name, root, pulse, kind in (('LacunaSustain',62.5,1,'magic'),
                                  ('MeridianSustain',47.5,20,'ranged'),('ChoirSustain',82.5,.75,'choir')):
        samples[name] = bed(root,pulse,kind)
    report = {name:save(args.output/(name+'.wav'),x) for name,x in samples.items()}
    # Same event gains/cadence used in game, no loudness-normalized showcase boost.
    sequences = {
        'Claws': [(0,'ClawSwipe',.64),(.5,'ClawSwipe',.64),(1.2,'ClawGrip',.65),(1.45,'ClawCrush',.85)],
        'Magic': [(0,'MagicSigil',.4),(.45,'MagicBolt',.4),(.75,'MagicMerge',.65),(1.1,'MagicCharge',.72),(1.9,'MagicFire',.85),(1.9,'LacunaSustain',.65)],
        'Ranged': [(0,'RangedLatch',.55),(.2,'RangedShot',.55),(.4,'RangedShot',.55),(.7,'RangedCharge',.7),(1.5,'RangedFire',.85),(1.5,'MeridianSustain',.52)],
        'Summon': [(0,'ChoirNote',.4),(.4,'ChoirNote',.4),(.8,'ChoirCharge',.7),(1.6,'ChoirFire',.85),(1.6,'ChoirSustain',.56)],
        'Rogue': [(0,'WitnessDraw',.5),(.3,'WitnessDraw',.5),(.6,'WitnessLock',.72),(.96,'WitnessFire',.85)],
        'Doll': [(0,'DollSummon',.65),(.65,'DollThread',.65),(.85,'DollThread',.65),(1.05,'DollThread',.65),(1.3,'DollCharge',.65),(1.9,'DollVerdict',.85)],
    }
    for name,events in sequences.items():
        end=max(at+len(samples[cue])/RATE for at,cue,_ in events)+.2
        mix=np.zeros((round(end*RATE),2))
        for at,cue,gain in events:
            x=samples[cue]; x=x[:,None] if x.ndim==1 else x
            if 'Sustain' in cue:
                x=x.copy(); x[-4410:]*=np.linspace(1,0,4410)[:,None]
            start=round(at*RATE);mix[start:start+len(x)]+=x*gain
        peak=float(np.max(np.abs(mix)))
        if peak>=.99: mix*=.98/peak
        save(args.audition/(name+'.wav'),mix)
    (args.audition/'levels.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
