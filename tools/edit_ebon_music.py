"""Cut Ebon Manor's phase cues from EigHt's "AutoMatador" with bar-exact loops.

The owner supplied the official free-BGM MP3 (BOOTH item 6178144). EigHt's terms
(https://eight-novel.fanbox.cc/posts/7647818, updated 2026-07-14) permit game
background use and editing, not standalone music distribution, Content ID or
streaming registration. Assets/ATTRIBUTION.md records credit and limits.

The track is a steady 125.000 BPM. Its first audible beat (0.235 s) is a
two-beat pickup: every section entry (the A drop, B, A' and the climax) lands
on the grid whose bar 0 starts at 1.195 s, so bar k starts at 1.195 + 1.92 k
seconds. Act One plays the whole intro from the pickup; the other cues start on
bar lines. LOOPSTART/LOOPEND are bar lines whose following material matches
(measured beat-timbre similarity 0.89-0.98); the last 60 ms before LOOPEND
crossfade into the audio that precedes LOOPSTART, so tModLoader's sample-exact
jump is continuous. Headroom -1.5 dB.
Requires numpy and soundfile (local audio tools, not CI).
"""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path

import numpy as np
import soundfile as sf

from ogg_tools import fixed_serial, read_vorbis_tags, set_vorbis_tags

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets" / "Music" / "EbonManor"
SOURCE_SHA256 = "af0e07f5fa3b7b0846e98cba94281cd507a20982d60c1d179842d79bf01eef43"
RATE = 48000
DOWNBEAT = 1.195
BAR = 1.92
GAIN_DB = -1.5
SEAM = 0.06

# name: (first bar, loop start bar, loop end bar or None, bars before the first attack downbeat)
CUES = {
    "ActOne": (-0.5, 20, 44, 14.5),  # pickup + whole intro -> A drop at bar 14; loop A -> breakdown -> B
    "ActTwo": (36, 40, 48, 2),       # build -> B at bar 38; loop the B groove
    "Finale": (51, 56, 68, 3),       # break -> A' at bar 54; loop A' -> gap -> climax
    "Curtain": (71, None, None, 0),  # outro after Victory, no loop tags
}


def bar_sample(bar):
    return round((DOWNBEAT + BAR * bar) * RATE)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def raised_cosine(count, rising=True):
    ramp = 0.5 - 0.5 * np.cos(np.linspace(0, math.pi, count))
    return ramp if rising else ramp[::-1]


def cut(song, first, loop_start, loop_end):
    begin = bar_sample(first)
    end = bar_sample(loop_end) if loop_end is not None else len(song)
    cue = song[begin:end].copy()
    fade_in = round(0.015 * RATE)
    cue[:fade_in] *= raised_cosine(fade_in)[:, None]
    loop = None
    if loop_end is not None:
        seam = round(SEAM * RATE)
        start = bar_sample(loop_start)
        # equal-power: the tail fades out while the audio leading into LOOPSTART fades in
        theta = np.linspace(0, math.pi / 2, seam)[:, None]
        cue[-seam:] = cue[-seam:] * np.cos(theta) + song[start - seam:start] * np.sin(theta)
        loop = (bar_sample(loop_start) - begin, bar_sample(loop_end) - begin)
    else:
        fade_out = round(0.01 * RATE)
        cue[-fade_out:] *= raised_cosine(fade_out, rising=False)[:, None]
    return cue, loop


def encode(name, cue, loop):
    buffer = io.BytesIO()
    # libsndfile's Vorbis writer overflows the stack on one multi-minute call; stream blocks
    with sf.SoundFile(buffer, "w", RATE, 2, format="OGG", subtype="VORBIS", compression_level=0.2) as out:
        for start in range(0, len(cue), 32768):
            out.write(cue[start:start + 32768].astype(np.float32))
    serial = int.from_bytes(hashlib.sha256(name.encode("utf-8")).digest()[:4], "little")
    tags = [("TITLE", f"AutoMatador ({name})"), ("ARTIST", "EigHt"),
            ("COMMENT", "Game-facing loop edit for Convergence; see Assets/ATTRIBUTION.md")]
    if loop is not None:
        tags += [("LOOPSTART", str(loop[0])), ("LOOPEND", str(loop[1]))]
    return set_vorbis_tags(fixed_serial(buffer.getvalue(), serial), tags)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, required=True, help="owner-supplied AutoMatador.mp3 (never committed)")
    parser.add_argument("--preview", type=Path, required=True, help="local WAV/report directory")
    parser.add_argument("--output", type=Path, default=OUTPUT_DIR)
    args = parser.parse_args()
    if sha256(args.source) != SOURCE_SHA256:
        parser.error("source MP3 does not match the approved AutoMatador hash")
    song, rate = sf.read(str(args.source), always_2d=True, dtype="float64")
    if rate != RATE or song.shape[1] != 2:
        parser.error("expected the 48 kHz stereo release")
    song *= 10 ** (GAIN_DB / 20)
    args.output.mkdir(parents=True, exist_ok=True)
    args.preview.mkdir(parents=True, exist_ok=True)
    report = {"recipe": "tools/" + Path(__file__).name, "recipe_sha256": sha256(Path(__file__)),
              "source_sha256": SOURCE_SHA256, "tempo_bpm": 125.0, "bar0_s": DOWNBEAT,
              "libraries": {"numpy": np.__version__, "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "cues": {}}
    for name, (first, loop_start, loop_end, lead_bars) in CUES.items():
        cue, loop = cut(song, first, loop_start, loop_end)
        data = encode(name, cue, loop)
        target = args.output / f"{name}.ogg"
        target.write_bytes(data)
        decoded, decoded_rate = sf.read(str(target), always_2d=True, dtype="float64")
        tags = dict(read_vorbis_tags(data))
        if decoded_rate != RATE or abs(len(decoded) - len(cue)) > 0:
            raise RuntimeError(f"{name}: decoded length {len(decoded)} != {len(cue)}")
        peak = float(np.max(np.abs(decoded)))
        entry = {"ogg": target.relative_to(ROOT).as_posix() if target.is_relative_to(ROOT) else str(target),
                 "ogg_sha256": sha256(target), "seconds": round(len(decoded) / RATE, 4),
                 "source_bars": [first, loop_end], "lead_bars": lead_bars,
                 "lead_ticks": round(lead_bars * BAR * 60, 3), "peak_dbfs": round(20 * math.log10(peak), 2),
                 "tags": tags}
        if loop is not None:
            entry |= {"loop_samples": list(loop), "loop_bars": [loop_start, loop_end],
                      "loop_seconds": round((loop[1] - loop[0]) / RATE, 4)}
            # what a player hears at the jump: 4 s before LOOPEND then 4 s after LOOPSTART
            span = 4 * RATE
            seam = np.concatenate([decoded[loop[1] - span:loop[1]], decoded[loop[0]:loop[0] + span]])
            sf.write(str(args.preview / f"{name}-seam.wav"), seam.astype(np.float32), RATE, subtype="PCM_16")
        report["cues"][name] = entry
    if sha256(args.source) != SOURCE_SHA256:
        raise RuntimeError("Source changed during render")
    (args.preview / "ebon-music-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["cues"], indent=2))


if __name__ == "__main__":
    main()
