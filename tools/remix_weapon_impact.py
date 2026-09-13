"""Short physical weapon articulations; no reference-recording samples.

Only SCORE destinations change. All source/master backups and six class
auditions stay in the explicit ignored audition directory. Other weapon beds,
Raid SFX and music are read-only. Based on the measured 2026-09-14 recording:
low/mid body and moving cutting air, not a flat laser tone under every swing.
"""
import argparse
import json
from pathlib import Path
import remix_weapon_foley as f

L = f.layer
SCORE = {
    'ClawSwipe': (.29, 'swing', [
        L('BladeUnsheathe', .9, .78, high=2700),
        L('Beams/WideFire', .8, .64, low=42, high=480),
        L('SwordImpale', .30, 1.21, low=1500, high=5200, at=.024)]),
    'ClawGrip': (.18, 'impact', [L('PylonHit', 1., .60, high=1800),
        L('ShellMassLatch', .18, 1.6, low=600, high=3200)]),
    'ClawCrush': (.38, 'impact', [L('HandCrushImpact', 1., .70, high=1200),
        L('PylonHit', .7, .78, high=2100), L('SwordImpale', .22, 1.5, low=2200, high=5500)]),
    'ClawHit': (.16, 'impact', [L('PylonHit', 1., .74, high=1900),
        L('CoreHit', .25, 1.1, low=1000, high=4200)]),
    'RangedLatch': (.17, 'impact', [L('PylonHit', 1., .82, high=2200),
        L('ShellMassLatch', .25, 1.6, low=1100, high=3700)]),
    'RangedShot': (.16, 'impact', [L('HandCrushImpact', .80, 1.28, high=620),
        L('PylonHit', .9, 1.10, low=180, high=2300),
        L('SwordImpale', .18, 1.65, low=2400, high=5600)]),
    'RangedCharge': (.8, 'charge', [L('Beams/ChargeGather', .8, .54, high=1700),
        L('Beams/ChargeRush', .50, .68, low=350, high=3600),
        L('PylonHit', .18, .78, reverse=True, window=.22, at=.50)]),
    'RangedFire': (.35, 'impact', [L('HandCrushImpact', .9, .82, high=1250),
        L('Beams/WideFire', .6, .88, high=2000),
        L('PylonHit', .40, 1.25, low=1800, high=5200)]),
    'WitnessDraw': (.26, 'swing', [L('BladeUnsheathe', 1., .73, high=2200),
        L('Beams/WideFire', .50, .82, high=500),
        L('CoreHit', .16, .94, low=1300, high=4100)]),
    'WitnessLock': (22/60, 'charge', [L('Beams/ChargeLock', .68, .7, high=3100),
        L('PylonHit', .32, .86, reverse=True, window=.27, at=.07)]),
    'WitnessFire': (.36, 'impact', [L('BladeUnsheathe', .85, .85, high=3700),
        L('HandCrushImpact', .76, .72, high=930),
        L('PylonHit', .24, .98, low=1400, high=4500)]),
    'WeaponHit': (.135, 'impact', [L('PylonHit', .85, 1.13, high=2300),
        L('CoreHit', .22, 1.24, low=1100, high=4200)]),
}


def contour(n, kind):
    t = f.np.arange(n) / f.RATE
    a = f.np.arange(n) / max(1, n-1)
    if kind == 'swing':
        # Breath → one fast cut → attached air, with no delayed second note.
        shape = f.np.exp(-((a-.15)/.11)**2) + .38*f.np.exp(-((a-.30)/.23)**2)
    elif kind == 'charge':
        shape = .14 + .20*f.np.exp(-a*18) + .82*a**2.8
        shape *= 1 - .42*f.np.clip((a-.68)/.12, 0, 1) * (1-f.np.clip((a-.80)/.13, 0, 1))
    else:
        shape = f.np.exp(-a*5.6) + .17*f.np.exp(-a*3)
    return shape * f.np.minimum(1, t/.0025) * f.np.minimum(1, (1-a)*n/(f.RATE*.030))**2


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=Path(__file__).resolve().parents[1]/'Assets/Sounds/FirstSeverance')
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--audition', required=True, type=Path)
    args = parser.parse_args()
    if args.source.resolve() == args.output.resolve():
        parser.error('Raid source cannot be the weapon destination')
    args.output.mkdir(parents=True, exist_ok=True)
    args.audition.mkdir(parents=True, exist_ok=True)
    names = sorted({p['source'] for _,_,layers in SCORE.values() for p in layers})
    hashes = {name: f.sha(args.source/(name+'.wav')) for name in names}
    inputs = {name: f.read(args.source/(name+'.wav')) for name in names}
    for name in names:
        f.archive(args.source/(name+'.wav'), args.audition/'raid-inputs')
    f.contour = contour
    metrics = {}
    for name, spec in SCORE.items():
        target = args.output/(name+'.wav')
        if target.exists():
            f.archive(target, args.audition/'previous-masters')
        x = f.render_hit(inputs, *spec)
        metrics[name] = f.save(target, x)
        metrics[name]['sources'] = [p['source'] for p in spec[2]]
        freq = f.np.fft.rfftfreq(len(x), 1/f.RATE)
        power = abs(f.np.fft.rfft(x))**2
        metrics[name]['below_700hz_power_fraction'] = round(float(power[freq<700].sum()/power.sum()), 5)
    # The untouched classes are included as actual runtime masters, not regenerated.
    samples = {p.stem: f.read(p) for p in args.output.glob('*.wav')}
    previews = f.auditions(samples, args.audition, {
        'Claws': [(0,'ClawSwipe',.80),(.4,'ClawSwipe',.80),(.95,'ClawGrip',.65),
                  (1.15,'ClawSwipe',.68),(1.22,'ClawCrush',.85)],
        'Ranged': [(i/60,'RangedLatch',.60) for i in (2,92,164,215,248)] +
                  [(.30,'RangedShot',.60),(.60,'RangedShot',.60),(5.,'RangedCharge',.84),
                   (5.8,'RangedFire',.95),(5.8,'MeridianSustain',.52)],
    })
    if any(f.sha(args.source/(name+'.wav')) != digest for name,digest in hashes.items()):
        raise RuntimeError('A read-only Raid input changed')
    report = dict(date='2026-09-14', recipe='tools/remix_weapon_impact.py',
        recipe_sha256=f.sha(Path(__file__)), helper_sha256=f.sha(Path(f.__file__)),
        numpy=f.np.__version__, source_sha256=hashes, assets=metrics, auditions=previews,
        listening='not_run; numerical analysis only; user audition required')
    (args.audition/'manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
