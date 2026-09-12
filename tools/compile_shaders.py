#!/usr/bin/env python3
"""Compile original Luminance materials; ship .fxc, never require player-side FXC."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SHADERS = ROOT / "Assets/AutoloadedEffects/Shaders"
MANIFEST = SHADERS / "compiled.json"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify() -> None:
    records = json.loads(MANIFEST.read_text(encoding="utf-8"))["files"]
    sources = sorted(SHADERS.glob("*.fx"))
    if set(records) != {p.name for p in sources}:
        raise ValueError("Shader export inventory is stale; run tools/compile_shaders.py --fxc <fxc.exe>.")
    for source in sources:
        record = records[source.name]
        if digest(source) != record["source_sha256"] or digest(source.with_suffix(".fxc")) != record["output_sha256"]:
            raise ValueError(f"Stale/missing shader export: {source.name}; compile it before packaging.")


def compile_all(compiler: Path) -> None:
    files = {}
    for source in sorted(SHADERS.glob("*.fx")):
        target = source.with_suffix(".fxc")
        # FXC writes the runtime export; this is asset compilation, not source editing.
        subprocess.run([str(compiler.resolve()), "/T", "fx_2_0", "/O3", "/Fo", str(target), str(source)], check=True)
        files[source.name] = {"source_sha256": digest(source), "output_sha256": digest(target)}
    MANIFEST.write_text(json.dumps({"compiler_sha256": digest(compiler), "flags": ["/T", "fx_2_0", "/O3"],
                                   "files": files}, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fxc", type=Path, help="explicit locally installed FXC; omit for read-only export validation")
    args = parser.parse_args()
    if args.fxc:
        compile_all(args.fxc)
    verify()
    print("Shader source/export hashes verified.")
