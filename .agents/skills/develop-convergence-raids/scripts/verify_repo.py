#!/usr/bin/env python3
"""Run Convergence's deterministic repository checks from any working directory."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


def run(command: list[str], root: Path) -> int:
    print("+", " ".join(command), flush=True)
    return subprocess.run(command, cwd=root, check=False).returncode


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repository", nargs="?", default=".")
    parser.add_argument(
        "--with-dotnet",
        action="store_true",
        help="also run the project build when pinned tModLoader targets are available",
    )
    parser.add_argument(
        "--audit-only",
        action="store_true",
        help="run checks that do not intentionally create bytecode or build output",
    )
    args = parser.parse_args()

    root = Path(args.repository).resolve()
    required = (
        root / "AGENTS.md",
        root / "ConvergenceMod.csproj",
        root / "tools" / "repository_checks.py",
        root / "tools" / "validate_yaml.py",
    )
    missing = [str(path) for path in required if not path.is_file()]
    if missing:
        print("Not a Convergence repository; missing:", file=sys.stderr)
        for path in missing:
            print(f"- {path}", file=sys.stderr)
        return 2

    if args.audit_only and args.with_dotnet:
        parser.error("--audit-only and --with-dotnet cannot be combined")

    commands = [
        [sys.executable, "tools/repository_checks.py"],
        [sys.executable, "tools/validate_yaml.py"],
    ]
    if not args.audit_only:
        commands.append(
            [sys.executable, "-m", "py_compile", "tools/repository_checks.py", "tools/validate_yaml.py"]
        )
    if args.with_dotnet:
        commands.append(["dotnet", "build", "ConvergenceMod.csproj"])

    for command in commands:
        try:
            status = run(command, root)
        except FileNotFoundError as exception:
            print(f"Missing required executable: {exception.filename}", file=sys.stderr)
            return 127
        if status != 0:
            return status

    if args.audit_only:
        print("Audit-only repository checks passed without intentional bytecode/build output.")
    elif not args.with_dotnet:
        print("Static repository checks passed. Real tModLoader build/load remains a separate gate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
