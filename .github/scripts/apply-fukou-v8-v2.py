from __future__ import annotations

import hashlib
import json
import pathlib
import re
import shutil
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
MUSIC = ROOT / "Assets" / "Music"
EXPECTED = {
    "ObsidianLiturgy.ogg": "39b6d164f46e514078cae9dcefa887ae6359dd7d4c6380129f64404517c191c2",
    "UnboundLiturgy.ogg": "fac5a5b6b580ddff0615c3c302377b96ba19759904ce5ec561d8e417c9e964f8",
    "DistantLiturgy.ogg": "a19fe1f848a777e825b2bc7701aa78858e3e585183dae89656e767826db162ec",
    "TerminalLiturgy.ogg": "f18b080345073b765b614ac9310f18a78fd255ff683223de7d318d6bfacbc94d",
}


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(*args: str) -> None:
    subprocess.run(args, cwd=ROOT, check=True)


def render(src: str, dst: pathlib.Path, filters: str) -> None:
    run(
        "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
        "-i", str(MUSIC / src), "-map_metadata", "-1", "-af", filters,
        "-ar", "48000", "-ac", "2", "-c:a", "libvorbis", "-b:a", "224k",
        str(dst),
    )


def probe(path: pathlib.Path) -> dict[str, object]:
    info = json.loads(subprocess.check_output([
        "ffprobe", "-v", "error", "-show_entries", "stream=sample_rate,channels",
        "-show_entries", "format=duration", "-of", "json", str(path)
    ], cwd=ROOT, text=True))
    result = subprocess.run([
        "ffmpeg", "-hide_banner", "-nostats", "-i", str(path),
        "-filter_complex", "ebur128=peak=true", "-f", "null", "-"
    ], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True, check=True)
    summary = result.stderr.rsplit("Summary:", 1)[-1]
    integrated = re.search(r"I:\s*(-?\d+(?:\.\d+)?) LUFS", summary)
    peak = re.search(r"Peak:\s*(-?\d+(?:\.\d+)?) dBFS", summary)
    if not integrated or not peak:
        raise RuntimeError(f"failed to parse ebur128 output for {path}")
    stream = info["streams"][0]
    return {
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
        "sample_rate": int(stream["sample_rate"]),
        "channels": int(stream["channels"]),
        "duration_seconds": round(float(info["format"]["duration"]), 6),
        "lufs_i": float(integrated.group(1)),
        "true_peak_dbfs": float(peak.group(1)),
    }


def patch_music_selector() -> None:
    path = ROOT / "Client/Encounters/FirstSeverance/FirstSeverancePrototypePresentation.cs"
    text = path.read_text(encoding="utf-8")
    old = '''    public override int Music => Main.dedServ ? -1 : MusicLoader.GetMusicSlot(Mod,
        ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat?.BossPhase switch
        {
            FirstSeveranceBossPhase.Final => "Assets/Music/TerminalLiturgy",
            FirstSeveranceBossPhase.Distant => "Assets/Music/DistantLiturgy",
            FirstSeveranceBossPhase.Unbound => "Assets/Music/UnboundLiturgy",
            _ => "Assets/Music/ObsidianLiturgy",
        });'''
    new = '''    private static FirstSeveranceBossPhase MusicPhase(FirstSeveranceCombatProjection? combat)
        => combat is { Substate: FirstSeveranceSubstate.PhaseTransition }
            ? combat.BossPhase switch
            {
                FirstSeveranceBossPhase.Final => FirstSeveranceBossPhase.Distant,
                FirstSeveranceBossPhase.Distant => FirstSeveranceBossPhase.Unbound,
                FirstSeveranceBossPhase.Unbound => FirstSeveranceBossPhase.Sealed,
                _ => combat.BossPhase,
            }
            : combat?.BossPhase ?? FirstSeveranceBossPhase.Sealed;

    public override int Music => Main.dedServ ? -1 : MusicLoader.GetMusicSlot(Mod,
        MusicPhase(ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat) switch
        {
            FirstSeveranceBossPhase.Final => "Assets/Music/TerminalLiturgy",
            FirstSeveranceBossPhase.Distant => "Assets/Music/DistantLiturgy",
            FirstSeveranceBossPhase.Unbound => "Assets/Music/UnboundLiturgy",
            _ => "Assets/Music/ObsidianLiturgy",
        });'''
    if old not in text:
        raise RuntimeError("music selector anchor not found")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


def replace_section(text: str, start_heading: str, next_heading: str, replacement: str) -> str:
    start = text.find(start_heading)
    end = text.find(next_heading, start + len(start_heading))
    if start < 0 or end < 0:
        raise RuntimeError(f"section anchors missing: {start_heading!r}, {next_heading!r}")
    return text[:start] + replacement.rstrip() + "\n\n" + text[end:]


def patch_docs(metrics: dict[str, dict[str, object]]) -> None:
    build = ROOT / "build.txt"
    text = build.read_text(encoding="utf-8")
    if "version = 0.2.46" not in text:
        raise RuntimeError("build version anchor not found")
    build.write_text(text.replace("version = 0.2.46", "version = 0.2.47", 1), encoding="utf-8")

    audio = ROOT / "docs/AUDIO_CUE_SHEET.md"
    text = audio.read_text(encoding="utf-8")
    current_bgm = '''## Current BGM

All four phase slots use owner-approved edits of **EigHt — 不幸な人形劇** as one recognizable identity. Phase I keeps the full reduced/lower/slower form; Phase II / III / Final now contain only selected high-energy sections, so an ordinary whole-file loop cannot return to the removed slow opening.

| Runtime slot | Active treatment |
|---|---|
| `Assets/Music/ObsidianLiturgy.ogg` | Phase I: full 168-BPM/-2-semitone reduced arrangement; +6.25 dB master lift; about -14.0 LUFS-I |
| `Assets/Music/UnboundLiturgy.ogg` | Phase II: 194-BPM/-1-semitone processed-master section 40.873–94.068s; +0.75 dB; 20 ms anti-click edges; about -12.9 LUFS-I |
| `Assets/Music/DistantLiturgy.ogg` | Phase III: full-identity 218-BPM section 92.523–138.485s; 20 ms anti-click edges; about -10.5 LUFS-I |
| `Assets/Music/TerminalLiturgy.ogg` | Final: 222-BPM/+1-semitone section 145.721–170.849s; -0.60 dB encode headroom; 20 ms anti-click edges; about -9.8 LUFS-I |

During `PhaseTransition`, music selection intentionally stays on the **previous phase slot** for the complete transformation (P1→P2 6.0s, P2→P3 5.0s, P3→Final 4.0s). The new phase file starts only when the transition resolves into the first attack. BossPhase itself still advances at transition start, so transformation visuals/SFX, HP gates, authority timing and network state are unchanged.

The master balance is raised on the music side rather than reducing warning SFX or changing user sliders. The resulting integrated-loudness ramp is approximately **-14.0 / -12.9 / -10.5 / -9.8 LUFS-I**, preserving phase escalation while keeping early-phase BGM materially present against raid cues. Exact hashes, durations, loudness and true peaks are in [0.2.47 evidence](evidence/2026-09-11-fukou-section-loops-mix.json); creator/source/terms remain owned by [Attribution](../Assets/ATTRIBUTION.md).

**Preparation is silent.** [PreparationSilence](../Client/Encounters/FirstSeverance/FirstSeverancePreparationSilence.cs) selects silence only while the local member has accepted preparation without combat. Combat start selects phase music; cancel/lost preparation releases the scene to ordinary music. No user music/SFX slider, world state or input flag changes.

Music never drives authority timers. Tracks are not sample-accurate seek-synchronized for joining peers. Numeric decode/peak/LUFS checks do not prove in-game loop/transition/mix acceptance.'''
    text = replace_section(text, "## Current BGM", "## Accepted SFX selection", current_bgm)
    audio.write_text(text, encoding="utf-8")

    status = ROOT / "docs/STATUS.md"
    text = status.read_text(encoding="utf-8")
    if "Development **0.2.46 / protocol 28**." not in text:
        raise RuntimeError("status version anchor missing")
    text = text.replace("Development **0.2.46 / protocol 28**.", "Development **0.2.47 / protocol 28**.", 1)
    text, count = re.subn(
        r"^- Latest change:.*$",
        "- Latest change: phase transitions keep the previous BGM through the full transformation, then start P2/P3/Final at section-only high-energy loops; P1/P2 mastering is raised so BGM stays audible against raid SFX. [Audio sheet](AUDIO_CUE_SHEET.md) owns the active cuts/mix; gameplay timing, SFX selection and silent preparation are unchanged.",
        text, count=1, flags=re.MULTILINE,
    )
    if count != 1:
        raise RuntimeError("status latest-change anchor missing")

    verification_heading = "## Verification state\n\n"
    start = text.find(verification_heading)
    if start < 0:
        raise RuntimeError("verification heading missing")
    para_start = start + len(verification_heading)
    para_end = text.find("\n\n", para_start)
    if para_end < 0:
        raise RuntimeError("verification paragraph end missing")
    verification = (
        "Latest native package remains the 0.2.46 package from source commit **9232caa** (0 errors, 4 existing CS8632 warnings). "
        "For **0.2.47**, the one-shot integration verifies all four new OGGs as finite 48 kHz stereo with bounded true peak and the intended rising LUFS-I curve, then runs repository tests, catalog refresh and `git diff --check` before push. "
        "The 0.2.47 native tModLoader package and in-game phase handoff/loop/mix audition remain `not_run` / user-owned. [0.2.47 evidence](evidence/2026-09-11-fukou-section-loops-mix.json) owns exact hashes and measurements."
    )
    text = text[:para_start] + verification + text[para_end:]

    text = text.replace(
        "- Latest audio: silent preparation → phase music, cancellation restoring ordinary music, phase/loop/mix audition.",
        "- Latest audio: silent preparation → P1, old BGM held through each transformation, new high-energy section starting with the first attack, section-loop seam, and BGM/SFX balance audition.",
        1,
    )
    next_heading = "## Next change\n\n"
    nstart = text.find(next_heading)
    if nstart < 0:
        raise RuntimeError("next-change heading missing")
    npara_start = nstart + len(next_heading)
    npara_end = text.find("\n\n", npara_start)
    if npara_end < 0:
        raise RuntimeError("next-change paragraph end missing")
    next_para = (
        "After obtaining a matching 0.2.47 client build, reload/restart and audition only the affected audio surfaces: confirm each transformation retains the old track until completion, the next phase enters directly on its selected high-energy section, loops never reintroduce the removed opening, and BGM remains continuously perceptible beneath attack SFX at the unchanged user sliders."
    )
    text = text[:npara_start] + next_para + text[npara_end:]
    status.write_text(text, encoding="utf-8")

    attr = ROOT / "Assets/ATTRIBUTION.md"
    text = attr.read_text(encoding="utf-8")
    header = "## EigHt `不幸な人形劇` section-loop / mix revision — 0.2.47"
    if header not in text:
        labels = [("P1", "ObsidianLiturgy.ogg"), ("P2", "UnboundLiturgy.ogg"), ("P3", "DistantLiturgy.ogg"), ("Final", "TerminalLiturgy.ogg")]
        rows = "\n".join(
            f"| `Assets/Music/{filename}` | {metrics[key]['duration_seconds']:.3f}s | {metrics[key]['lufs_i']:.1f} LUFS-I | {metrics[key]['true_peak_dbfs']:.1f} dBTP | `{metrics[key]['sha256']}` |"
            for key, filename in labels
        )
        block = f'''\n\n{header}

- Creator/composer, work, source, governing terms and redistribution conditions remain exactly the **0.2.46 EigHt `不幸な人形劇`** record; this revision introduces no new third-party recording or composition.
- Edit basis: the four already-approved 0.2.46 runtime phase masters. P1 remains full-form and receives +6.25dB static gain. P2 is cropped to processed-master 40.873–94.068s and receives +0.75dB. P3 is cropped to 92.523–138.485s. Final is cropped to 145.721–170.849s and reduced 0.60dB for encode headroom. P2/P3/Final receive 20ms entry/exit anti-click fades.
- Playback change: during the 6.0s/5.0s/4.0s `PhaseTransition` windows the previous phase music slot is retained; the next file starts only after transformation resolves. This changes no composition, SFX source, user slider, gameplay clock or network state.
- Loop rule: P2/P3/Final contain only their selected section, therefore an ordinary whole-file loop cannot reintroduce the discarded opening material.

| Runtime asset | Duration | Loudness | True peak | SHA-256 |
|---|---:|---:|---:|---|
{rows}
'''
        attr.write_text(text.rstrip() + block + "\n", encoding="utf-8")


def write_evidence(metrics: dict[str, dict[str, object]]) -> None:
    evidence = {
        "date": "2026-09-11",
        "version": "0.2.47",
        "protocol": 28,
        "scope": "First Severance phase-music handoff, V8 section loops and BGM loudness; gameplay/network/SFX unchanged",
        "source": {
            "basis": "0.2.46 approved EigHt 不幸な人形劇 runtime phase masters already committed to the repository",
            "prior_hashes": {"P1": EXPECTED["ObsidianLiturgy.ogg"], "P2": EXPECTED["UnboundLiturgy.ogg"], "P3": EXPECTED["DistantLiturgy.ogg"], "Final": EXPECTED["TerminalLiturgy.ogg"]},
            "third_party_rights": "unchanged from Assets/ATTRIBUTION.md 0.2.46 EigHt record; no new source recording introduced",
        },
        "edit": {
            "transition_music": "During PhaseTransition, music selection resolves to the previous BossPhase; the new phase track begins when the transition substate ends.",
            "P1": "full 0.2.46 phase master, +6.25 dB static gain",
            "P2": "processed-master 40.873s..94.068s, +0.75 dB, 20 ms entry/exit fades",
            "P3": "processed-master 92.523s..138.485s, 20 ms entry/exit fades",
            "Final": "processed-master 145.721s..170.849s, -0.60 dB for encode headroom, 20 ms entry/exit fades",
            "loop_policy": "P2/P3/Final files contain only the selected high-energy section; deleted earlier material cannot reappear on an ordinary whole-file loop.",
        },
        "runtime_assets": metrics,
        "verification": {
            "audio": "passed: all four OGGs decode as 48 kHz stereo, bounded true peak, monotonic integrated loudness from P1 through Final",
            "repository_static": "pending workflow verifier step",
            "git_diff_check": "pending workflow verifier step",
            "native_mod_build": "not_run: latest native package remains 0.2.46; 0.2.47 changes presentation C# and audio assets",
            "runtime": "not_run: user-owned transition/loop/mix listening",
        },
    }
    path = ROOT / "docs/evidence/2026-09-11-fukou-section-loops-mix.json"
    path.write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    build = (ROOT / "build.txt").read_text(encoding="utf-8")
    if "version = 0.2.46" not in build:
        raise RuntimeError("expected 0.2.46 base")
    for filename, expected in EXPECTED.items():
        actual = sha256(MUSIC / filename)
        if actual != expected:
            raise RuntimeError(f"base hash mismatch for {filename}: {actual}")

    temp = pathlib.Path("/tmp/fukou-v8-v2")
    if temp.exists():
        shutil.rmtree(temp)
    temp.mkdir(parents=True)
    render("ObsidianLiturgy.ogg", temp / "ObsidianLiturgy.ogg", "volume=6.25dB")
    render("UnboundLiturgy.ogg", temp / "UnboundLiturgy.ogg", "atrim=start=40.873:end=94.068,asetpts=PTS-STARTPTS,volume=0.75dB,afade=t=in:st=0:d=0.020,afade=t=out:st=53.175:d=0.020")
    render("DistantLiturgy.ogg", temp / "DistantLiturgy.ogg", "atrim=start=92.523:end=138.485,asetpts=PTS-STARTPTS,afade=t=in:st=0:d=0.020,afade=t=out:st=45.942:d=0.020")
    render("TerminalLiturgy.ogg", temp / "TerminalLiturgy.ogg", "atrim=start=145.721:end=170.849,asetpts=PTS-STARTPTS,volume=-0.60dB,afade=t=in:st=0:d=0.020,afade=t=out:st=25.108:d=0.020")
    for path in temp.glob("*.ogg"):
        (MUSIC / path.name).write_bytes(path.read_bytes())

    files = {"P1": MUSIC / "ObsidianLiturgy.ogg", "P2": MUSIC / "UnboundLiturgy.ogg", "P3": MUSIC / "DistantLiturgy.ogg", "Final": MUSIC / "TerminalLiturgy.ogg"}
    metrics = {key: probe(path) for key, path in files.items()}
    for name, metric in metrics.items():
        assert metric["sample_rate"] == 48000 and metric["channels"] == 2, (name, metric)
        assert metric["true_peak_dbfs"] <= -0.2, (name, metric)
    assert -14.8 <= metrics["P1"]["lufs_i"] <= -13.2, metrics["P1"]
    assert -13.7 <= metrics["P2"]["lufs_i"] <= -12.2, metrics["P2"]
    assert -11.3 <= metrics["P3"]["lufs_i"] <= -9.7, metrics["P3"]
    assert -10.7 <= metrics["Final"]["lufs_i"] <= -8.8, metrics["Final"]
    assert metrics["P1"]["lufs_i"] < metrics["P2"]["lufs_i"] < metrics["P3"]["lufs_i"] < metrics["Final"]["lufs_i"]

    patch_music_selector()
    patch_docs(metrics)
    write_evidence(metrics)


if __name__ == "__main__":
    main()
