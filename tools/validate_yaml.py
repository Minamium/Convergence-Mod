#!/usr/bin/env python3
"""Parse and minimally validate repository-owned GitHub YAML files."""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    print("PyYAML is required; install tools/requirements-ci.txt", file=sys.stderr)
    raise SystemExit(2)


ROOT = Path(__file__).resolve().parents[1]
GITHUB_ROOT = ROOT / ".github"


def require_mapping(value: object, path: Path) -> dict[object, object]:
    if not isinstance(value, dict):
        raise ValueError(f"{path.relative_to(ROOT)} must contain a YAML mapping")
    return value


def validate(path: Path) -> None:
    with path.open(encoding="utf-8") as stream:
        document = require_mapping(yaml.safe_load(stream), path)

    relative = path.relative_to(ROOT).as_posix()
    if relative.startswith(".github/workflows/"):
        jobs = document.get("jobs")
        if not isinstance(jobs, dict) or not jobs:
            raise ValueError(f"{relative} must define at least one job")
    elif relative.startswith(".github/ISSUE_TEMPLATE/") and path.name != "config.yml":
        if not isinstance(document.get("body"), list) or not document["body"]:
            raise ValueError(f"{relative} must define a non-empty issue form body")
        for field in ("name", "description"):
            if not isinstance(document.get(field), str) or not document[field].strip():
                raise ValueError(f"{relative} must define {field}")
    elif path.name == "dependabot.yml" and document.get("version") != 2:
        raise ValueError(".github/dependabot.yml must use version 2")


def main() -> int:
    files = sorted((*GITHUB_ROOT.rglob("*.yml"), *GITHUB_ROOT.rglob("*.yaml")))
    if not files:
        print("No GitHub YAML files found", file=sys.stderr)
        return 1

    try:
        for path in files:
            validate(path)
    except (OSError, ValueError, yaml.YAMLError) as exception:
        print(f"YAML validation failed: {exception}", file=sys.stderr)
        return 1

    print(f"YAML validation passed ({len(files)} files inspected).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
