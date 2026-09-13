"""Weapon articulations from the accepted, original portal-beam masters.

Reuses PCM/EQ/loop/export safeguards from remix_weapon_foley. No Raid/BGM input
is changed; six auditions and hash-addressed prior masters stay outside assets.
"""
from copy import deepcopy
from pathlib import Path
import remix_weapon_foley as f


def configure():
    score = deepcopy(f.SCORE)
    replacements = {
        'LanceFire': 'Beams/PortalFire', 'CoreSalvoFire': 'Beams/CoreSalvoFire',
        'GridFire': 'Beams/WideFire', 'EnergyCharge': 'Beams/ChargeRush',
        'EnergyGather': 'Beams/ChargeGather', 'EnergyLock': 'Beams/ChargeLock',
        'SpreadExecution': 'Beams/SpreadRay', 'SpreadDissolve': 'Beams/SpreadScatter',
        'BladeUnsheathe': 'Beams/PortalFire', 'ShellArc': 'Beams/PortalCharge',
    }
    for name, (duration, kind, layers) in score.items():
        for layer in layers:
            if layer['source'] in replacements:
                layer['source'] = replacements[layer['source']]
                layer['start'] = .035 if kind not in ('charge', 'gather') else 0
                if kind in ('charge', 'gather'):
                    # Stretch pressure intake to its existing animation, not silence
                    # followed by a tiny chirp. Durations/trigger times stay intact.
                    layer['speed'] = min(layer['speed'], .65)
        if kind == 'impact' and name not in ('RangedShot', 'WeaponHit', 'ClawHit'):
            score[name] = (duration + .065, kind, layers)
    score['ClawSwipe'] = (.245, 'swing', [
        f.layer('Beams/PortalFire', 1.0, .92, start=.035, high=4200),
        f.layer('Beams/WideFire', .58, .72, high=1500),
        f.layer('PylonHit', .15, 1.35, low=1800, high=6200, at=.025)])
    score['ClawCrush'] = (.40, 'impact', [
        f.layer('Beams/WideFire', 1., .64, high=3600),
        f.layer('HandCrushImpact', .38, .8, high=850),
        f.layer('CoreHit', .20, .82, low=1100, high=5500)])
    score['ClawGrip'] = (.19, 'impact', [f.layer('PylonHit', .8, .70, high=2300),
                                             f.layer('Beams/ChargeLock', .5, .84, high=4300)])
    score['ClawHit'] = (.15, 'impact', [f.layer('Beams/CoreSalvoFire', .85, 1.12, high=5400),
                                            f.layer('PylonHit', .55, .75, high=1400)])
    score['WeaponHit'] = (.13, 'impact', [f.layer('Beams/CoreSalvoFire', .68, 1.42, high=4400),
                                              f.layer('CoreHit', .32, 1.10, low=700, high=6000)])
    f.SCORE = score
    f.LOOPS = {name: (base, replacements.get(body, body), low, high, gain)
               for name, (base, body, low, high, gain) in f.LOOPS.items()}
    old_contour = f.contour

    def contour(n, kind):
        envelope = old_contour(n, kind)
        t = f.np.arange(n) / f.RATE
        if kind in ('impact', 'arrival'):
            # A short body and attached airy reflection, not a second note.
            envelope = f.np.maximum(envelope, .16 * f.np.exp(-t * 10))
            envelope *= f.np.minimum(1, t / .0025) * f.np.minimum(1, (n / f.RATE - t) / .025)
        elif kind == 'charge':
            envelope *= 1 - .50 * f.np.clip((t - n/f.RATE + .07) / .07, 0, 1)
        return envelope
    f.contour = contour
    # The generic runner records this recipe's hash, not an unmodified old recipe.
    f.__file__ = str(Path(__file__).resolve())


if __name__ == '__main__':
    configure()
    f.main()
