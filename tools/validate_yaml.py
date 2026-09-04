#!/usr/bin/env python3
"""Parse and minimally validate repository-owned YAML files."""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
    from yaml.constructor import ConstructorError
except ImportError:
    print("PyYAML is required; install tools/requirements-ci.txt", file=sys.stderr)
    raise SystemExit(2)


ROOT = Path(__file__).resolve().parents[1]
GITHUB_ROOT = ROOT / ".github"
SKILLS_ROOT = ROOT / ".agents" / "skills"
DOCS_CATALOG_ROOT = ROOT / "docs" / "catalog"


class UniqueKeyLoader(yaml.SafeLoader):
    """Safe YAML loader that rejects duplicate mapping keys."""


def construct_unique_mapping(
    loader: UniqueKeyLoader,
    node: yaml.nodes.MappingNode,
    deep: bool = False,
) -> dict[object, object]:
    mapping: dict[object, object] = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            raise ConstructorError(
                "while constructing a mapping",
                node.start_mark,
                f"found duplicate key {key!r}",
                key_node.start_mark,
            )
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


UniqueKeyLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG,
    construct_unique_mapping,
)


def require_mapping(value: object, path: Path) -> dict[object, object]:
    if not isinstance(value, dict):
        raise ValueError(f"{path.relative_to(ROOT)} must contain a YAML mapping")
    return value


def validate(path: Path) -> None:
    with path.open(encoding="utf-8") as stream:
        document = require_mapping(yaml.load(stream, Loader=UniqueKeyLoader), path)

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
    elif relative.endswith("/agents/openai.yaml"):
        interface = document.get("interface")
        if not isinstance(interface, dict):
            raise ValueError(f"{relative} must define an interface mapping")
        for field in ("display_name", "short_description", "default_prompt"):
            if not isinstance(interface.get(field), str) or not interface[field].strip():
                raise ValueError(f"{relative} must define interface.{field}")

        skill_name = path.parent.parent.name
        if f"${skill_name}" not in interface["default_prompt"]:
            raise ValueError(f"{relative} default_prompt must explicitly invoke ${skill_name}")
    elif relative == "docs/catalog/schema.yml":
        if document.get("schema_version") != 1:
            raise ValueError(f"{relative} must use schema_version 1")
        for field in (
            "document_types",
            "statuses",
            "authority_topic_statuses",
            "required_fields",
            "allowed_fields",
        ):
            if not isinstance(document.get(field), list) or not document[field]:
                raise ValueError(f"{relative} must define non-empty {field}")
        exclusions = document.get("excluded_paths")
        if not isinstance(exclusions, dict) or not exclusions:
            raise ValueError(f"{relative} must define non-empty excluded_paths mapping")
        for excluded_path, reason in exclusions.items():
            if not isinstance(excluded_path, str) or not excluded_path.startswith("docs/"):
                raise ValueError(f"{relative} excluded_paths keys must be docs/ paths")
            if not isinstance(reason, str) or not reason.strip():
                raise ValueError(f"{relative} excluded_paths reasons must be non-empty strings")
    elif relative == "docs/catalog/documents.yml":
        if document.get("schema_version") != 1:
            raise ValueError(f"{relative} must use schema_version 1")
        if not isinstance(document.get("documents"), list) or not document["documents"]:
            raise ValueError(f"{relative} must define a non-empty documents list")
        if not isinstance(document.get("excluded_documents"), list):
            raise ValueError(f"{relative} must define excluded_documents")


def main() -> int:
    files = sorted(
        (
            *GITHUB_ROOT.rglob("*.yml"),
            *GITHUB_ROOT.rglob("*.yaml"),
            *SKILLS_ROOT.rglob("*.yml"),
            *SKILLS_ROOT.rglob("*.yaml"),
            *DOCS_CATALOG_ROOT.rglob("*.yml"),
            *DOCS_CATALOG_ROOT.rglob("*.yaml"),
        )
    )
    if not files:
        print("No repository YAML files found", file=sys.stderr)
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
