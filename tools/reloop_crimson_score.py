"""Re-edit the approved game asset, preserving the introductory music and measured beats.

No download or source master replacement. Reports are numerical, not listening approval.
Pass --export to write the game derivative; analysis is otherwise read-only.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave
import numpy as np


def decode(path, ffmpeg):
    raw = subprocess.check_output([str(ffmpeg), '-v', 'error', '-i', str(path),
                                   '-f', 'f32le', '-ar', '48000', '-ac', '2', '-'])
    return np.frombuffer(raw, '<f4').reshape(-1, 2).astype(np.float64)


def candidates(x, score):
    beats = np.array(score['BeatTicks']) / 60
    # Compare one complete bar before each join, not only a300ms chroma snapshot.
    def features(t, duration=1.85):
        a = x[int((t-duration)*48000):int(t*48000)].mean(axis=1)[::8]
        z = np.abs(np.fft.rfft(np.lib.stride_tricks.sliding_window_view(a, 512)[::128] * np.hanning(512))) ** 2
        freq = np.fft.rfftfreq(512, 1/6000)
        bins = np.rint(69+12*np.log2(np.maximum(freq, 1)/440)).astype(int) % 12
        chroma = np.array([z[:, (bins == k) & (freq > 100)].sum() for k in range(12)])
        chroma /= max(1e-12, np.linalg.norm(chroma))
        rms = float(np.sqrt(np.mean(a*a)))
        return chroma, rms
    cache = {i: features(t) for i, t in enumerate(beats) if 24 < t < 105}
    result = []
    for end in cache:
        if not 92 < beats[end] < 104: continue # Before the quiet outro, not the file end.
        for start in cache:
            if not 24 < beats[start] < 57 or (end-start) % 32: continue
            a, ar = cache[start]; b, br = cache[end]
            level = abs(20*np.log10(ar/br))
            correlation = float(a@b)
            result.append(dict(start_beat=start, end_beat=end, start=float(beats[start]), end=float(beats[end]),
                               chroma=correlation, level_delta_db=level, merit=correlation-.035*level))
    return sorted(result, key=lambda c: c['merit'], reverse=True)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--audio', type=Path, required=True)
    p.add_argument('--score', type=Path, required=True)
    p.add_argument('--ffmpeg', type=Path, required=True)
    p.add_argument('--export', type=Path)
    p.add_argument('--report', type=Path)
    a = p.parse_args()
    x = decode(a.audio, a.ffmpeg)
    score = json.loads(a.score.read_text(encoding='utf-8'))
    options = candidates(x, score)
    report = dict(input_sha256=hashlib.sha256(a.audio.read_bytes()).hexdigest(), candidates=options[:6])
    if a.export:
        chosen = options[0]
        start, end = round(chosen['start']*48000), round(chosen['end']*48000)
        # Four measured beats, phase-aligned by a whole eight-bar interval.
        si, ei = chosen['start_beat'], chosen['end_beat']
        cross = round((score['BeatTicks'][ei]-score['BeatTicks'][ei-4])*800)
        # Do not time-stretch either performance. Align the pre-start bar at its end.
        bridge = x[start-cross:start]
        mixed = x[:end].copy()
        t = np.linspace(0, 1, cross)[:, None]
        t = t*t*(3-2*t)
        # Correlation-normalized equal-power blend prevents a mid-crossfade dip.
        outgoing = mixed[end-cross:end].copy()
        corr = float(np.sum(outgoing*bridge)/max(1e-12, np.sqrt(np.sum(outgoing*outgoing)*np.sum(bridge*bridge))))
        divisor = np.sqrt((1-t)**2+t*t+2*max(0, corr)*t*(1-t))
        mixed[end-cross:end] = (outgoing*(1-t)+bridge*t)/divisor
        peak = float(np.max(np.abs(mixed)))
        # The existing Vorbis decode already exceeds full scale at some peaks.
        # Leave modest headroom for this encode rather than hard-clipping PCM.
        headroom = min(1, .92/peak)
        mixed *= headroom
        a.export.mkdir(parents=True, exist_ok=True)
        out = a.export/'GracefulOrdeal.ogg'
        subprocess.run([str(a.ffmpeg), '-v', 'error', '-y', '-f', 'f32le', '-ar', '48000', '-ac', '2', '-i', '-',
                        '-c:a', 'libvorbis', '-q:a', '7', '-metadata', 'ARTIST=kuku',
                        '-metadata', 'TITLE=Graceful Ordeal - Convergence game loop',
                        '-metadata', f'LOOPSTART={start}', '-metadata', f'LOOPEND={end}', str(out)],
                       input=mixed.astype('<f4').tobytes(), check=True)
        score['LoopStartSample'], score['LoopEndSample'] = start, end
        score['BeatTicks'], score['Energy'] = score['BeatTicks'][:ei], score['Energy'][:ei]
        (a.export/'Score.json').write_text(json.dumps(score, separators=(',', ':'))+'\n', encoding='utf-8', newline='\n')
        decoded = decode(out, a.ffmpeg)
        if np.max(np.abs(decoded)) >= 1: raise ValueError('Decoded export clips')
        audition = np.concatenate((decoded[end-48000*6:end], decoded[start:start+48000*8]))
        if a.report:
            a.report.parent.mkdir(parents=True, exist_ok=True)
            with wave.open(str(a.report.parent/'loop-seam-preview.wav'), 'wb') as w:
                w.setparams((2, 2, 48000, 0, 'NONE', 'not compressed'))
                w.writeframes((np.clip(audition, -1, 1)*32767).astype('<i2').tobytes())
        report.update(selected=chosen, crossfade_seconds=cross/48000, beat_span=ei-si,
                      headroom_gain_db=float(20*np.log10(headroom)),
                      output_sha256=hashlib.sha256(out.read_bytes()).hexdigest(),
                      decoded_peak=float(np.max(np.abs(decoded))),
                      seam_step=float(np.max(np.abs(decoded[end-1]-decoded[start]))),
                      listening='Owner audition required; numerical continuity is not musical approval')
    if a.report:
        a.report.parent.mkdir(parents=True, exist_ok=True)
        a.report.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
