#!/usr/bin/env python3
"""Local toolchain diagnosis and a source-identified tModLoader package build."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def output(*command: str) -> str:
    return subprocess.check_output(command, cwd=ROOT, text=True, encoding="utf-8").strip()


def source_record() -> dict:
    # Include untracked source and tracked deletions; exclude generated/local files using Git's rules.
    paths = output("git", "ls-files", "-z", "--cached", "--others", "--exclude-standard").split("\0")
    files = {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() if (ROOT / p).is_file() else None
             for p in sorted(set(paths)) if p}
    digest = hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest()
    return {"source": str(ROOT), "commit": output("git", "rev-parse", "HEAD"),
            "branch": output("git", "branch", "--show-current"),
            "dirty": output("git", "status", "--porcelain=v1"),
            "source_sha256": digest, "files": files}


def environment(properties: list[str]) -> dict:
    raw = output("dotnet", "msbuild", "ConvergenceMod.csproj", "-nologo",
                 "-getProperty:TModLoaderTargets,tMLSteamPath,TModLoaderSavePath,TargetPath,ConvergenceDevelopmentSolo,DefineConstants,ExtraBuildModFlags,TargetFramework,LangVersion",
                 *properties)
    result = json.loads(raw)["Properties"]
    targets = (ROOT / result["TModLoaderTargets"]).resolve()
    if not targets.is_file() or not (Path(result["tMLSteamPath"]) / "tModLoader.dll").is_file():
        raise ValueError("Configure Convergence.local.props or TML_PATH with an installed tModLoader; targets are missing.")
    if ROOT.name != "Convergence":
        raise ValueError("The actual build source directory must be named Convergence (including worktrees).")
    result["TModLoaderTargets"] = str(targets)
    result["targets_sha256"] = hashlib.sha256(targets.read_bytes()).hexdigest()
    result["tml_assembly_sha256"] = hashlib.sha256((Path(result["tMLSteamPath"]) / "tModLoader.dll").read_bytes()).hexdigest()
    result["dotnet_sdk"] = output("dotnet", "--version")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("doctor", "build"))
    parser.add_argument("--tml", help="explicit installed tModLoader directory; overrides local config")
    parser.add_argument("--save", help="explicit existing tModLoader user-data directory")
    parser.add_argument("--configuration", choices=("Debug", "Release"), default="Debug")
    parser.add_argument("--release-candidate", action="store_true", help="compile out solo debug admission; not a release approval")
    parser.add_argument("--native", action="store_true", help="use tModLoader's compiler to resolve installed modReferences without extracting permanent DLL references")
    args = parser.parse_args()
    properties = [f"-p:Configuration={args.configuration}"]
    for name, value in (("TModLoaderPath", args.tml), ("TModLoaderSavePath", args.save)):
        if value:
            properties.append(f"-p:{name}={Path(value).resolve()}")
    if args.release_candidate:
        properties.append("-p:ConvergenceDevelopmentSolo=false")
    try:
        env = environment(properties)
        source = source_record()
        print(json.dumps({k: v for k, v in source.items() if k != "files"} | {"environment": env}, indent=2), flush=True)
        if args.action == "doctor":
            return 0
        save = Path(env["TModLoaderSavePath"])
        if not env["TModLoaderSavePath"] or not (save / "Mods").is_dir():
            raise ValueError("Set an existing user-data directory with Mods via TModLoaderSavePath or --save; no empty profile is created.")
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
        record_dir = ROOT / ".local" / "builds" / stamp
        record_dir.mkdir(parents=True)
        package = save / "Mods" / "Convergence.tmod"
        # Preserve the previous package before tML's normal overwrite. No save/Mod-list changes.
        if package.is_file():
            import shutil
            shutil.copy2(package, record_dir / "Convergence.previous.tmod")
        command = ["dotnet", "build", "ConvergenceMod.csproj", *properties]
        build_cwd = ROOT
        if args.native:
            build_cwd = Path(env["tMLSteamPath"])
            command = ["dotnet", str(build_cwd / "tModLoader.dll"), "-server", "-build", str(ROOT),
                       "-define", ";".join(filter(None, env["DefineConstants"].split(";"))), "-tmlsavedirectory", str(save)]
        record = {"started_utc": stamp, "source": source, "environment": env, "command": command,
                  "runtime_checks": "not_run; user-owned"}
        with (record_dir / "build.log").open("w", encoding="utf-8") as log:
            process = subprocess.Popen(command, cwd=build_cwd, text=True, encoding="utf-8", errors="replace",
                                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                       env=os.environ | {"DOTNET_CLI_UI_LANGUAGE": "en"})
            for line in process.stdout:
                print(line, end="", flush=True)
                log.write(line)
            code = process.wait()
        after = source_record()
        record["exit_code"] = code
        record["source_changed_during_build"] = after["source_sha256"] != source["source_sha256"]
        if code == 0 and package.is_file():
            record["artifact"] = {"path": str(package), "bytes": package.stat().st_size,
                                  "sha256": hashlib.sha256(package.read_bytes()).hexdigest()}
        elif code == 0:
            code = 1
            record["error"] = "Build returned success without the expected package."
        if record["source_changed_during_build"]:
            code = 1
            record["error"] = "Source changed during build; do not attribute this artifact to the starting source."
        record["result_code"] = code
        (record_dir / "record.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
        print(f"Build record: {record_dir / 'record.json'}", flush=True)
        return code
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"Development setup failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
