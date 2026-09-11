from __future__ import annotations

import hashlib
import json
import pathlib
import re
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


def patch_docs(metrics: dict[str, dict[str, object]]) -> None:
    build = ROOT / "build.txt"
    text = build.read_text(encoding="utf-8")
    if "version = 0.2.46" not in text:
        raise RuntimeError("build version anchor not found")
    build.write_text(text.replace("version = 0.2.46", "version = 0.2.47", 1), encoding="utf-8")

    audio = ROOT / "docs/AUDIO_CUE_SHEET.md"
    text = audio.read_text(encoding="utf-8")
    section = '''## Section-loop handoff and stronger BGM mix — 0.2.47

Phase transitions no longer request the next phase track as soon as the BossPhase value advances. While the authoritative combat substate is `PhaseTransition`, the scene effect keeps requesting the previous phase music slot, so the existing track continues through the 6.0s / 5.0s / 4.0s transformation. The new phase track begins only when the transition substate resolves into the first attack. This is presentation-only: BossPhase, transformation SFX/visuals, HP gates, authority timing and protocol remain unchanged.

P2/P3/Final now use only their selected high-energy source sections, so a normal whole-file loop cannot return to the deleted slow opening. P2 uses the processed-master 40.873–94.068s region, P3 92.523–138.485s, and Final 145.721–170.849s; each receives a 20ms edge fade for an encoded anti-click boundary. P1 remains the full phase form.

The BGM master balance is raised rather than reducing warning SFX or mutating user sliders. P1 receives +6.25dB and P2 +0.75dB; P3 keeps its accepted level and Final is trimmed 0.60dB only for encode headroom. Integrated loudness is approximately **-14.0 / -12.9 / -10.5 / -9.8 LUFS-I** from P1 through Final, preserving phase escalation while keeping the lower phases materially more audible against the positionless raid cues. Exact hashes and decode measurements are in [0.2.47 evidence](evidence/2026-09-11-fukou-section-loops-mix.json). In-game perceived balance and transition timing remain user-owned listening checks.

'''
    if "## Section-loop handoff and stronger BGM mix — 0.2.47" not in text:
        anchor = "## EigHt phase-progression BGM — 0.2.46"
        if anchor not in text:
            raise RuntimeError("audio cue insertion anchor missing")
        text = text.replace(anchor, section + anchor, 1)
        audio.write_text(text, encoding="utf-8")

    status = ROOT / "docs/STATUS.md"
    text = status.read_text(encoding="utf-8")
    new_current = (
        "Development **0.2.47**, protocol **28**. "
        "[First Severance phase BGM](AUDIO_CUE_SHEET.md#section-loop-handoff-and-stronger-bgm-mix--0247) "
        "now holds the previous track through each PhaseTransition, then starts the next phase at its selected high-energy section "
        "when the first attack begins. P2/P3/Final are section-only loops that cannot return to the removed slow opening; "
        "P1/P2 mastering is raised so BGM remains present against raid SFX while preserving a P1 -> P2 -> P3 -> Final loudness ramp. "
        "Gameplay authority, phase timing, SFX, protocol and preparation silence are unchanged."
    )
    text, count = re.subn(r"Development \*\*0\.2\.46\*\*, protocol \*\*28\*\*\.[^\n]*", new_current, text, count=1)
    if count != 1:
        raise RuntimeError("status current-build anchor missing")
    evidence_line = (
        "[0.2.47 BGM handoff/mix checks](evidence/2026-09-11-fukou-section-loops-mix.json) own the V8 section ranges, "
        "transition-slot rule, four-master hashes, 48kHz/stereo/duration/LUFS/true-peak checks and repository verification. "
        "C# changes are presentation-only; native package and in-game transition/loop/mix listening remain user-owned `not_run`.\n\n"
    )
    if evidence_line not in text:
        marker = "## Verification state\n\n"
        if marker not in text:
            raise RuntimeError("status verification anchor missing")
        text = text.replace(marker, marker + evidence_line, 1)
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

- Creator/composer, work, source, governing terms and redistribution conditions remain exactly the **0.2.46 EigHt `不幸な人形劇`** record above; this revision introduces no new third-party recording or composition.
- Edit basis: the four already-approved 0.2.46 runtime phase masters. P1 remains full-form and receives +6.25dB static gain. P2 is cropped to processed-master 40.873–94.068s and receives +0.75dB. P3 is cropped to 92.523–138.485s. Final is cropped to 145.721–170.849s and reduced 0.60dB for encode headroom. P2/P3/Final receive 20ms entry/exit anti-click fades.
- Playback change: during the 6.0s/5.0s/4.0s `PhaseTransition` windows the previous phase music slot is retained; the next file starts only after the transformation resolves. This changes no composition, SFX source, user slider, gameplay clock or network state.
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
            "transition_music": "During PhaseTransition, music selection resolves to the previous BossPhase. The new phase track begins only when the transition substate ends; BossPhase/gameplay/visual/SFX state remains unchanged.",
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
            "native_mod_build": "not_run in Linux one-shot workflow; C# change is presentation-only and requires user in-game confirmation",
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

    temp = pathlib.Path("/tmp/fukou-v8-fixed")
    temp.mkdir(parents=True, exist_ok=True)
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
