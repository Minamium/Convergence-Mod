"""Analyze an explicitly supplied licensed WAV; never downloads or redistributes masters.

Outputs are local unless --export is explicitly selected. NumPy + external FFmpeg.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import numpy as np


def read(path):
    with wave.open(str(path), 'rb') as w:
        if w.getsampwidth() != 2 or w.getnchannels() != 2:
            raise ValueError('Expected stereo PCM16 master')
        return w.getframerate(), np.frombuffer(w.readframes(w.getnframes()), '<i2').reshape(-1, 2).astype(np.float64)/32768


def analyze(sr, stereo):
    # 100 Hz spectral onset envelope; independent of compressor/loudness changes.
    x = stereo.mean(axis=1)[::4]
    rate, hop, size = sr//4, sr//400, 1024
    windows = np.lib.stride_tricks.sliding_window_view(x, size)[::hop]
    spec = np.abs(np.fft.rfft(windows * np.hanning(size), axis=1))
    flux = np.maximum(np.diff(np.log1p(spec*10), axis=0), 0).sum(axis=1)
    flux -= np.convolve(flux, np.ones(151)/151, 'same')
    flux = np.maximum(flux, 0)
    times = (np.arange(len(flux))*hop + size/2)/rate
    results = []
    for bpm in np.arange(75, 201, .05):
        period = 60/bpm
        phase = np.exp(2j*np.pi*times/period)
        z = np.sum(flux*phase)
        results.append((float(abs(z)/flux.sum()), float(bpm), float(np.angle(z)%(2*np.pi)*period/(2*np.pi))))
    selected = []
    for strength, bpm, offset in sorted(results, reverse=True):
        if all(abs(bpm-p['bpm']) > 2 for p in selected):
            selected.append(dict(bpm=round(bpm, 2), phase_seconds=round(offset, 4), strength=round(strength, 4)))
        if len(selected) == 8:
            break
    blocks = []
    for i in range(0, len(stereo), sr*4):
        block=stereo[i:i+sr*4]
        blocks.append([round(i/sr, 3), round(float(20*np.log10(max(1e-9,np.sqrt(np.mean(block*block))))), 2)])
    return dict(duration=len(stereo)/sr, sample_rate=sr, tempo_candidates=selected, four_second_rms_dbfs=blocks,
                method='Numerical onset-periodicity and energy analysis, not subjective listening')


def score_export(sr, x, ffmpeg, out, report):
    # Local maxima of broadband spectral flux, then an adaptive beat tracker.
    mono=x.mean(axis=1)[::4]
    rate=sr//4
    n,hop=1024,rate//100
    spec=np.abs(np.fft.rfft(np.lib.stride_tricks.sliding_window_view(mono,n)[::hop]*np.hanning(n),axis=1))
    flux=np.maximum(np.diff(np.log1p(spec*10),axis=0),0).sum(axis=1)
    times=(np.arange(len(flux))*hop+n/2)/rate
    flux=np.maximum(flux-np.convolve(flux,np.ones(101)/101,'same'),0)
    peaks=[i for i in range(2,len(flux)-2) if flux[i]==max(flux[i-2:i+3]) and flux[i]>np.percentile(flux,65)]
    # Find matching quiet beat boundaries for a musical return, not file EOF/silence.
    freqs=np.fft.rfftfreq(n,1/rate)
    bins=np.rint(69+12*np.log2(np.maximum(freqs,1)/440)).astype(int)%12
    chroma=np.stack([np.sum(spec[:,(bins==k)&(freqs>130)&(freqs<3500)]**2,axis=1) for k in range(12)],axis=1)
    def signature(i):
        c=chroma[max(0,i-38):i].mean(axis=0)
        return c/max(1e-8,np.linalg.norm(c))
    choices=[]
    for s in peaks:
        if not 8<times[s]<24: continue
        for e in peaks:
            if not 127<times[e]<137.5: continue
            level=abs(np.log(max(1e-9,np.mean(spec[e-25:e]**2))/max(1e-9,np.mean(spec[s-25:s]**2))))
            choices.append((float(np.dot(signature(s),signature(e)))-.12*level,s,e))
    quality,s,e=max(choices)
    start,end=round(times[s]*sr),round(times[e]*sr)
    # A continuous 300ms tail-to-pre-loop bridge. No silent gap, tempo/pitch edit,
    # loudness normalization or separate loop sound played once per cycle.
    mixed=x[:end].copy()
    cross=round(.30*sr)
    w=np.linspace(0,1,cross)[:,None]
    w=w*w*(3-2*w)
    mixed[end-cross:end]=mixed[end-cross:end]*(1-w)+x[start-cross:start]*w
    out.mkdir(parents=True,exist_ok=True)
    pcm=(np.clip(mixed,-1,1)*32767).astype('<i2').tobytes()
    subprocess.run([str(ffmpeg),'-v','error','-y','-f','s16le','-ar',str(sr),'-ac','2','-i','-',
                    '-c:a','libvorbis','-q:a','6','-metadata',f'LOOPSTART={start}',
                    '-metadata',f'LOOPEND={end}','-metadata','ARTIST=kuku',
                    '-metadata','TITLE=Graceful Ordeal - Crimson Foundry game loop',
                    str(out/'GracefulOrdeal.ogg')],input=pcm,check=True)
    beat=[]
    cursor=float(times[peaks[0]])
    period=60/126.7
    while cursor<end/sr:
        index=int(np.argmin(abs(times-cursor)))
        lo,hi=max(0,index-9),min(len(flux),index+10)
        peak=lo+int(np.argmax(flux[lo:hi]))
        actual=float(times[peak]) if flux[peak]>np.percentile(flux,60) else cursor
        if not beat or actual-beat[-1]>.27: beat.append(actual)
        if len(beat)>1: period=float(np.clip(.92*period+.08*(beat[-1]-beat[-2]),.42,.52))
        cursor=actual+period
    energies=[]
    for b in beat:
        c=mixed[max(0,int((b-.08)*sr)):min(end,int((b+.17)*sr))]
        db=20*np.log10(max(1e-8,float(np.sqrt(np.mean(c*c)))))
        energies.append(round(float(np.clip((db+23)/13,0,1)),3))
    score=dict(SampleRate=sr,LoopStartSample=start,LoopEndSample=end,
               BeatTicks=[round(b*60) for b in beat],Energy=energies,IntroTicks=480)
    (out/'Score.json').write_text(json.dumps(score,separators=(',',':'))+'\n',encoding='utf-8')
    audition=np.concatenate([mixed[end-sr*5:end],mixed[start:start+sr*8]])
    with wave.open(str(report.parent/'loop-seam-audition.wav'),'wb') as w:
        w.setparams((2,2,sr,0,'NONE','not compressed')); w.writeframes((audition*32767).astype('<i2').tobytes())
    return dict(loop_start_sample=start,loop_end_sample=end,loop_start_seconds=start/sr,
                loop_end_seconds=end/sr,bridge_seconds=.30,boundary_chroma_score=round(quality,3),
                seam_step_peak=float(np.max(abs(mixed[-1]-mixed[start]))),
                beat_count=len(beat),peak_dbfs=float(20*np.log10(np.max(abs(mixed)))),
                listening='Owner audition required; numerical matching is not a listening approval')


def main():
    p=argparse.ArgumentParser()
    p.add_argument('source', type=Path)
    p.add_argument('--report', required=True, type=Path)
    p.add_argument('--export', type=Path)
    p.add_argument('--ffmpeg', type=Path)
    a=p.parse_args()
    sr,x=read(a.source)
    report=analyze(sr,x)
    report['input_sha256']=hashlib.sha256(a.source.read_bytes()).hexdigest()
    a.report.parent.mkdir(parents=True,exist_ok=True)
    if a.export:
        if not a.ffmpeg: p.error('--export requires --ffmpeg')
        report['export']=score_export(sr,x,a.ffmpeg,a.export,a.report)
    a.report.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))


if __name__=='__main__':
    main()
