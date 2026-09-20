"""Keep every sample of the approved full game track, then append a musical return.

Input must be the preserved pre-shortening OGG and its original Score.json.
Numerical seam checks are not listening approval; external masters are untouched.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import wave
import numpy as np
from reloop_crimson_score import decode


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('audio', 'score', 'ffmpeg', 'export', 'report'):
        p.add_argument('--' + name, type=Path, required=True)
    a = p.parse_args()
    if hashlib.sha256(a.audio.read_bytes()).hexdigest() != 'f8f0a566a18dfa7d9ba2fd6203d877160bc6e0ae04123a694a2c9307bbaaffa9':
        raise ValueError('Use the approved complete pre-shortening game asset, never the shortened edit')
    x = decode(a.audio, a.ffmpeg)
    s = json.loads(a.score.read_text(encoding='utf-8'))
    # Return to an already measured strong section, but do not discard the later climax.
    start_index = min(range(len(s['BeatTicks'])), key=lambda i: abs(s['BeatTicks'][i] - 3289))
    start = s['BeatTicks'][start_index] * 800
    period = int(round(np.median(np.diff(s['BeatTicks'][-32:]))))
    # Append one complete bar after the original ending, aligned to the beat map.
    last = s['BeatTicks'][-1]
    end_tick = last + period * 5
    end = end_tick * 800
    bridge_len = end - len(x)
    if bridge_len < 48000 or start < bridge_len: raise ValueError('Invalid complete-track bridge')
    # Original ending already decays toward its old loop point. Continue that
    # point briefly while smoothly revealing the bar immediately before return.
    old_start = s['LoopStartSample']
    outgoing = x[old_start:old_start + bridge_len].copy()
    incoming = x[start - bridge_len:start]
    t = np.linspace(0, 1, bridge_len)[:, None]
    t = t*t*(3-2*t)
    bridge = outgoing * (1-t) + incoming * t
    # Preserve continuity at the append itself without touching any original sample.
    n = min(480, bridge_len)
    bridge[:n] += (x[-1] - bridge[0]) * (1 - np.linspace(0, 1, n)[:, None])
    mixed = np.concatenate((x, bridge))
    gain = min(1., .92 / np.max(np.abs(mixed)))
    mixed *= gain
    original_ticks, original_energy = list(s['BeatTicks']), list(s['Energy'])
    for j in range(1, 5):
        s['BeatTicks'].append(last + period * j)
        s['Energy'].append(float(s['Energy'][start_index]))
    s.update(LoopStartSample=start, LoopEndSample=end, IntroTicks=900)
    a.export.mkdir(parents=True, exist_ok=True)
    out = a.export / 'GracefulOrdeal.ogg'
    subprocess.run([str(a.ffmpeg), '-v', 'error', '-y', '-f', 'f32le', '-ar', '48000', '-ac', '2', '-i', '-',
                    '-c:a', 'libvorbis', '-q:a', '7', '-metadata', 'ARTIST=kuku',
                    '-metadata', 'TITLE=Graceful Ordeal - complete Convergence game loop',
                    '-metadata', f'LOOPSTART={start}', '-metadata', f'LOOPEND={end}', str(out)],
                   input=mixed.astype('<f4').tobytes(), check=True)
    (a.export / 'Score.json').write_text(json.dumps(s, separators=(',', ':')) + '\n', encoding='utf-8', newline='\n')
    decoded = decode(out, a.ffmpeg)
    if len(decoded) < end or np.max(np.abs(decoded)) >= 1: raise ValueError('Invalid encoded duration/headroom')
    a.report.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(a.report.parent / 'full-loop-seam-preview.wav'), 'wb') as w:
        w.setparams((2, 2, 48000, 0, 'NONE', 'not compressed'))
        audition = np.concatenate((decoded[end-48000*9:end], decoded[start:start+48000*8]))
        w.writeframes((audition * 32767).astype('<i2').tobytes())
    report = dict(preserved_seconds=len(x)/48000, appended_seconds=bridge_len/48000,
                  return_seconds=[end/48000, start/48000], original_samples_preserved_before_encode=True,
                  original_beat_data_preserved=s['BeatTicks'][:len(original_ticks)] == original_ticks and s['Energy'][:len(original_energy)] == original_energy,
                  gain_db=float(20*np.log10(gain)), decoded_peak=float(np.max(np.abs(decoded))),
                  seam_step=float(np.max(np.abs(decoded[end-1]-decoded[start]))),
                  sha256=hashlib.sha256(out.read_bytes()).hexdigest(), listening='not_run: owner audition required')
    a.report.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8', newline='\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__': main()
