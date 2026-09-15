"""Sharp, energy-bearing weapon articulations from original Convergence sources.

No third-party audio or BGM is read. Keep long-beam releases and all sustain
masters byte-identical. Archive every replaced master and input before export.
Numerical checks and class auditions are not a claim of human listening.
"""
import argparse
import json
from pathlib import Path

import remix_weapon_foley as f

np = f.np
L = f.layer
PROTECTED = ('MagicFire', 'RangedFire', 'ChoirFire', 'DollVerdict',
             'LacunaSustain', 'MeridianSustain', 'ChoirSustain')
# seconds, envelope, original layers, rising/falling resonant edge (Hz), edge gain
SCORE = {
    'ClawSwipe': (.25, 'swing', [L('BladeUnsheathe', 1, 1.25, low=500, high=9000),
        L('Beams/PortalFire', .6, 1.7, low=1300, high=7500)], 760, .12),
    'ClawGrip': (.16, 'impact', [L('Beams/ChargeLock', .7, 1.65, low=700, high=7000),
        L('SwordImpale', .6, 1.8, low=1600, high=8000)], 480, .16),
    'ClawCrush': (.31, 'impact', [L('Beams/PortalFire', 1, .92, low=170, high=7800),
        L('SwordImpale', .8, 1.3, low=800, high=9500)], 640, .22),
    'ClawHit': (.13, 'impact', [L('SwordImpale', 1, 1.6, low=700, high=9200),
        L('Beams/SpreadRay', .35, 1.9, low=1400, high=7000)], 970, .14),
    'MagicSigil': (.26, 'arrival', [L('Beams/ChargeLock', .65, 1.2, low=350, high=7000),
        L('Beams/SpreadScatter', .4, 1.45, low=1400, high=8500)], 780, .26),
    'MagicBolt': (.22, 'impact', [L('Beams/PortalFire', 1, 1.6, low=380, high=8800),
        L('Beams/SpreadRay', .4, 1.6, low=1000, high=6500)], 1100, .28),
    'MagicMerge': (22/60, 'gather', [L('Beams/PortalFire', .6, .9, reverse=True, low=300, high=7500),
        L('Beams/ChargeRush', 1, 1.25, low=420, high=8000)], 640, .22),
    'MagicCharge': (.8, 'charge', [L('Beams/PortalCharge', 1, 1, low=350, high=7000),
        L('Beams/ChargeRush', .6, .8, low=700, high=8200, at=.32)], 520, .30),
    'RangedLatch': (.15, 'impact', [L('Beams/ChargeLock', .7, 1.7, low=650, high=8600),
        L('SwordImpale', .45, 1.75, low=1800, high=10000)], 440, .12),
    'RangedShot': (.20, 'impact', [L('Beams/PortalFire', 1.1, 1.9, low=140, high=9800),
        L('Beams/CoreSalvoFire', .45, 1.5, low=350, high=6500)], 860, .20),
    'RangedCharge': (.8, 'charge', [L('Beams/ChargeGather', .6, .8, low=250, high=4800),
        L('Beams/ChargeRush', 1, .8, low=750, high=9000, at=.12)], 430, .24),
    'ChoirNote': (.30, 'arrival', [L('Beams/SpreadScatter', .6, 1.25, low=900, high=7000),
        L('Beams/PortalFire', .45, 1.3, low=500, high=7000)], 610, .34),
    'ChoirCharge': (.8, 'charge', [L('Beams/PortalCharge', .7, .8, low=350, high=6000),
        L('Beams/ChargeRush', .8, .85, low=850, high=8500, at=.25)], 470, .28),
    'WitnessDraw': (.24, 'swing', [L('BladeUnsheathe', 1, 1.5, low=650, high=9200),
        L('Beams/PortalFire', .5, 1.8, low=1400, high=7600)], 950, .20),
    'WitnessLock': (22/60, 'charge', [L('Beams/ChargeLock', .75, 1.2, low=380, high=8000),
        L('Beams/ChargeRush', .6, 1.4, low=1200, high=9300)], 720, .24),
    'WitnessFire': (.32, 'impact', [L('Beams/PortalFire', .9, 1.18, low=240, high=9200),
        L('SwordImpale', .8, 1.4, low=800, high=9000)], 820, .24),
    'WeaponHit': (.13, 'impact', [L('Beams/SpreadRay', .5, 1.8, low=1000, high=7200),
        L('SwordImpale', .9, 1.9, low=1800, high=9300)], 1300, .14),
    'DollSummon': (.55, 'arrival', [L('Beams/SpreadScatter', .7, .82, low=550, high=7600),
        L('Beams/ChargeLock', .7, .75, low=260, high=6400)], 620, .22),
    'DollThread': (.23, 'impact', [L('Beams/PortalFire', .9, 1.5, low=350, high=8400),
        L('Beams/SpreadRay', .3, 1.6, low=1300, high=7500)], 930, .27),
    'DollCharge': (.6, 'charge', [L('Beams/PortalCharge', .9, 1.2, low=300, high=7500),
        L('Beams/ChargeRush', .6, 1, low=950, high=8500, at=.18)], 580, .28),
}


def articulate(inputs, spec, seed):
    seconds, kind, layers, frequency, gain = spec
    x = f.render_hit(inputs, seconds, kind, layers)
    t = np.arange(len(x)) / f.RATE
    a = np.arange(len(x)) / (len(x)-1)
    charging = kind in ('gather', 'charge')
    # An attached resonant cut/arc, not a delayed impact or musical second note.
    pitch = frequency * ((.7 + 1.7*a**2.4) if charging else (.55 + 1.6*np.exp(-t*22)))
    phase = np.cumsum(pitch) * (2*np.pi/f.RATE)
    tone = np.sin(phase + 1.35*np.sin(phase*1.91)) + .27*np.sin(phase*2.74)
    noise = np.random.default_rng(seed).normal(0, 1, len(x))
    air = f.band(noise[:, None], 1500, 8500)[:, 0]
    envelope = f.contour(len(x), kind)
    x += (gain*tone + .13*air) * envelope
    if not charging and not spec is SCORE['ClawSwipe']:
        # Soft attached reflections, always inside the bounded event; no late boom.
        source = x.copy()
        for delay, amount in ((.017, .11), (.033, .055)):
            offset = round(delay*f.RATE)
            x[offset:] += source[:-offset]*amount
    x = f.master(x[:, None], .28 if charging else .32, .84)[:, 0]
    edge = np.minimum(1, t/.003) * np.minimum(1, (seconds-t)/.035)**2
    x *= edge
    x[0] = x[-1] = 0
    return x


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=Path(__file__).resolve().parents[1]/'Assets/Sounds/FirstSeverance')
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--audition', required=True, type=Path)
    args = parser.parse_args()
    if args.source.resolve() == args.output.resolve():
        parser.error('source and destination must differ')
    if set(PROTECTED) & SCORE.keys():
        raise ValueError('approved long-beam releases must remain unchanged')
    protected = {n: f.sha(args.output/(n+'.wav')) for n in PROTECTED}
    names = sorted({p['source'] for _,_,ls,_,_ in SCORE.values() for p in ls})
    inputs = {n: f.read(args.source/(n+'.wav')) for n in names}
    hashes = {n: f.sha(args.source/(n+'.wav')) for n in names}
    for n in names:
        f.archive(args.source/(n+'.wav'), args.audition/'raid-inputs')
    metrics = {}
    for i, (name, spec) in enumerate(SCORE.items()):
        path = args.output/(name+'.wav')
        f.archive(path, args.audition/'previous-masters')
        x = articulate(inputs, spec, 91500+i)
        metrics[name] = f.save(path, x)
        freq = np.fft.rfftfreq(len(x), 1/f.RATE)
        power = abs(np.fft.rfft(x))**2
        metrics[name]['presence_700_6000_fraction'] = round(float(power[(freq>700)&(freq<6000)].sum()/power.sum()), 4)
        metrics[name]['peak_time_ms'] = round(float(np.argmax(abs(x)) / f.RATE * 1000), 2)
        metrics[name]['layers'] = spec[2]
    for n, digest in protected.items():
        assert f.sha(args.output/(n+'.wav')) == digest, n
    for n, digest in hashes.items():
        assert f.sha(args.source/(n+'.wav')) == digest, n
    samples = {p.stem: f.read(p) for p in args.output.glob('*.wav')}
    previews = f.auditions(samples, args.audition, {
        'Claws': [(0,'ClawSwipe',.8),(.4,'ClawSwipe',.8),(.95,'ClawGrip',.65),(1.15,'ClawSwipe',.68),(1.22,'ClawCrush',.85)]})
    report = dict(date='2026-09-15', recipe='tools/remix_weapon_articulation.py', recipe_sha256=f.sha(Path(__file__)),
        helper_sha256=f.sha(Path(f.__file__)), numpy=np.__version__, source_sha256=hashes,
        protected_sha256=protected, assets=metrics, auditions=previews,
        listening='not_run; numerical checks only; user audition required')
    (args.audition/'manifest.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(exports=len(metrics), protected=len(protected), auditions=list(previews),
        peak_dbfs=max(m['peak_4x_dbfs'] for m in metrics.values()),
        rms_dbfs_range=[min(m['rms_dbfs'] for m in metrics.values()),max(m['rms_dbfs'] for m in metrics.values())])))


if __name__ == '__main__':
    main()
