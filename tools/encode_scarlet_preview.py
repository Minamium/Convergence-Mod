#!/usr/bin/env python3
"""Turn the Scarlet rig harness output (tools/preview-scarlet-rigs.ps1) into review videos and a review page.

    py -3.12 tools/encode_scarlet_preview.py .local/scarlet-rigs --sfx-a --bgm --page

* Every render listed in index.json becomes videos/<scene>-<camera>-<variant>.webm (VP9 crf 32, 60 fps) carrying
  sound set A on the exact Born / Fire ticks the harness recorded (state.json "sounds"), voiced like ScarletVoices:
  the cue table (file, MaxInstances, ReplaceOldest / IgnoreNew, lease) and ScarletSounds.Gain are PARSED from
  Client/Encounters/CrimsonFoundry/ScarletSounds.cs, so the mux never drifts from the game. ReplaceOldest is a hard
  stop, as in tModLoader. With --bgm a "-bgm" version also carries the Graceful Ordeal window the harness arranged
  with the production CrimsonMusicMixer, at CrimsonInvocation.MusicGain.
* User sliders (--sliders): "owner" = sound 0.161 and music 0.685, both LINEAR here (the Scarlet score is its own
  DynamicSoundEffectInstance at MusicGain * Main.musicVolume; sound effects scale linearly) or "unity". One fixed
  make-up gain (+12 dB) for every video, lowered only when a mix would pass -1 dBFS; the gain is recorded.
* --pairs: side-by-side videos (left off, right on) for the material and the residue-yield switches when both
  renders exist (<scene>-<camera>-<variant without -material>-offon.webm, <scene>-<camera>-<variant>-yield-offon.webm).
* The page opens with the design's owner list (§4.4 videos 1-20) mapped to the files that exist.
* --page: review/index.html (and the same page as index.html at the output root) with the videos, stills, state
  curves, gates, G10 visibility and skipped renders. Serve the output directory with a local HTTP server that answers
  Range requests (videos seek), then open / or /review/.

Offline review only: not a playtest; in-game acceptance stays not_run. Needs numpy and imageio-ffmpeg (py -3.12).
"""
from __future__ import annotations

import argparse
import html
import json
import os
import pathlib
import re
import struct
import subprocess
import sys
import wave

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOUNDS_CS = ROOT / 'Client/Encounters/CrimsonFoundry/ScarletSounds.cs'
VISUALS_CS = ROOT / 'Client/Encounters/CrimsonFoundry/CrimsonGestureVisuals.cs'
SOUND_DIR = ROOT / 'Assets/Sounds/CrimsonFoundry'
RATE = 48000
FRAMES_PER_TICK = RATE // 60
SLIDERS = {'owner': (0.161, 0.685), 'unity': (1.0, 1.0)}  # (sound, music)
MAKEUP = 10 ** (12 / 20)
CEILING = 10 ** (-1 / 20)
NOTICE = 'Offline render of the production rigs, forecasts, seals and ink. Not a playtest; in-game acceptance is not_run.'


# ---- the production cue table -------------------------------------------------------------------------------------

def parse_sound_table(text: str) -> dict:
    """ScarletSounds.cs -> {cue: {file, ticks, instances, limit, lease}} plus the shared gain and fade."""
    enum = re.search(r'enum\s+ScarletCue\s*:\s*byte\s*\{([^}]*)\}', text)
    if not enum:
        raise ValueError('ScarletCue enum not found')
    names = [n.strip() for n in enum.group(1).replace('\n', ' ').split(',') if n.strip()]
    specs = re.findall(r'new\("(\w+)",\s*(\d+),\s*(\d+),\s*SoundLimitBehavior\.(\w+)\)', text)
    if len(specs) != len(names):
        raise ValueError(f'{len(names)} cues but {len(specs)} specs')
    gain = float(re.search(r'const\s+float\s+Gain\s*=\s*([\d.]+)f', text).group(1))
    fade = int(re.search(r'const\s+int\s+FadeTicks\s*=\s*(\d+)', text).group(1))
    margin_match = re.search(r'LeaseMargin\s*=\s*FadeTicks\s*\+\s*(\d+)', text)
    margin = fade + (int(margin_match.group(1)) if margin_match else 0)
    cues = {name: {'file': f, 'ticks': int(t), 'instances': int(i), 'limit': limit, 'lease': int(t) + margin}
            for name, (f, t, i, limit) in zip(names, specs)}
    return {'gain': gain, 'fade': fade, 'cues': cues}


def voice_capacity(text: str) -> int:
    match = re.search(r'new\s+ScarletVoices\((\d+)\)', text)
    return int(match.group(1)) if match else 24


def schedule(events: list, table: dict, lengths: dict, capacity: int = 24) -> list:
    """Voices for the recorded cues: [{cue, start, end}] in samples. A voice ends with its file or its lease; a new
    voice past MaxInstances stops the oldest of that cue (ReplaceOldest, hard) or is dropped (IgnoreNew); past the
    ScarletVoices capacity it is dropped."""
    voices: list = []
    for event in sorted(events, key=lambda e: (e['frame'], e['cue'])):
        spec = table['cues'][event['cue']]
        start = event['frame'] * FRAMES_PER_TICK
        end = start + min(lengths.get(event['cue'], spec['ticks'] * FRAMES_PER_TICK), spec['lease'] * FRAMES_PER_TICK)
        active = [v for v in voices if v['end'] > start]
        if len(active) >= capacity:
            continue
        same = sorted((v for v in active if v['cue'] == event['cue']), key=lambda v: v['start'])
        if len(same) >= spec['instances']:
            if spec['limit'] == 'IgnoreNew':
                continue
            same[0]['end'] = start
        voices.append({'cue': event['cue'], 'start': start, 'end': end, 'tick': event['tick']})
    return voices


def makeup_gain(peak: float) -> float:
    """The fixed +12 dB, lowered only so the mix stays under -1 dBFS."""
    return MAKEUP if peak * MAKEUP <= CEILING or peak <= 0 else CEILING / peak


# ---- audio io -----------------------------------------------------------------------------------------------------

def ffmpeg_exe(explicit: str | None) -> str:
    if explicit:
        return explicit
    if os.environ.get('FFMPEG'):
        return os.environ['FFMPEG']
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def decode(ff: str, path: pathlib.Path):
    import numpy as np
    raw = subprocess.run([ff, '-v', 'error', '-i', str(path), '-f', 'f32le', '-ac', '2', '-ar', str(RATE), '-'],
                         check=True, capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def read_wav(path: pathlib.Path):
    """PCM16 or float32 RIFF (the harness writes float32 stereo 48 kHz)."""
    import numpy as np
    data = path.read_bytes()
    if data[:4] != b'RIFF' or data[8:12] != b'WAVE':
        raise ValueError(f'{path} is not a WAV file')
    pos, fmt, channels, bits, body = 12, None, 2, 16, None
    while pos + 8 <= len(data):
        tag, size = data[pos:pos + 4], struct.unpack('<I', data[pos + 4:pos + 8])[0]
        chunk = data[pos + 8:pos + 8 + size]
        if tag == b'fmt ':
            fmt, channels, rate, _, _, bits = struct.unpack('<HHIIHH', chunk[:16])
            if rate != RATE:
                raise ValueError(f'{path}: {rate} Hz, expected {RATE}')
        elif tag == b'data':
            body = chunk
        pos += 8 + size + (size & 1)
    if body is None or fmt is None:
        raise ValueError(f'{path}: no fmt/data chunk')
    samples = np.frombuffer(body, dtype=np.float32 if fmt == 3 else np.int16).astype(np.float32)
    if fmt != 3:
        samples /= 32768.0
    return samples.reshape(-1, channels)


def write_wav16(path: pathlib.Path, data) -> None:
    import numpy as np
    pcm = (np.clip(data, -1, 1) * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())


class Mixer:
    def __init__(self, ff: str, sliders: str):
        self.ff = ff
        text = SOUNDS_CS.read_text(encoding='utf-8')
        self.table = parse_sound_table(text)
        self.capacity = voice_capacity(VISUALS_CS.read_text(encoding='utf-8')) if VISUALS_CS.exists() else 24
        self.sound, self.music = SLIDERS[sliders]
        self.sliders = sliders
        self.cache: dict = {}

    def clip(self, cue: str):
        if cue not in self.cache:
            self.cache[cue] = decode(self.ff, SOUND_DIR / (self.table['cues'][cue]['file'] + '.ogg'))
        return self.cache[cue]

    def render(self, state: dict, frames: int, bgm_path: pathlib.Path | None):
        import numpy as np
        total = frames * FRAMES_PER_TICK
        out = np.zeros((total, 2), dtype=np.float32)
        lengths = {cue: len(self.clip(cue)) for cue in {e['cue'] for e in state['sounds']}}
        voices = schedule(state['sounds'], self.table, lengths, self.capacity)
        sfx_gain = self.table['gain'] * self.sound
        for v in voices:
            clip = self.clip(v['cue'])[: v['end'] - v['start']]
            end = min(total, v['start'] + len(clip))
            if end > v['start']:
                out[v['start']:end] += clip[: end - v['start']] * sfx_gain
        music_gain = None
        if bgm_path is not None and bgm_path.exists() and state.get('bgm'):
            music = read_wav(bgm_path)[:total]
            music_gain = state['bgm']['musicGain'] * self.music
            out[: len(music)] += music * music_gain
        peak = float(np.abs(out).max()) if total else 0.0
        gain = makeup_gain(peak)
        meta = {'sliders': {'name': self.sliders, 'sound': self.sound, 'music': self.music}, 'sfxGain': sfx_gain,
                'musicGain': music_gain, 'makeupDb': round(20 * np.log10(gain), 2), 'peakDbfs': round(20 * np.log10(max(peak * gain, 1e-9)), 2),
                'voices': [{'cue': v['cue'], 'tick': v['tick'], 'file': self.table['cues'][v['cue']]['file'],
                            'samples': v['end'] - v['start']} for v in voices]}
        return out * gain, meta


# ---- video ---------------------------------------------------------------------------------------------------------

VP9 = ['-c:v', 'libvpx-vp9', '-crf', '32', '-b:v', '0', '-pix_fmt', 'yuv420p', '-row-mt', '1', '-deadline', 'good', '-cpu-used', '4']


def parse_variant(variant: str) -> dict:
    """The harness's RigVariant.Suffix: normal|reduced, then -material, -proposed, -yieldoff in that order."""
    parts = variant.split('-')
    flags = set(parts[1:])
    if parts[0] not in ('normal', 'reduced') or not flags <= {'material', 'proposed', 'yieldoff'}:
        raise ValueError(f'unknown variant {variant}')
    return {'reduced': parts[0] == 'reduced', 'material': 'material' in flags, 'proposed': 'proposed' in flags, 'yieldoff': 'yieldoff' in flags}


def variant_name(reduced: bool, material: bool, proposed: bool, yieldoff: bool) -> str:
    return ('reduced' if reduced else 'normal') + ('-material' if material else '') + ('-proposed' if proposed else '') + ('-yieldoff' if yieldoff else '')


def pair_plan(keys: set) -> list:
    """(scene, camera, off variant, on variant, target stem) for every material and residue-yield pair both renders of
    which exist. keys: {(scene, camera, variant)}."""
    plan = []
    for scene, camera, variant in sorted(keys):
        v = parse_variant(variant)
        if not v['material']:
            on = variant_name(v['reduced'], True, v['proposed'], v['yieldoff'])
            if (scene, camera, on) in keys:
                plan.append((scene, camera, variant, on, f'{scene}-{camera}-{variant}-offon'))
        if not v['yieldoff']:
            off = variant_name(v['reduced'], v['material'], v['proposed'], True)
            if (scene, camera, off) in keys:
                plan.append((scene, camera, off, variant, f'{scene}-{camera}-{variant}-yield-offon'))
    return plan


# The design's owner outputs (§4.4 table): number, design name, the harness file it is, what it is for.
def owner_videos() -> list:
    game = variant_name(False, True, True, False)
    rows = []
    for act in (1, 2, 3):
        rows.append((act, f'act{act}-signature-A2-normal.webm', f'act{act}-signature-A2-{game}.webm', 'the main call: in the air, real size, sound A'))
    for act in (1, 2, 3):
        rows.append((3 + act, f'act{act}-signature-C-normal-offon.webm', f'act{act}-signature-C-{variant_name(False, False, True, False)}-offon.webm',
                     'body material off (left) and on (right), proposed motion in both'))
    for act in (1, 2, 3):
        rows.append((6 + act, f'act{act}-signature-A-normal.webm', f'act{act}-signature-A-{game}.webm', 'visible from the ground? (design §6 Q1)'))
    for act in (1, 2, 3):
        rows.append((9 + act, f'act{act}-basic-A2-normal.webm', f'act{act}-basic-A2-{game}.webm', 'carried over to the basic phrase (Register .6)'))
    for act in (1, 2, 3):
        rows.append((12 + act, f'act{act}-signature-A2-reduced.webm', f'act{act}-signature-A2-{variant_name(True, True, True, False)}.webm', 'Reduced Effects'))
    for act in (1, 2, 3):
        rows.append((15 + act, f'act{act}-signature-V-normal.webm', f'act{act}-signature-V-{game}.webm', "Vespera's command"))
    for act in (1, 2):
        rows.append((18 + act, f'act{act}-signature-A2-yield-offon.webm', f'act{act}-signature-A2-{game}-yield-offon.webm',
                     'signature residue: yield off (left) and on (right) (design §6 Q2)'))
    return rows


def run(cmd: list) -> None:
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(' '.join(map(str, cmd[:6])) + ' ...\n' + result.stderr[-2000:])


def silent_video(ff: str, folder: pathlib.Path) -> pathlib.Path | None:
    video = folder / 'video.webm'
    if video.exists():
        return video
    if (folder / 'f0000.png').exists():
        run([ff, '-y', '-v', 'error', '-framerate', '60', '-i', str(folder / 'f%04d.png'), *VP9, str(video)])
        return video
    return None


def mux(ff: str, video: pathlib.Path, audio, target: pathlib.Path, scratch: pathlib.Path) -> None:
    write_wav16(scratch, audio)
    try:
        run([ff, '-y', '-v', 'error', '-i', str(video), '-i', str(scratch), '-map', '0:v', '-map', '1:a',
             '-c:v', 'copy', '-c:a', 'libopus', '-b:a', '160k', '-shortest', str(target)])
    finally:
        scratch.unlink(missing_ok=True)


def pair(ff: str, left: pathlib.Path, right: pathlib.Path, target: pathlib.Path) -> None:
    run([ff, '-y', '-v', 'error', '-i', str(left), '-i', str(right), '-filter_complex',
         '[0:v]scale=960:540[a];[1:v]scale=960:540[b];[a][b]hstack=inputs=2[v]', '-map', '[v]', '-map', '1:a?',
         *VP9, '-c:a', 'copy', str(target)])


# ---- page ----------------------------------------------------------------------------------------------------------

SERIES = ['apparition charge', 'heat', 'ignite', 'drain', 'send']


def body_value(tick: dict, name: str) -> float:
    """S1's body envelopes as the rigs draw them (ticks[].attack.body); the Choir's Send is its most advanced limb."""
    body = ((tick.get('attack') or {}).get('body')) or {}
    if name == 'send' and 'limbs' in body:
        return max((limb[1] for limb in body['limbs']), default=0.0)
    return float(body.get(name, 0.0))


def curves(state: dict) -> dict:
    """Compact per-tick series for the page (relative ticks)."""
    ticks = state['ticks']
    return {
        'scene': state['scene'], 'camera': state['camera'], 'variant': state['variant'],
        'rel': [t['rel'] for t in ticks],
        'series': {
            'apparition charge': [t['apparition'][0] for t in ticks],
            **{name: [body_value(t, name) for t in ticks] for name in SERIES[1:]},
        },
        'origin': ticks[0]['tick'] - ticks[0]['rel'] if ticks else 0,
        'plans': [{'technique': p['technique'], 'pulse': p['pulse'], 'role': p['role'], 'born': p['born'], 'fire': p['fire'], 'end': p['end']}
                  for p in state['plans']],
        'sounds': state['sounds'],
    }


PAGE_CSS = """
:root{color-scheme:dark;--bg:#121011;--panel:#1b1819;--line:#3a3436;--text:#f2eef0;--muted:#b9b0b4;--faint:#857c80;
--accent:#e66767;--s1:#3987e5;--s2:#d95926;--s3:#199e70;--s4:#b07be8;--s5:#c9a400;--pass:#199e70;--fail:#e66767;--warn:#c98500}
@media (prefers-color-scheme: light){:root:not([data-theme="dark"]){color-scheme:light;--bg:#f6f4f2;--panel:#fcfcfb;--line:#ddd7d3;
--text:#0b0b0b;--muted:#52514e;--faint:#77736e;--accent:#b8323a;--s1:#2a78d6;--s2:#eb6834;--s3:#1baf7a;--s4:#7a45c2;--s5:#8f7400;--pass:#147a55;--fail:#b8323a;--warn:#8a5d00}}
:root[data-theme="dark"]{color-scheme:dark}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,"Segoe UI","Yu Gothic UI",sans-serif}
main{max-width:1240px;margin:0 auto;padding:24px 16px 64px}h1{font-size:22px;margin:0 0 4px}h2{font-size:18px;margin:36px 0 10px;border-bottom:1px solid var(--line);padding-bottom:6px}
h3{font-size:15px;margin:22px 0 8px;color:var(--muted)}.notice{background:var(--panel);border-left:4px solid var(--accent);padding:10px 14px;margin:12px 0 20px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(360px,1fr));gap:14px}.card{background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:10px}
video,img{width:100%;border-radius:4px;background:#000;display:block}.cap{font-size:13px;color:var(--muted);margin-top:6px;word-break:break-all}
table{border-collapse:collapse;width:100%;font-size:13px}th,td{border-bottom:1px solid var(--line);padding:5px 8px;text-align:left;vertical-align:top}th{color:var(--muted);font-weight:600}
.st{font-weight:600}.pass{color:var(--pass)}.fail{color:var(--fail)}.not_run,.baseline_written{color:var(--warn)}code{font-size:12px}
.chart{position:relative}.legend{display:flex;gap:16px;font-size:13px;color:var(--muted);margin:4px 0}.legend i{display:inline-block;width:14px;height:3px;border-radius:2px;margin-right:6px;vertical-align:middle}
.tip{position:absolute;pointer-events:none;background:var(--panel);border:1px solid var(--line);border-radius:6px;padding:6px 8px;font-size:12px;display:none;white-space:nowrap;z-index:2}
details{margin:6px 0}summary{cursor:pointer;color:var(--muted)}
"""

PAGE_JS = """
const DATA = JSON.parse(document.getElementById('data').textContent);
const SERIES = [['apparition charge','--s1'],['heat','--s2'],['ignite','--s3'],['drain','--s4'],['send','--s5']];
for (const c of DATA.curves) {
  const host = document.getElementById('curve-' + c.scene); if (!host) continue;
  const W = 960, H = 220, L = 40, R = 10, T = 18, B = 26, x0 = c.rel[0], x1 = c.rel[c.rel.length - 1];
  const X = r => L + (r - x0) / Math.max(1, x1 - x0) * (W - L - R), Y = v => T + (1 - v) * (H - T - B);
  let s = `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="${c.scene} attack inputs per tick">`;
  for (const v of [0, .5, 1]) s += `<line x1="${L}" x2="${W - R}" y1="${Y(v)}" y2="${Y(v)}" stroke="var(--line)" stroke-width="1"/><text x="${L - 6}" y="${Y(v) + 4}" text-anchor="end" font-size="11" fill="var(--faint)">${v}</text>`;
  for (const p of c.plans) { if (p.role !== 'current') continue;
    const b = X(p.born - c.origin), f = X(p.fire - c.origin);
    s += `<line x1="${b}" x2="${b}" y1="${T}" y2="${H - B}" stroke="var(--faint)" stroke-dasharray="3 4" stroke-width="1"/>`;
    s += `<line x1="${f}" x2="${f}" y1="${T}" y2="${H - B}" stroke="var(--muted)" stroke-width="1"/><text x="${f + 3}" y="${T + 10}" font-size="11" fill="var(--muted)">F${p.pulse + 1}</text>`; }
  for (const e of c.sounds) { const x = X(e.tick - c.origin); s += `<path d="M${x - 4} ${H - B + 10} L${x + 4} ${H - B + 10} L${x} ${H - B + 3} Z" fill="var(--faint)"/>`; }
  for (const [name, color] of SERIES) { const v = c.series[name];
    s += `<polyline fill="none" stroke="var(${color})" stroke-width="2" stroke-linejoin="round" points="${v.map((y, i) => X(c.rel[i]) + ',' + Y(y)).join(' ')}"/>`; }
  s += `<line class="cross" x1="0" x2="0" y1="${T}" y2="${H - B}" stroke="var(--text)" stroke-width="1" opacity="0"/>`;
  s += `<text x="${W - R}" y="${H - 4}" text-anchor="end" font-size="11" fill="var(--faint)">tick from the phrase's first warning</text></svg>`;
  host.innerHTML = s;
  const svg = host.querySelector('svg'), cross = svg.querySelector('.cross'), tip = host.parentElement.querySelector('.tip');
  svg.addEventListener('mousemove', ev => { const box = svg.getBoundingClientRect(), px = (ev.clientX - box.left) / box.width * W;
    const r = Math.round(x0 + (px - L) / (W - L - R) * (x1 - x0)), i = Math.max(0, Math.min(c.rel.length - 1, r - x0)), x = X(c.rel[i]);
    cross.setAttribute('x1', x); cross.setAttribute('x2', x); cross.setAttribute('opacity', .5);
    const cues = c.sounds.filter(e => e.tick - c.origin === c.rel[i]).map(e => e.cue + ' (note ' + (e.pulse + 1) + ')');
    tip.innerHTML = `<b>T${c.rel[i] >= 0 ? '+' : ''}${c.rel[i]}</b><br>` + SERIES.map(([n]) => `${n}: ${c.series[n][i].toFixed(3)}`).join('<br>') + (cues.length ? '<br>sound: ' + cues.join(', ') : '');
    tip.style.display = 'block'; tip.style.left = Math.min(box.width - 200, (x / W) * box.width + 12) + 'px'; tip.style.top = '30px'; });
  svg.addEventListener('mouseleave', () => { tip.style.display = 'none'; cross.setAttribute('opacity', 0); });
}
"""


def e(text) -> str:
    return html.escape(str(text))


def page(out: pathlib.Path, index: dict, gates: dict | None, videos: list, pairs: list, curve_sets: list, audio_meta: dict) -> pathlib.Path:
    review = out / 'review'
    review.mkdir(exist_ok=True)
    parts = [f'<!doctype html><html lang="ja"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
             f'<title>Scarlet rig review</title><style>{PAGE_CSS}</style></head><body><main>',
             '<h1>Scarlet Invocation: attack expression, offline rig review</h1>',
             f'<div class="notice"><b>オフライン描画・プレイテストではない・実機の受け入れは not_run。</b><br>{e(NOTICE)}<br>'
             f'Not reproduced: {e(", ".join(index.get("notReproduced", [])))}.</div>']
    notes = out / 'review-notes.json'
    if notes.exists():
        # Optional reviewer notes for this render (a JSON list of plain strings), e.g. what changed since the last one.
        items = json.loads(notes.read_text(encoding='utf-8'))
        parts.append('<h2>Changes in this render</h2><ul>' + ''.join(f'<li>{e(str(item))}</li>' for item in items) + '</ul>')
    if audio_meta:
        parts.append(f'<p class="cap">Sound: set A from ScarletSounds.cs on the recorded Born/Fire ticks; sliders {e(audio_meta["sliders"])}; '
                     f'BGM = arranged Graceful Ordeal at MusicGain x music slider; one +12 dB make-up for all videos (lowered only to stay under -1 dBFS).</p>')
    existing = {v['file'] for v in videos + pairs}
    parts.append('<h2>Owner outputs (design §4.4)</h2><p class="cap">The in-game picture is "material on, proposed motion" '
                 '(the body material, the approved motion and Vespera\'s command). Each video carries sound set A; the "-bgm" twin adds the arranged score.</p>'
                 '<table><tr><th>#</th><th>design name</th><th>file</th><th>for</th></tr>')
    for number, design, file, purpose in owner_videos():
        bgm = file.replace('.webm', '-bgm.webm')
        cell = (f'<a href="../videos/{e(file)}">{e(file)}</a>' if file in existing else f'<span class="fail">missing: {e(file)}</span>')
        if bgm in existing:
            cell += f' (<a href="../videos/{e(bgm)}">+BGM</a>)'
        parts.append(f'<tr><td>{number}</td><td><code>{e(design)}</code></td><td>{cell}</td><td>{e(purpose)}</td></tr>')
    parts.append('</table>')
    parts.append('<h2>Videos</h2>')
    by_scene: dict = {}
    for v in videos + pairs:
        by_scene.setdefault(v['scene'], []).append(v)
    for scene, items in by_scene.items():
        parts.append(f'<h3>{e(scene)}</h3><div class="grid">')
        for v in items:
            parts.append(f'<div class="card"><video controls preload="metadata" src="../videos/{e(v["file"])}"></video>'
                         f'<div class="cap">{e(v["file"])}{" — " + e(v["note"]) if v.get("note") else ""}</div></div>')
        parts.append('</div>')
    stills = [r for r in index.get('renders', []) if r.get('stills')]
    if stills:
        parts.append('<h2>Stills (zoom 1, real size)</h2>')
        for r in stills:
            parts.append(f'<h3>{e(r["scene"])} {e(r["camera"])}-{e(r["variant"])}</h3><div class="grid">')
            for label in r['stills']:
                src = f'../{r["dir"]}/stills/{label}.png'
                parts.append(f'<div class="card"><a href="{e(src)}"><img loading="lazy" src="{e(src)}" alt="{e(label)}"></a><div class="cap">{e(label)}</div></div>')
            parts.append('</div>')
    if curve_sets:
        parts.append('<h2>Attack clock per tick (state.json)</h2>'
                     '<p class="cap">What the rigs draw from the plans: the apparition\'s Signal charge (today\'s input) and S1\'s body envelopes the material draws '
                     '(Heat: the warning\'s draw-in, Ignite: the strike, Drain: the body sinking with the river, Send: the blood front reaching the hooks / fingertips, 1 on Fire; '
                     'the Choir\'s Send is its most advanced limb). Dashed lines: warnings (Born); solid lines: strikes (Fire); triangles: sound cues. '
                     'Every channel (Front, Return, Snap, motion, per-arm values) is in state.json ticks[].attack.</p>')
        legend = ''.join(f'<span><i style="background:var({c})"></i>{e(n)}</span>' for n, c in
                         [('apparition charge', '--s1'), ('heat', '--s2'), ('ignite', '--s3'), ('drain', '--s4'), ('send', '--s5')])
        for c in curve_sets:
            parts.append(f'<h3>{e(c["scene"])} ({e(c["camera"])}-{e(c["variant"])})</h3><div class="legend">{legend}</div>'
                         f'<div class="chart"><div id="curve-{e(c["scene"])}"></div><div class="tip"></div></div>'
                         '<details><summary>Sound cues (table)</summary><table><tr><th>tick from first warning</th><th>cue</th><th>note</th><th>technique</th></tr>'
                         + ''.join(f'<tr><td>{s["tick"] - c["origin"]:+d}</td><td>{e(s["cue"])}</td><td>{s["pulse"] + 1}</td><td>{e(s["technique"])}</td></tr>' for s in c['sounds'])
                         + '</table></details>')
    if gates:
        parts.append('<h2>Gates</h2><table><tr><th>id</th><th>status</th><th>gate</th><th>note</th></tr>')
        for g in gates['gates'] + gates.get('self', []):
            status = g['Status'] + (' (vacuous)' if g.get('Vacuous') else '')
            parts.append(f'<tr><td>{e(g["Id"])}</td><td class="st {e(g["Status"])}">{e(status)}</td><td>{e(g["Title"])}</td><td>{e(g["Note"])}</td></tr>')
        parts.append('</table>')
        g10 = next((g for g in gates['gates'] if g['Id'] == 'G10'), None)
        if g10 and g10.get('Measured'):
            parts.append('<h3>G10: share of the body inside the in-game screen</h3><table><tr><th>scene</th><th>A (ground) mean / min / max</th><th>A2 (air) mean / min / max</th></tr>')
            for row in g10['Measured']:
                fa, fb = row['A'], row['A2']
                parts.append(f'<tr><td>{e(row["scene"])}</td><td>{fa["mean"]:.0%} / {fa["min"]:.0%} / {fa["max"]:.0%}</td><td>{fb["mean"]:.0%} / {fb["min"]:.0%} / {fb["max"]:.0%}</td></tr>')
            parts.append('</table>')
        parts.append('<details><summary>gates.json (measurements)</summary><pre style="white-space:pre-wrap;font-size:11px">'
                     + e(json.dumps(gates, ensure_ascii=False, indent=1)[:200000]) + '</pre></details>')
    if index.get('skipped'):
        parts.append('<h2>Not rendered (not_run)</h2><table><tr><th>scene</th><th>camera-variant</th><th>reason</th></tr>')
        for s in index['skipped']:
            parts.append(f'<tr><td>{e(s["scene"])}</td><td>{e(s["camera"])}-{e(s["variant"])}</td><td>{e(s["reason"])}</td></tr>')
        parts.append('</table>')
    data = json.dumps({'curves': curve_sets}, ensure_ascii=False).replace('</', '<\\/')
    parts.append(f'<script id="data" type="application/json">{data}</script><script>{PAGE_JS}</script></main></body></html>')
    target = review / 'index.html'
    html = ''.join(parts)
    target.write_text(html, encoding='utf-8')
    # The same page at the output root (/index.html), its links one level up.
    (out / 'index.html').write_text(html.replace('src="../', 'src="').replace('href="../', 'href="'), encoding='utf-8')
    return target


# ---- main ----------------------------------------------------------------------------------------------------------

def main(argv: list | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('output', type=pathlib.Path, help='the harness output directory (index.json)')
    parser.add_argument('--sfx-a', action='store_true', help='mux sound set A on the recorded Born/Fire ticks')
    parser.add_argument('--bgm', action='store_true', help='also write a version over the arranged Graceful Ordeal window')
    parser.add_argument('--pairs', action='store_true', help='side-by-side material off|on and yield off|on videos')
    parser.add_argument('--page', action='store_true', help='write review/index.html')
    parser.add_argument('--sliders', choices=sorted(SLIDERS), default='owner')
    parser.add_argument('--ffmpeg')
    args = parser.parse_args(argv)
    out = args.output if args.output.is_absolute() else (pathlib.Path.cwd() / args.output)
    index = json.loads((out / 'index.json').read_text(encoding='utf-8'))
    gates_path = out / 'gates.json'
    gates = json.loads(gates_path.read_text(encoding='utf-8')) if gates_path.exists() else None
    ff = ffmpeg_exe(args.ffmpeg)
    videos_dir = out / 'videos'
    videos_dir.mkdir(exist_ok=True)
    mixer = Mixer(ff, args.sliders) if args.sfx_a or args.bgm else None
    videos, pairs, curve_sets, report = [], [], [], {}
    seen_curves = set()
    renders = sorted(index.get('renders', []), key=lambda r: (r['scene'], r['variant'].startswith('reduced'), r['camera'] != 'A2'))
    for r in renders:
        folder = out / r['dir']
        state = json.loads((folder / 'state.json').read_text(encoding='utf-8'))
        if r['scene'] not in seen_curves and r['camera'] in ('A2', 'A', 'C', 'V', 'B'):
            seen_curves.add(r['scene'])
            curve_sets.append(curves(state))
        silent = silent_video(ff, folder)
        if silent is None:
            continue
        name = f'{r["scene"]}-{r["camera"]}-{r["variant"]}'
        if mixer is None:
            # No audio asked for: keep any video an earlier run muxed; only a missing one gets the silent copy.
            target = videos_dir / f'{name}.webm'
            if not target.exists():
                target.write_bytes(silent.read_bytes())
            videos.append({'scene': r['scene'], 'file': target.name, 'note': ''})
            if (videos_dir / f'{name}-bgm.webm').exists():
                videos.append({'scene': r['scene'], 'file': f'{name}-bgm.webm', 'note': 'BGM'})
            continue
        frames = r['frames']
        if args.sfx_a:
            audio, meta = mixer.render(state, frames, None)
            mux(ff, silent, audio, videos_dir / f'{name}.webm', folder / 'audio.tmp.wav')
            videos.append({'scene': r['scene'], 'file': f'{name}.webm', 'note': 'sound set A'})
            report[f'{name}.webm'] = meta
        if args.bgm and state.get('bgm'):
            audio, meta = mixer.render(state if args.sfx_a else {**state, 'sounds': []}, frames, folder / state['bgm']['file'])
            mux(ff, silent, audio, videos_dir / f'{name}-bgm.webm', folder / 'audio.tmp.wav')
            videos.append({'scene': r['scene'], 'file': f'{name}-bgm.webm', 'note': ('sound set A + ' if args.sfx_a else '') + 'BGM'})
            report[f'{name}-bgm.webm'] = meta
        print(f'{name}: {"sound A " if args.sfx_a else ""}{"+ BGM" if args.bgm else ""}')
    if args.pairs:
        keys = {(r['scene'], r['camera'], r['variant']) for r in index.get('renders', [])}
        for scene, camera, off_variant, on_variant, stem in pair_plan(keys):
            left, right = videos_dir / f'{scene}-{camera}-{off_variant}.webm', videos_dir / f'{scene}-{camera}-{on_variant}.webm'
            if not left.exists() or not right.exists():
                continue
            target = videos_dir / f'{stem}.webm'
            pair(ff, left, right, target)
            pairs.append({'scene': scene, 'file': target.name, 'note': 'left off, right on'})
            print(f'{target.name}: side by side')
    if not args.pairs:
        for path in sorted(videos_dir.glob('*offon.webm')):
            pairs.append({'scene': path.name.split('-')[0] + '-' + path.name.split('-')[1], 'file': path.name, 'note': 'left off, right on'})
    if mixer is None and (videos_dir / 'audio.json').exists():
        report = None
    if report is not None:
        (videos_dir / 'audio.json').write_text(json.dumps({'note': NOTICE, 'table': mixer.table if mixer else None, 'videos': report},
                                                      ensure_ascii=False, indent=1), encoding='utf-8')
    if args.page:
        audio_meta = {}
        if mixer:
            audio_meta = {'sliders': f'{args.sliders} (sound {SLIDERS[args.sliders][0]}, music {SLIDERS[args.sliders][1]})'}
        elif (videos_dir / 'audio.json').exists():
            earlier = json.loads((videos_dir / 'audio.json').read_text(encoding='utf-8')).get('videos') or {}
            first = next(iter(earlier.values()), None)
            if first:
                s = first['sliders']
                audio_meta = {'sliders': f'{s["name"]} (sound {s["sound"]}, music {s["music"]})'}
        target = page(out, index, gates, videos, pairs, curve_sets, audio_meta)
        print(f'page: {target}')
        print(f'serve {out} with a Range-capable local HTTP server and open /review/index.html')
    return 0


if __name__ == '__main__':
    sys.exit(main())
