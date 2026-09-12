"""Weapon-sized edits of accepted, project-authored Raid SFX (NumPy only).

No BGM, third-party sample or new oscillator library is used. Source WAVs are
read-only. SCORE owns explicit source windows, rate, EQ, gain and event placement.
Exact inputs and superseded weapon masters are archived outside the runtime tree.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import wave

import numpy as np

RATE = 44100


def layer(source, gain=1., speed=1., start=0., low=40., high=8000., reverse=False,
          window=None, at=0.):
    return dict(source=source, gain=gain, speed=speed, start=start, low=low,
                high=high, reverse=reverse, window=window, at=at)


SCORE = {
    'ClawSwipe': (.20, 'swing', [
        layer('BladeUnsheathe', 1., 1.16, high=6500),
        layer('SwordImpale', .16, 1.3, low=1900, high=7800, at=.012)]),
    'ClawGrip': (.18, 'impact', [
        layer('ShellMassLatch', .82, 1.25, high=4500),
        layer('PylonHit', .50, .82, high=950)]),
    'ClawCrush': (.32, 'impact', [
        layer('HandCrushImpact', .80, 1.0, high=4600),
        layer('ShellMassCollapse', .46, 1.25, high=2100),
        layer('SwordImpale', .48, .91, low=1800, high=7400)]),
    'ClawHit': (.12, 'impact', [
        layer('SwordImpale', .85, 1.35, high=6900),
        layer('PylonHit', .48, .8, high=1200)]),
    'MagicSigil': (.22, 'arrival', [
        layer('ShellMassLatch', .78, 1.3, high=5100),
        layer('CoreHit', .34, .92, low=700, high=7000, at=.022)]),
    'MagicBolt': (.15, 'impact', [
        layer('LanceFire', .72, 1.4, high=6600),
        layer('CoreSalvoFire', .32, 1.2, high=1300),
        layer('CoreHit', .18, 1.4, low=1300, high=7600)]),
    'MagicMerge': (22/60, 'gather', [
        layer('ShellMassShed', .70, 1.0, window=.42, reverse=True, high=6000),
        layer('EnergyGather', .42, 1.1, start=.35, high=2600),
        layer('CoreHit', .22, .8, at=.02)]),
    'MagicCharge': (.8, 'charge', [
        layer('EnergyCharge', .82, 1.0, start=.55, high=5700),
        layer('ShellArc', .25, .76, window=.38, reverse=True, at=.38)]),
    'MagicFire': (.34, 'impact', [
        layer('LanceFire', .68, .94, high=7400),
        layer('CoreSalvoFire', .66, .88, high=3500),
        layer('SpreadExecution', .20, .8, low=1000, high=6600, window=.11)]),
    'RangedLatch': (.14, 'impact', [
        layer('PylonHit', .82, 1.15, high=5500),
        layer('ShellMassLatch', .36, 1.5, high=6700)]),
    'RangedShot': (.13, 'impact', [
        layer('CoreSalvoFire', .72, 1.4, high=4100),
        layer('SwordImpale', .40, 1.4, low=2000, high=8200),
        layer('PylonHit', .44, .71, high=900)]),
    'RangedCharge': (.8, 'charge', [
        layer('EnergyGather', .60, .76, start=.4, high=3100),
        layer('EnergyLock', .33, .84, start=.25, high=6500)]),
    'RangedFire': (.29, 'impact', [
        layer('GridFire', .70, 1.3, high=5200),
        layer('HandCrushImpact', .56, .92, high=1600),
        layer('SwordImpale', .24, 1.2, low=2500)]),
    'ChoirNote': (.23, 'arrival', [
        layer('CoreHit', .76, .72, high=6200),
        layer('SpreadDissolve', .34, 1.2, low=900, high=4900),
        layer('LanceFire', .25, .72, high=900)]),
    'ChoirCharge': (.8, 'charge', [
        layer('EnergyCharge', .54, .84, start=.7, high=3900),
        layer('EnergyLock', .34, 1.1, start=.3, high=5400),
        layer('CoreHit', .20, .70, window=.2, reverse=True, at=.58)]),
    'ChoirFire': (.42, 'impact', [
        layer('GridFire', .62, .81, high=6200),
        layer('ShellMassCollapse', .46, .84, high=3200),
        layer('SpreadExecution', .13, .66, low=1200, window=.17)]),
    'WitnessDraw': (.20, 'swing', [
        layer('BladeUnsheathe', .82, .92, high=5800),
        layer('CoreHit', .20, 1.12, low=1600, at=.012)]),
    'WitnessLock': (22/60, 'charge', [
        layer('EnergyLock', .60, 1.35, start=.3, high=5800),
        layer('SwordImpale', .24, .85, window=.23, reverse=True, at=.08)]),
    'WitnessFire': (.34, 'impact', [
        layer('SwordImpale', .83, .92, high=7900),
        layer('HandCrushImpact', .56, .82, high=1900),
        layer('LanceFire', .22, 1.24, high=6100)]),
    'WeaponHit': (.10, 'impact', [
        layer('PylonHit', .65, 1.42, high=3300),
        layer('CoreHit', .36, 1.22, low=1100, high=7200)]),
    'DollSummon': (.55, 'arrival', [
        layer('ShellMassShed', .54, 1.1, high=5400),
        layer('CoreHit', .56, .81, high=6700),
        layer('SpreadDissolve', .35, .92, low=700, high=5500, at=.055)]),
    'DollThread': (.16, 'impact', [
        layer('LanceFire', .76, 1.42, high=7100),
        layer('CoreHit', .36, 1.12, high=6300)]),
    'DollCharge': (.6, 'charge', [
        layer('EnergyCharge', .82, 1.18, start=.7, high=6200),
        layer('ShellArc', .20, .87, window=.30, reverse=True, at=.27)]),
    'DollVerdict': (.34, 'impact', [
        layer('LanceFire', .70, .98, high=7200),
        layer('CoreSalvoFire', .46, 1.0, high=3600),
        layer('CoreHit', .31, .92, low=1500, high=7100)]),
}

LOOPS = {
    'LacunaSustain': ('LacunaSustain', 'LanceFire', 280., 7600., .19),
    'MeridianSustain': ('MeridianSustain', 'CoreSalvoFire', 180., 6700., .23),
    'ChoirSustain': ('ChoirSustain', 'GridFire', 420., 6100., .16),
}


def read(path):
    with wave.open(str(path), 'rb') as source:
        if source.getsampwidth() != 2:
            raise ValueError(f'Expected PCM16 project input: {path.name}')
        rate = source.getframerate()
        x = np.frombuffer(source.readframes(source.getnframes()), '<i2').astype(float)
        x = x.reshape(-1, source.getnchannels()) / 32768.
    if rate != RATE:
        times = np.arange(round(len(x)*RATE/rate)) * rate/RATE
        x = np.stack([np.interp(times, np.arange(len(x)), side) for side in x.T], axis=1)
    return x


def band(x, low, high):
    freq = np.fft.rfftfreq(len(x), 1/RATE)
    gain = (1 - np.exp(-(freq/low)**4)) * np.exp(-(freq/high)**4)
    return np.fft.irfft(np.fft.rfft(x, axis=0)*gain[:, None], len(x), axis=0)


def rms(x):
    return float(np.sqrt(np.mean(x*x)))


def contour(n, kind):
    t = np.arange(n) / RATE
    a = np.arange(n) / max(1, n-1)
    if kind == 'charge':
        # Audible arrival, brief held pressure, then an accelerating rise.
        shape = .24*np.exp(-a*18) + .14 + .83*a**2.65
    elif kind == 'gather':
        shape = .20 + .85*np.sin(a*np.pi/2)**2
    elif kind == 'swing':
        shape = np.exp(-((a-.20)/.19)**2) + .30*np.exp(-((a-.46)/.20)**2)
    elif kind == 'arrival':
        shape = np.exp(-a*3.7)*(.92+.08*np.cos(a*27))
    else:
        shape = np.exp(-a*4.3)
    attack = np.minimum(1, t/.0025)
    tail = np.minimum(1, (1-a)*n/(RATE*(.016 if kind == 'charge' else .025)))**2
    return shape * attack * tail


def master(x, target, peak=.86):
    x -= np.mean(x, axis=0)
    drive = min(2.8, max(.55, target / max(.001, rms(x))))
    x = np.tanh(x*drive)
    x *= min(1.8, peak / max(.001, float(np.max(np.abs(x)))))
    return x


def render_hit(inputs, seconds, kind, layers):
    n = round(seconds * RATE)
    mixed = np.zeros((n, 1))
    for spec in layers:
        src = np.mean(inputs[spec['source']], axis=1, keepdims=True)
        start = round(spec['start'] * RATE)
        window = spec['window'] or seconds * spec['speed']
        clip = src[start:start + round(window*RATE)]
        if not len(clip):
            raise ValueError(f'Empty source window: {spec}')
        if spec['reverse']:
            clip = clip[::-1]
        clip = band(clip, spec['low'], spec['high'])
        take = min(n-round(spec['at']*RATE), round(len(clip)/spec['speed']))
        if take <= 0:
            raise ValueError(f'Layer outside event: {spec}')
        warped = np.interp(np.arange(take)*spec['speed'], np.arange(len(clip)), clip[:, 0])
        warped *= spec['gain'] * min(4., .28/max(.003, rms(warped)))
        edge = min(176, take//4)
        warped[:edge] *= np.linspace(0, 1, edge)
        warped[-edge:] *= np.linspace(1, 0, edge)
        at = round(spec['at']*RATE)
        mixed[at:at+take, 0] += warped
    mixed *= contour(n, kind)[:, None]
    mixed = master(mixed, .30 if kind in ('impact', 'swing') else .24)
    # DC removal/limiting must not reintroduce nonzero endpoint samples.
    edge = min(110, n//8)
    mixed[:edge] *= np.linspace(0, 1, edge)[:, None]
    mixed[-edge:] *= np.linspace(1, 0, edge)[:, None]
    return mixed[:, 0]


def render_loop(inputs, name):
    base_name, body_name, low, high, detail_gain = LOOPS[name]
    base = inputs[base_name].copy()
    if len(base) != RATE*4 or base.shape[1] != 2:
        raise ValueError(f'Expected accepted four-second stereo bed: {base_name}')
    base = band(base, 48., high)
    # A stable energy window, without the Raid launch accent or distant aftermath.
    clip = np.mean(inputs[body_name][round(.20*RATE):round(1.40*RATE)], axis=1)
    seam = round(.12*RATE)
    fade = np.linspace(0, 1, seam)
    period = np.concatenate([clip[seam:-seam], clip[-seam:]*(1-fade)+clip[:seam]*fade])
    # Periodic Fourier interpolation retains one wrap with a 120ms crossfade.
    spectrum = np.fft.rfft(period)
    detail = np.fft.irfft(spectrum, RATE*4) * (RATE*4/len(period))
    detail = band(detail[:, None], low, high)[:, 0]
    detail /= max(.01, rms(detail))
    t = np.arange(len(base))/RATE
    slow = .85 + .15*np.sin(2*np.pi*.5*t)
    for side in range(2):
        base[:, side] += np.roll(detail, 0 if side == 0 else round(.006*RATE))*detail_gain*slow
    return master(base, .34, .81)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, x):
    if not np.isfinite(x).all() or np.max(np.abs(x)) >= .99:
        raise ValueError(f'Invalid/clipping output {path.name}')
    channels = x[:, None] if x.ndim == 1 else x
    interpolated = np.fft.irfft(np.fft.rfft(channels, axis=0), len(x)*4, axis=0)*4
    oversampled_peak = float(np.max(np.abs(interpolated)))
    if oversampled_peak > .96:
        x = x * (.96/oversampled_peak)
    pcm = np.round(x*32767).astype('<i2')
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(1 if x.ndim == 1 else x.shape[1])
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    decoded = read(path)
    interpolated = np.fft.irfft(np.fft.rfft(decoded, axis=0), len(decoded)*4, axis=0)*4
    return dict(seconds=round(len(x)/RATE, 6),
                peak_dbfs=round(20*np.log10(np.max(np.abs(decoded))), 3),
                peak_4x_dbfs=round(20*np.log10(np.max(np.abs(interpolated))), 3),
                rms_dbfs=round(20*np.log10(rms(decoded)), 3),
                seam=round(float(np.max(np.abs(decoded[0]-decoded[-1]))), 7),
                max_step=round(float(np.max(np.abs(np.diff(decoded, axis=0)))), 7),
                sha256=sha(path))


def archive(path, folder):
    folder.mkdir(parents=True, exist_ok=True)
    target = folder/(path.stem+'-'+sha(path)[:16]+path.suffix)
    if not target.exists():
        shutil.copy2(path, target)


def auditions(samples, folder):
    cues = {
        'Claws': [(0,'ClawSwipe',.64),(.40,'ClawSwipe',.64),(.95,'ClawGrip',.65),
                  (1.15,'ClawSwipe',.68),(1.22,'ClawCrush',.85)],
        'Magic': [(0,'MagicSigil',.44),(1.7,'MagicSigil',.472),(2.9,'MagicSigil',.504),
                  (3.75,'MagicSigil',.536),(4.4,'MagicSigil',.568),(4.9,'MagicSigil',.60),
                  (5.33,'MagicSigil',.632),(340/60,'MagicMerge',.70),
                  (362/60,'MagicCharge',.8),(410/60,'MagicFire',.95),(410/60,'LacunaSustain',.65)],
        'Ranged': [(0,'RangedLatch',.60),(.30,'RangedShot',.60),(.6,'RangedShot',.60),
                   (1.2,'RangedLatch',.60),(2.2,'RangedLatch',.60),(3.1,'RangedLatch',.60),
                   (4.0,'RangedLatch',.60),(5.,'RangedCharge',.84),(5.8,'RangedFire',.95),
                   (5.8,'MeridianSustain',.52)],
        'Summon': [(0,'ChoirNote',.46),(.4,'ChoirNote',.46),(.8,'ChoirNote',.46),
                   (1.4,'MagicMerge',.6),(2.2,'ChoirCharge',.76),(3.,'ChoirFire',.95),
                   (3.,'ChoirSustain',.56)],
        'Rogue': [(i*29/60,'WitnessDraw',.56) for i in range(6)] +
                 [(3.1,'WitnessLock',.86),(3.1+22/60,'WitnessFire',.95)],
        'Doll': [(0,'DollSummon',.65),(1+24/60,'DollThread',.65),
                 (1+44/60,'DollThread',.65),(1+58/60,'DollThread',.65),
                 (2.,'DollCharge',.65),(2.6,'DollVerdict',.85),(2.6,'LacunaSustain',.48)],
    }
    report = {}
    for name, events in cues.items():
        end = max(at+(1.2 if name == 'Doll' and 'Sustain' in cue else len(samples[cue])/RATE)
                  for at,cue,_ in events)+.18
        mix = np.zeros((round(end*RATE), 2))
        for at, cue, gain in events:
            x = samples[cue].copy()
            if x.ndim == 1:
                x = np.repeat(x[:, None], 2, axis=1)
            if 'Sustain' in cue:
                if name == 'Doll':
                    x = x[:round(1.2*RATE)]
                x[:882] *= np.linspace(0,1,882)[:, None]
                x[-882:] *= np.linspace(1,0,882)[:, None]
            start = round(at*RATE)
            mix[start:start+len(x)] += x*gain
        peak = float(np.max(np.abs(mix)))
        attenuation = min(1., .96/max(.001, peak))
        report[name] = save(folder/(name+'.wav'), mix*attenuation)
        report[name]['preview_only_safety_attenuation_db'] = round(20*np.log10(attenuation), 3)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path,
                        default=Path(__file__).resolve().parents[1]/'Assets/Sounds/FirstSeverance')
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--audition', required=True, type=Path)
    args = parser.parse_args()
    if args.source.resolve() == args.output.resolve():
        parser.error('Raid source must never be the weapon destination')
    args.output.mkdir(parents=True, exist_ok=True)
    args.audition.mkdir(parents=True, exist_ok=True)
    names = sorted({part['source'] for _,_,layers in SCORE.values() for part in layers}
                   | {source for value in LOOPS.values() for source in value[:2]})
    inputs = {name: read(args.source/(name+'.wav')) for name in names}
    source_hashes = {name: sha(args.source/(name+'.wav')) for name in names}
    for name in names:
        archive(args.source/(name+'.wav'), args.audition/'raid-inputs')
    samples = {name: render_hit(inputs, *spec) for name,spec in SCORE.items()}
    samples.update({name: render_loop(inputs,name) for name in LOOPS})
    metrics = {}
    for name, x in samples.items():
        target = args.output/(name+'.wav')
        if target.exists():
            archive(target,args.audition/'previous-masters')
        metrics[name] = save(target, x)
        metrics[name]['sources'] = [part['source'] for part in SCORE[name][2]] if name in SCORE else list(LOOPS[name][:2])
    report = dict(recipe='tools/remix_weapon_foley.py', recipe_sha256=sha(Path(__file__)),
                  numpy=np.__version__, source_sha256=source_hashes, assets=metrics,
                  auditions=auditions(samples,args.audition))
    for name in names:
        if sha(args.source/(name+'.wav')) != source_hashes[name]:
            raise RuntimeError(f'Input changed during render: {name}')
    (args.audition/'manifest.json').write_text(json.dumps(report, indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
