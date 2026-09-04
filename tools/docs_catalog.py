#!/usr/bin/env python3
"""Validate indexed Markdown metadata and generate the documentation catalog."""

from __future__ import annotations

import argparse
import datetime as datetime_module
import hashlib
import re
import sys
from pathlib import Path, PurePosixPath

try:
    import yaml
except ImportError:
    print("PyYAML is required; install tools/requirements-ci.txt", file=sys.stderr)
    raise SystemExit(2)


ROOT = Path(__file__).resolve().parents[1]
DOCS_ROOT = ROOT / "docs"
SCHEMA_PATH = DOCS_ROOT / "catalog" / "schema.yml"
CATALOG_PATH = DOCS_ROOT / "catalog" / "documents.yml"
CATALOG_DOCS_ROOT = CATALOG_PATH.parent

DOC_ID_PATTERN = re.compile(
    r"^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$"
)
TOPIC_PATTERN = re.compile(r"^[a-z0-9]+(?:[._-][a-z0-9]+)*$")
H1_PATTERN = re.compile(r"^ {0,3}#(?!#)[ \t]+(.+?)[ \t]*$")
FENCE_OPEN_PATTERN = re.compile(r"^ {0,3}(`{3,}|~{3,}).*$")

SCHEMA_FIELDS = {
    "schema_version",
    "document_types",
    "statuses",
    "authority_topic_statuses",
    "required_fields",
    "allowed_fields",
    "excluded_paths",
}
REQUIRED_AUTHORITY_TOPIC_STATUSES = {"accepted", "provisional"}


class CatalogError(ValueError):
    """Raised when indexed documentation violates its declared schema."""


class StrictSafeLoader(yaml.SafeLoader):
    """Safe YAML loader that rejects duplicate mapping keys."""


def construct_unique_mapping(
    loader: StrictSafeLoader,
    node: yaml.MappingNode,
    deep: bool = False,
) -> dict[object, object]:
    loader.flatten_mapping(node)
    mapping: dict[object, object] = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        try:
            duplicate = key in mapping
        except TypeError as exception:
            raise yaml.constructor.ConstructorError(
                "while constructing a mapping",
                node.start_mark,
                "found an unhashable mapping key",
                key_node.start_mark,
            ) from exception
        if duplicate:
            raise yaml.constructor.ConstructorError(
                "while constructing a mapping",
                node.start_mark,
                f"found duplicate key {key!r}",
                key_node.start_mark,
            )
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


StrictSafeLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG,
    construct_unique_mapping,
)


def strict_yaml_load(content: str) -> object:
    return yaml.load(content, Loader=StrictSafeLoader)


def load_mapping(path: Path) -> dict[str, object]:
    try:
        value = strict_yaml_load(path.read_text(encoding="utf-8"))
    except (OSError, yaml.YAMLError) as exception:
        raise CatalogError(f"cannot read {path.relative_to(ROOT)}: {exception}") from exception

    if not isinstance(value, dict):
        raise CatalogError(f"{path.relative_to(ROOT)} must contain a YAML mapping")
    if not all(isinstance(key, str) for key in value):
        raise CatalogError(f"{path.relative_to(ROOT)} mapping keys must be strings")
    return value


def require_string_list(
    metadata: dict[str, object],
    field: str,
    relative_path: str,
    *,
    allow_empty: bool = True,
) -> list[str]:
    value = metadata.get(field)
    if not isinstance(value, list) or (not allow_empty and not value):
        qualifier = "non-empty " if not allow_empty else ""
        raise CatalogError(f"{relative_path} front matter {field} must be a {qualifier}list")

    result: list[str] = []
    for item in value:
        if not isinstance(item, str) or not item.strip():
            raise CatalogError(f"{relative_path} front matter {field} must contain strings")
        result.append(item.strip())
    return result


def require_schema_string_list(
    schema: dict[str, object],
    field: str,
    *,
    allow_empty: bool = False,
) -> list[str]:
    value = schema.get(field)
    if not isinstance(value, list) or (not allow_empty and not value):
        qualifier = "non-empty " if not allow_empty else ""
        raise CatalogError(f"docs/catalog/schema.yml {field} must be a {qualifier}string list")
    if not all(isinstance(item, str) and item.strip() for item in value):
        raise CatalogError(f"docs/catalog/schema.yml {field} must contain non-empty strings")

    result = [item.strip() for item in value]
    if len(result) != len(set(result)):
        raise CatalogError(f"docs/catalog/schema.yml {field} contains duplicate values")
    return result


def load_excluded_paths(schema: dict[str, object]) -> dict[str, str]:
    value = schema.get("excluded_paths")
    if not isinstance(value, dict):
        raise CatalogError(
            "docs/catalog/schema.yml excluded_paths must map exact Markdown paths to reasons"
        )

    result: dict[str, str] = {}
    casefolded_paths: dict[str, str] = {}
    for raw_path, raw_reason in value.items():
        if not isinstance(raw_path, str) or not isinstance(raw_reason, str):
            raise CatalogError(
                "docs/catalog/schema.yml excluded_paths must map strings to reason strings"
            )

        path = raw_path.strip()
        reason = raw_reason.strip()
        pure_path = PurePosixPath(path)
        if (
            not path
            or not reason
            or "\\" in path
            or pure_path.is_absolute()
            or pure_path.parts[:1] != ("docs",)
            or ".." in pure_path.parts
            or pure_path.suffix.lower() != ".md"
            or pure_path.as_posix() != path
        ):
            raise CatalogError(
                "docs/catalog/schema.yml excluded_paths entries must use exact "
                "repository-relative docs/**/*.md paths and non-empty reasons"
            )

        folded = path.casefold()
        previous = casefolded_paths.get(folded)
        if previous is not None:
            raise CatalogError(
                f"docs/catalog/schema.yml excluded_paths contains case-colliding paths: "
                f"{previous} and {path}"
            )
        casefolded_paths[folded] = path
        result[path] = reason

        resolved = (ROOT / path).resolve()
        try:
            resolved.relative_to(DOCS_ROOT.resolve())
        except ValueError as exception:
            raise CatalogError(
                f"docs/catalog/schema.yml excluded path escapes docs/: {path}"
            ) from exception
        if not resolved.is_file():
            raise CatalogError(
                f"docs/catalog/schema.yml excluded path does not exist: {path}"
            )
    return result


def normalize_date(value: object, relative_path: str) -> str:
    if isinstance(value, datetime_module.datetime):
        value = value.date()
    if isinstance(value, datetime_module.date):
        return value.isoformat()
    if isinstance(value, str):
        try:
            return datetime_module.date.fromisoformat(value).isoformat()
        except ValueError as exception:
            raise CatalogError(
                f"{relative_path} front matter last_reviewed must be an ISO date"
            ) from exception
    raise CatalogError(f"{relative_path} front matter last_reviewed must be an ISO date")


def normalize_markdown_content(content: str) -> str:
    """Normalize platform line endings and terminal newlines before hashing."""

    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    return normalized.rstrip("\n") + "\n"


def content_sha256(content: str) -> str:
    normalized = normalize_markdown_content(content)
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def parse_front_matter(path: Path) -> tuple[dict[str, object], str, str] | None:
    relative_path = path.relative_to(ROOT).as_posix()
    try:
        content = path.read_text(encoding="utf-8")
    except OSError as exception:
        raise CatalogError(f"cannot read {relative_path}: {exception}") from exception

    normalized_content = normalize_markdown_content(content)
    lines = normalized_content.splitlines()
    if not lines or lines[0] != "---":
        return None

    try:
        closing_index = lines.index("---", 1)
    except ValueError as exception:
        raise CatalogError(f"{relative_path} has unterminated YAML front matter") from exception

    try:
        metadata = strict_yaml_load("\n".join(lines[1:closing_index]))
    except yaml.YAMLError as exception:
        raise CatalogError(f"{relative_path} has invalid YAML front matter: {exception}") from exception
    if not isinstance(metadata, dict):
        raise CatalogError(f"{relative_path} front matter must be a YAML mapping")
    if not all(isinstance(key, str) for key in metadata):
        raise CatalogError(f"{relative_path} front matter keys must be strings")

    body = "\n".join(lines[closing_index + 1 :])
    return metadata, body, normalized_content


def is_fence_close(line: str, marker_character: str, minimum_length: int) -> bool:
    stripped = line.lstrip(" ")
    if len(line) - len(stripped) > 3:
        return False

    marker_length = len(stripped) - len(stripped.lstrip(marker_character))
    return marker_length >= minimum_length and not stripped[marker_length:].strip()


def find_h1_titles(body: str) -> list[str]:
    """Find ATX H1 headings while ignoring fenced code blocks."""

    titles: list[str] = []
    fence_character: str | None = None
    fence_length = 0

    for line in body.splitlines():
        if fence_character is not None:
            if is_fence_close(line, fence_character, fence_length):
                fence_character = None
                fence_length = 0
            continue

        fence_match = FENCE_OPEN_PATTERN.match(line)
        if fence_match is not None:
            marker = fence_match.group(1)
            fence_character = marker[0]
            fence_length = len(marker)
            continue

        h1_match = H1_PATTERN.match(line)
        if h1_match is not None:
            titles.append(h1_match.group(1).strip())

    return titles


def build_catalog() -> dict[str, object]:
    schema = load_mapping(SCHEMA_PATH)
    unknown_schema_fields = sorted(set(schema) - SCHEMA_FIELDS)
    missing_schema_fields = sorted(SCHEMA_FIELDS - set(schema))
    if unknown_schema_fields:
        raise CatalogError(
            "docs/catalog/schema.yml has unknown fields: "
            + ", ".join(unknown_schema_fields)
        )
    if missing_schema_fields:
        raise CatalogError(
            "docs/catalog/schema.yml is missing fields: "
            + ", ".join(missing_schema_fields)
        )

    schema_version = schema.get("schema_version")
    if not isinstance(schema_version, int) or isinstance(schema_version, bool) or schema_version < 1:
        raise CatalogError("docs/catalog/schema.yml must define a positive schema_version")

    document_types = require_schema_string_list(schema, "document_types")
    statuses = require_schema_string_list(schema, "statuses")
    authority_topic_statuses = require_schema_string_list(
        schema, "authority_topic_statuses"
    )
    required_fields = require_schema_string_list(schema, "required_fields")
    allowed_fields = require_schema_string_list(schema, "allowed_fields")
    excluded_paths = load_excluded_paths(schema)

    unknown_authority_statuses = sorted(set(authority_topic_statuses) - set(statuses))
    if unknown_authority_statuses:
        raise CatalogError(
            "docs/catalog/schema.yml authority_topic_statuses contains unknown statuses: "
            + ", ".join(unknown_authority_statuses)
        )
    if set(authority_topic_statuses) != REQUIRED_AUTHORITY_TOPIC_STATUSES:
        raise CatalogError(
            "docs/catalog/schema.yml authority_topic_statuses must contain exactly "
            "accepted and provisional"
        )
    unallowed_required_fields = sorted(set(required_fields) - set(allowed_fields))
    if unallowed_required_fields:
        raise CatalogError(
            "docs/catalog/schema.yml required_fields are missing from allowed_fields: "
            + ", ".join(unallowed_required_fields)
        )

    records: list[dict[str, object]] = []
    records_by_id: dict[str, dict[str, object]] = {}
    topic_owners: dict[str, str] = {}

    markdown_paths = (
        path
        for path in DOCS_ROOT.rglob("*")
        if path.is_file() and path.suffix.casefold() == ".md"
    )
    for path in sorted(markdown_paths, key=lambda item: item.as_posix().casefold()):
        relative_path = path.relative_to(ROOT).as_posix()
        parsed = parse_front_matter(path)

        if relative_path in excluded_paths:
            if parsed is not None:
                raise CatalogError(
                    f"{relative_path} has front matter but is also listed in excluded_paths"
                )
            continue

        if CATALOG_DOCS_ROOT in path.parents:
            raise CatalogError(
                f"{relative_path} is unexpected Markdown under docs/catalog; "
                "add an exact excluded_paths entry with a reason"
            )

        if parsed is None:
            raise CatalogError(
                f"{relative_path} must have valid front matter or an exact "
                "docs/catalog/schema.yml excluded_paths entry with a reason"
            )
        metadata, body, normalized_content = parsed

        missing = [field for field in required_fields if field not in metadata]
        if missing:
            raise CatalogError(f"{relative_path} is missing front matter fields: {', '.join(missing)}")
        unknown = sorted(set(metadata) - set(allowed_fields))
        if unknown:
            raise CatalogError(
                f"{relative_path} has unknown front matter fields: {', '.join(unknown)}"
            )

        doc_id = metadata.get("doc_id")
        document_type = metadata.get("document_type")
        status = metadata.get("status")
        if not isinstance(doc_id, str) or not DOC_ID_PATTERN.fullmatch(doc_id):
            raise CatalogError(f"{relative_path} has invalid doc_id")
        if doc_id in records_by_id:
            raise CatalogError(f"duplicate doc_id {doc_id}: {records_by_id[doc_id]['path']} and {relative_path}")
        if document_type not in document_types:
            raise CatalogError(f"{relative_path} has unknown document_type {document_type!r}")
        if status not in statuses:
            raise CatalogError(f"{relative_path} has unknown status {status!r}")

        owners = require_string_list(metadata, "owners", relative_path, allow_empty=False)
        topics = require_string_list(metadata, "source_of_truth_for", relative_path)
        aliases = require_string_list(metadata, "aliases", relative_path)
        related_code = require_string_list(metadata, "related_code", relative_path)
        related_docs = require_string_list(metadata, "related_docs", relative_path)
        reviewed = normalize_date(metadata.get("last_reviewed"), relative_path)

        if topics and status not in authority_topic_statuses:
            raise CatalogError(
                f"{relative_path} status {status!r} cannot own source_of_truth_for topics"
            )

        h1_matches = find_h1_titles(body)
        if len(h1_matches) != 1:
            raise CatalogError(
                f"{relative_path} must contain exactly one H1 outside fenced code blocks"
            )
        title = h1_matches[0]

        for topic in topics:
            if not TOPIC_PATTERN.fullmatch(topic):
                raise CatalogError(f"{relative_path} has invalid source_of_truth_for topic {topic!r}")
            previous = topic_owners.get(topic)
            if previous is not None:
                raise CatalogError(f"topic {topic} is owned by both {previous} and {doc_id}")
            topic_owners[topic] = doc_id

        for code_path in related_code:
            resolved = (ROOT / code_path).resolve()
            try:
                resolved.relative_to(ROOT.resolve())
            except ValueError as exception:
                raise CatalogError(f"{relative_path} related_code escapes repository: {code_path}") from exception
            if not resolved.exists():
                raise CatalogError(f"{relative_path} related_code does not exist: {code_path}")

        record: dict[str, object] = {
            "doc_id": doc_id,
            "path": relative_path,
            "title": title,
            "content_sha256": content_sha256(normalized_content),
            "document_type": document_type,
            "status": status,
            "owners": owners,
            "last_reviewed": reviewed,
            "source_of_truth_for": topics,
            "aliases": aliases,
            "related_code": related_code,
            "related_docs": related_docs,
        }
        records.append(record)
        records_by_id[doc_id] = record

    if not records:
        raise CatalogError("no indexed Markdown documents with front matter were found")

    for record in records:
        for related_doc in record["related_docs"]:
            if related_doc not in records_by_id:
                raise CatalogError(
                    f"{record['path']} related_docs references unknown doc_id {related_doc}"
                )

    records.sort(key=lambda record: str(record["doc_id"]))
    excluded_records = [
        {"path": path, "reason": excluded_paths[path]}
        for path in sorted(excluded_paths, key=str.casefold)
    ]
    return {
        "schema_version": schema_version,
        "generated_from": "validated Markdown under docs/",
        "documents": records,
        "excluded_documents": excluded_records,
    }


def write_catalog(catalog: dict[str, object]) -> None:
    content = yaml.safe_dump(
        catalog,
        allow_unicode=True,
        sort_keys=False,
        width=1000,
    )
    CATALOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    CATALOG_PATH.write_text(content, encoding="utf-8", newline="\n")
    print(f"Wrote {CATALOG_PATH.relative_to(ROOT)} ({len(catalog['documents'])} documents).")


def check_catalog(catalog: dict[str, object]) -> None:
    if not CATALOG_PATH.is_file():
        raise CatalogError("docs/catalog/documents.yml is missing; run --write")
    committed = load_mapping(CATALOG_PATH)
    if committed != catalog:
        raise CatalogError("docs/catalog/documents.yml is stale; run tools/docs_catalog.py --write")
    print(f"Documentation catalog passed ({len(catalog['documents'])} documents).")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--write", action="store_true", help="regenerate docs/catalog/documents.yml")
    mode.add_argument("--check", action="store_true", help="validate metadata and committed catalog")
    args = parser.parse_args()

    try:
        catalog = build_catalog()
        if args.write:
            write_catalog(catalog)
        else:
            check_catalog(catalog)
    except CatalogError as exception:
        print(f"Documentation catalog failed: {exception}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
