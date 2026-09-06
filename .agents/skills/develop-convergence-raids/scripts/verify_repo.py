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
        "--write-catalog",
        action="store_true",
        help="regenerate the documentation catalog before checking the final edit batch",
    )
    parser.add_argument(
        "--with-domain",
        action="store_true",
        help="also run the Terraria-independent domain harness in Release configuration",
    )
    parser.add_argument(
        "--with-dotnet",
        action="store_true",
        help="also run the project build when pinned tModLoader targets are available",
    )
    parser.add_argument(
        "--with-codec",
        action="store_true",
        help="check compiled packet bodies (PowerShell 7); use the Mod assembly if --with-dotnet, otherwise linked domain sources",
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
        root / "tools" / "docs_catalog.py",
        root / "tools" / "repository_checks.py",
        root / "tools" / "validate_yaml.py",
    )
    missing = [str(path) for path in required if not path.is_file()]
    if missing:
        print("Not a Convergence repository; missing:", file=sys.stderr)
        for path in missing:
            print(f"- {path}", file=sys.stderr)
        return 2

    if args.audit_only and (args.write_catalog or args.with_domain or args.with_dotnet or args.with_codec):
        parser.error("--audit-only cannot be combined with --write-catalog, --with-domain, --with-dotnet, or --with-codec")

    commands = []
    if args.write_catalog:
        commands.append([sys.executable, "-B", "tools/docs_catalog.py", "--write"])
    commands.extend([
        [sys.executable, "-B", "tools/docs_catalog.py", "--check"],
        [sys.executable, "-B", "tools/repository_checks.py"],
        [sys.executable, "-B", "tools/validate_yaml.py"],
    ])
    if args.with_domain:
        commands.append(
            [
                "dotnet", "run", "--project",
                "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj",
                "--configuration", "Release",
            ]
        )
    if args.with_dotnet:
        commands.append(["dotnet", "build", "ConvergenceMod.csproj"])
    if args.with_codec:
        if not args.with_dotnet and not args.with_domain:
            commands.append(["dotnet", "build", "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj", "--configuration", "Release"])
        assembly = ("bin/Debug/net8.0/Convergence.dll" if args.with_dotnet else
                    "Tests/Convergence.DomainTests/bin/Release/net8.0/Convergence.DomainTests.dll")
        commands.append(["pwsh", "-NoProfile", "-NonInteractive", "-File", "tools/check-codec.ps1", "-AssemblyPath", assembly])

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
    else:
        print("Selected automated checks passed. Runtime load/playtest evidence remains separate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
