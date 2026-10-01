"""Cut Ebon Manor's phase cues from EigHt's "AutoMatador" with long, natural loops.

The owner supplied the official free-BGM MP3 (BOOTH item 6178144). EigHt's terms
(https://eight-novel.fanbox.cc/posts/7647818, updated 2026-07-14) permit game
background use and editing, not standalone music distribution, Content ID or
streaming registration. Assets/ATTRIBUTION.md records credit and limits.

The track is a steady 125.000 BPM. Its first audible beat (0.235 s) is a
two-beat pickup: every section entry (the A drop, B, A' and the climax) lands
on the grid whose bar 0 starts at 1.195 s, so bar k starts at 1.195 + 1.92 k
seconds. Each act keeps its entry (Act One plays the whole intro from the
pickup; Act II and the Finale start on their bar lines), so the fight's beat
grid is unchanged.

Loops (owner, 2026-10-02: long loops that keep the song's development, natural
joins). Every jump follows the song's own repetition: the bars after LOOPEND
resemble the bars after LOOPSTART and the bars before them match too (measured
beat-synchronous chroma/timbre/energy similarity, see LOOPS). The join is a
one-beat crossfade that lands on LOOPSTART's downbeat; its gains are
correlation-compensated so the overlap keeps constant power, and the incoming
window is aligned to the outgoing one by cross-correlation. When an act enters
after its loop start, the file holds the first pass and then the loop (the first
pass ends with the same join). One bar of the continuation is written after
LOOPEND so the Vorbis frames around the jump stay continuous (never played:
tModLoader wraps at LOOPEND). Headroom -1.5 dB.
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
BEAT = BAR / 4
GAIN_DB = -1.5
JOIN = BEAT          # crossfade length, ending on the loop start's downbeat
ALIGN = 0.008        # cross-correlation search for the incoming window, seconds

# name: (first bar, loop start bar, loop end bar or None, bars before the first attack downbeat)
CUES = {
    "ActOne": (-0.5, 14, 54, 14.5),  # pickup + whole intro -> A drop; A' at bar 54 returns to A (40 bars)
    "ActTwo": (36, 32, 61, 2),       # build -> B at bar 38; the gap before the climax returns to the breakdown (29 bars)
    "Finale": (51, 16, 67, 3),       # break -> A' at bar 54 -> climax; its second phrase returns to A (51 bars)
    "Curtain": (71, None, None, 0),  # outro after Victory, no loop tags
}


def bar_sample(bar):
    return round((DOWNBEAT + BAR * bar) * RATE)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def raised_cosine(count, rising=True):
    ramp = 0.5 - 0.5 * np.cos(np.linspace(0, math.pi, count))
    return ramp if rising else ramp[::-1]


def aligned_start(song, outgoing_end, incoming_end, length):
    """Shift the incoming window (ending at incoming_end) to best match the outgoing one."""
    reach = round(ALIGN * RATE)
    a = song[outgoing_end - length:outgoing_end].mean(1)
    best, shift = -2.0, 0
    for lag in range(-reach, reach + 1, 4):
        b = song[incoming_end - length + lag:incoming_end + lag].mean(1)
        c = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12))
        if c > best:
            best, shift = c, lag
    for lag in range(shift - 3, shift + 4):
        b = song[incoming_end - length + lag:incoming_end + lag].mean(1)
        c = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12))
        if c > best:
            best, shift = c, lag
    return shift, best


def join(song, block, resume, rho):
    """Crossfade the block's last beat into the audio leading to `resume` (constant power for correlation rho)."""
    length = round(JOIN * RATE)
    incoming = song[resume - length:resume]
    theta = np.linspace(0, math.pi / 2, length)[:, None]
    gain = 1 / np.sqrt(1 + max(rho, 0.0) * np.sin(2 * theta))
    block[-length:] = (block[-length:] * np.cos(theta) + incoming * np.sin(theta)) * gain


def cut(song, first, loop_start, loop_end):
    begin = bar_sample(first)
    if loop_end is None:
        cue = song[begin:].copy()
        fade_out = round(0.01 * RATE)
        cue[-fade_out:] *= raised_cosine(fade_out, rising=False)[:, None]
        cue[:round(0.015 * RATE)] *= raised_cosine(round(0.015 * RATE))[:, None]
        return cue, None, None
    end = bar_sample(loop_end)
    # The loop resumes where the incoming material lines up with the outgoing beat (a few samples off the
    # bar line at most), so the jump is continuous; the loop is then that many samples shorter than N bars.
    shift, rho = aligned_start(song, end, bar_sample(loop_start), round(JOIN * RATE))
    resume = bar_sample(loop_start) + shift
    if first <= loop_start:
        cue = song[begin:end].copy()
        join(song, cue, resume, rho)
        loop = (resume - begin, len(cue))
    else:
        # first pass, then the loop; the first pass ends with the same join into the loop
        entry = song[begin:end].copy()
        join(song, entry, resume, rho)
        body = song[resume:end].copy()
        join(song, body, resume, rho)
        cue = np.concatenate([entry, body])
        loop = (len(entry), len(cue))
    cue[:round(0.015 * RATE)] *= raised_cosine(round(0.015 * RATE))[:, None]
    # never played (the player wraps at LOOPEND): keeps the encoded frames continuous across the jump
    guard = song[resume:resume + round(BAR * RATE)]
    seam = {"align_samples": shift, "correlation": round(rho, 3), "grid_drift_samples_per_loop": -shift}
    return np.concatenate([cue, guard]), loop, seam


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
              "source_sha256": SOURCE_SHA256, "tempo_bpm": 125.0, "bar0_s": DOWNBEAT, "join_s": JOIN,
              "libraries": {"numpy": np.__version__, "soundfile": sf.__version__, "libsndfile": sf.__libsndfile_version__},
              "cues": {}}
    for name, (first, loop_start, loop_end, lead_bars) in CUES.items():
        cue, loop, seam = cut(song, first, loop_start, loop_end)
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
                      "loop_seconds": round((loop[1] - loop[0]) / RATE, 4),
                      "join": seam}
            # what a player hears at the jump: 6 s before LOOPEND then 6 s after LOOPSTART (the file as played)
            span = 6 * RATE
            heard = np.concatenate([decoded[loop[1] - span:loop[1]], decoded[loop[0]:loop[0] + span]])
            sf.write(str(args.preview / f"{name}-seam.wav"), heard.astype(np.float32), RATE, subtype="PCM_16")
            if loop[0] > 0 and first > loop_start:
                pass_join = np.asarray(decoded[loop[0] - span:loop[0] + span])
                sf.write(str(args.preview / f"{name}-first-pass.wav"), pass_join.astype(np.float32), RATE, subtype="PCM_16")
        report["cues"][name] = entry
    if sha256(args.source) != SOURCE_SHA256:
        raise RuntimeError("Source changed during render")
    (args.preview / "ebon-music-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["cues"], indent=2))


if __name__ == "__main__":
    main()
