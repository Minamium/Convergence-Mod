"""Original Raid beam pressure/ion-foil cues. NumPy only; no recorded samples.

The reference recordings informed envelope/event grouping, not timbre claims or
sample extraction. --output is explicit: shared weapon/BGM masters are untouched.
PCM16 stereo, 44.1 kHz, bounded peaks and DC, short end releases. BeamSustain is
periodic (integer-frequency partials + periodic FFT noise), not a faded one-shot.
"""
import argparse
import hashlib
import json
import subprocess
import wave
from pathlib import Path

import numpy as np

RATE = 44100
SPECS = {
    'PortalCharge': (.47, 'gather', .62),
    'WideCharge': (.65, 'gather', .72),
    'GridCharge': (1.0, 'grid_charge', .68),
    'ChargeGather': (.70, 'gather', .78),
    'ChargeLock': (.40, 'lock', .60),
    'PortalFire': (1.00, 'beam', .74),
    'CurtainFire': (.69, 'wide', .74),
    'CoreSalvoFire': (.94, 'salvo', .76),
    'GridFire': (.94, 'grid', .72),
    'ChargeRush': (.47, 'rush', .76),
    'WideFire': (.88, 'wide', .74),
    'BeamSustain': (2.40, 'loop', .57),
    'FloodFire': (.64, 'wide', .72),
    'SpreadRay': (.34, 'ray', .70),
    'SpreadScatter': (.46, 'scatter', .60),
}


def noise(n, rng, low, high):
    f = np.fft.rfftfreq(n, 1 / RATE)
    response = (1 - np.exp(-(f / low)**4)) * np.exp(-(f / high)**2)
    x = np.fft.irfft(np.fft.rfft(rng.normal(size=n)) * response, n=n)
    return x / max(1e-9, np.sqrt(np.mean(x*x)))


def ease(t, a, b):
    u = np.clip((t-a)/(b-a), 0, 1)
    return u*u*(3-2*u)


def pressure(t, rng, wide=False):
    """No saw/chiptune carrier: an inharmonic cavity plus moving noise skirts."""
    n = len(t)
    low = noise(n,rng,28,240)
    mid = noise(n,rng,190,1750)
    edge = noise(n,rng,1450,6200)
    cavity = np.zeros(n)
    for i, f in enumerate((62, 89.1, 137.7, 218.2, 339.6, 523.9)):
        phi = 2*np.pi*f*t + .38*np.sin(2*np.pi*(5.2+i*.61)*t)
        cavity += np.sin(phi+rng.uniform(-np.pi,np.pi)) / (i+1)**1.12
    drift = .72 + .18*np.sin(2*np.pi*11.3*t+.7*np.sin(2*np.pi*2.1*t))
    return low*(.39 if wide else .28) + cavity*.19 + mid*.25*drift + edge*.045


def impulse(t, rng, mass=1):
    """Impact under the thin pilot: short rupture then a low pressure bloom."""
    sweep_phase = 2*np.pi*(59*t + 2.8*(1-np.exp(-t/.025)))
    low = (np.sin(sweep_phase)+.24*np.sin(sweep_phase*1.97))*np.exp(-t/.14)
    crack = noise(len(t),rng,650,7600)*np.exp(-t/.024)
    grain = noise(len(t),rng,65,1750)*np.exp(-t/.078)
    return (low*.56*mass + crack*.24 + grain*.32)*ease(t,0,.002)


def synth(name, seconds, kind):
    seed = int.from_bytes(hashlib.sha256(name.encode()).digest()[:8], 'little')
    rng = np.random.default_rng(seed)
    t = np.arange(round(seconds*RATE))/RATE
    bed = pressure(t,rng,kind in ('wide','salvo','loop'))
    if kind in ('gather','grid_charge'):
        rise = ease(t,0,seconds*.70)
        # Last warning darkening has its own audible intake, not a fire sound early.
        dip = 1-.84*ease(t,seconds-.115,seconds-.025)
        x = bed*(.08+.43*rise**1.8)*dip
        foil = noise(len(t),rng,850,4700)
        x += foil*.09*rise**2*dip
        for i in range(9):
            f = 175*(1.211**i)
            x += .04*np.sin(2*np.pi*f*t+.42*np.sin(2*np.pi*17*t))*rise*dip/(1+i*.25)
        if kind == 'grid_charge':
            for p in (.02,.19,.38,.62):
                tt = np.maximum(t-p,0)
                x += noise(len(t),rng,600,2400)*.025*np.exp(-tt/.025)*(t>=p)
    elif kind == 'lock':
        # A short stiff aperture latch, not the victory ending's EnergyLock sample.
        x = impulse(t,rng,.42)*.48
        for i,f in enumerate((387,601,967,1421,2287)):
            x += np.sin(2*np.pi*f*t)*np.exp(-t/(.09+i*.014))*.10/(1+i*.4)
    elif kind == 'loop':
        # Every component repeats exactly at the buffer boundary.
        x = noise(len(t),rng,30,270)*.31 + noise(len(t),rng,200,2100)*.20
        for i,f in enumerate((62,91,139,223,347)):
            cycles = round(f*seconds)
            x += .11/(i+1)*np.sin(2*np.pi*cycles*t/seconds + .2*np.sin(2*np.pi*29*t/seconds))
        x *= .83+.11*np.sin(2*np.pi*11*t/seconds)+.06*np.sin(2*np.pi*23*t/seconds)
    elif kind == 'scatter':
        x = noise(len(t),rng,800,5600)*np.exp(-t/.15)*.25
        for i in range(18):
            at = rng.uniform(.015,.23)
            tt = np.maximum(t-at,0)
            x += np.sin(2*np.pi*rng.uniform(1250,4800)*tt)*np.exp(-tt/.028)*(t>=at)*.025
        x += noise(len(t),rng,90,640)*np.exp(-t/.085)*.1
    elif kind == 'ray':
        x = impulse(t,rng,.55)*.5
        x += np.sin(2*np.pi*2380*t + 1.4*np.exp(-t/.018))*np.exp(-t/.035)*.10
        x += bed*.13*np.exp(-t/.13)
    else:
        wide = kind in ('wide','salvo')
        x = impulse(t,rng,1.18 if wide else .91)
        # Pilot->amplification: body arrives over the same first seven ticks.
        plateau = ease(t,.014,.115)*(1-ease(t,seconds-.15,seconds-.015))
        x += bed*plateau*(.75 if wide else .63)
        if kind in ('grid','salvo'):
            # One bounded ensemble; accents follow the unchanged 0.5s scatter span.
            for i,at in enumerate((.045,.112,.188,.269,.367,.475)):
                tt = np.maximum(t-at,0)
                x += noise(len(t),rng,480,4500)*np.exp(-tt/.027)*(t>=at)*(.10-i*.006)
            if kind == 'salvo': x += bed*.16*plateau
        if kind == 'rush':
            x += noise(len(t),rng,430,4200)*np.sin(np.pi*np.clip(t/.39,0,1))**2*.23
    # Small early reflections, not a second impact/reverb after the attack ends.
    if kind != 'loop':
        reflected = x.copy()
        for delay,gain in ((.021,.11),(.043,-.075),(.071,.047)):
            k=round(delay*RATE)
            reflected[k:] += x[:-k]*gain
        x = reflected*ease(t,0,.002)*(1-ease(t,seconds-.045,seconds))
    x -= x.mean()
    # Mid-dominant stereo: center/sub survive mono downmix, sides carry only foil.
    side = noise(len(t),rng,500,6500)*.018
    if kind != 'loop': side *= ease(t,0,.006)*(1-ease(t,seconds-.06,seconds))
    stereo = np.column_stack((x+side,x-side))
    if kind != 'loop':
        stereo *= (ease(t,0,.001)*(1-ease(t,seconds-.018,seconds)))[:,None]
    peak = SPECS[name][2]
    # Keep crest factor, do not hard-limit or force every cue to maximum RMS.
    stereo *= min(peak/max(abs(stereo).max(),1e-9), .175/max(np.sqrt(np.mean(stereo**2)),1e-9))
    return stereo


def write_wav(path, x):
    with wave.open(str(path),'wb') as out:
        out.setnchannels(2); out.setsampwidth(2); out.setframerate(RATE)
        out.writeframes(np.round(np.clip(x,-1,1)*32767).astype('<i2').tobytes())


def audit(x):
    return {'seconds':round(len(x)/RATE,5),'peak_dbfs':round(float(20*np.log10(max(abs(x).max(),1e-12))),2),
        'rms_dbfs':round(float(20*np.log10(max(np.sqrt(np.mean(x*x)),1e-12))),2),
        'dc':float(abs(x.mean(axis=0)).max()),'boundary_step':float(abs(x[0]-x[-1]).max()),
        'clipped_samples':int((abs(x)>=1).sum())}


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output',type=Path,required=True)
    p.add_argument('--preview',type=Path)
    p.add_argument('--ffmpeg',type=Path)
    args=p.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    assets={name:synth(name,*spec[:2]) for name,spec in SPECS.items()}
    report={}
    for name,x in assets.items():
        path=args.output/(name+'.wav'); write_wav(path,x)
        report[name]=audit(x)
        report[name]['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    if args.preview:
        args.preview.mkdir(parents=True,exist_ok=True)
        # The preview uses the current feedback .8 gain, with exact deadline fade.
        def mix(seconds,events):
            bus=np.zeros((round(seconds*RATE),2))
            for name,at,gain,limit in events:
                start=round(at*RATE); clip=assets[name][:round(limit*RATE)].copy()
                n=min(len(clip),len(bus)-start)
                if n<1: continue
                clip=clip[:n]; remain=limit-np.arange(n)/RATE
                clip*=np.minimum(1,np.maximum(remain,0)/.1)[:,None]
                bus[start:start+n]+=clip*gain*.8
            # Leave playback equivalent; fail on clipping instead of hiding a bad mix.
            if abs(bus).max()>=1: raise ValueError('Preview bus clips')
            return bus
        scenarios={
            '01-eight-cast':(7.0,[(n,.25+i*.7+off,gain,dur) for i in range(8)
                for n,off,gain,dur in [('PortalCharge',0,.98,28/60),('PortalFire',28/60,.96,1.0)]]),
            '02-side-bands-and-charge':(4.1,[('ChargeGather',.2,.98,.7),('ChargeLock',.5,.72,.4),
                ('ChargeRush',.9,.96,28/60),('WideCharge',1.8,.90,.6),('CurtainFire',2.4,.96,41/60)]),
            '03-grid-and-core':(3.0,[('GridCharge',.2,.90,1.),('CoreSalvoFire',1.2,1.,56/60)]),
            '04-rotation-and-field':(6.5,[('WideCharge',.1,.90,.65),('BeamSustain',.75,.95,2.4),
                ('WideFire',.75,1.1,.5),('WideFire',2.0,1.1,.5),('WideCharge',4.,.95,.65),
                ('WideFire',4.65,.95,50/60),('WideFire',4.85,.70,38/60)]),
            '05-spread-ray-and-dissolve':(1.5,[('SpreadRay',.2,.90,.34),('SpreadScatter',.24,.68,.46)]),
        }
        all_parts=[]
        for name,(seconds,events) in scenarios.items():
            x=mix(seconds,events); write_wav(args.preview/(name+'.wav'),x)
            report[name]=audit(x); all_parts.extend([x,np.zeros((RATE//2,2))])
        joined=np.concatenate(all_parts)
        wav=args.preview/'Beam-audition.wav'; write_wav(wav,joined)
        if args.ffmpeg:
            subprocess.run([str(args.ffmpeg),'-v','error','-y','-i',str(wav),'-codec:a','libmp3lame','-b:a','192k',
                str(args.preview/'Beam-audition.mp3')],check=True)
        (args.preview/'audio-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))


if __name__=='__main__': main()
