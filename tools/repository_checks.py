#!/usr/bin/env python3
"""Dependency-free repository policy checks for local development and CI."""

from __future__ import annotations

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path, PurePosixPath
from urllib.parse import unquote


ROOT = Path(__file__).resolve().parents[1]

REQUIRED_PATHS = (
    ".editorconfig",
    ".gitattributes",
    ".gitignore",
    ".github/CODEOWNERS",
    ".github/PULL_REQUEST_TEMPLATE.md",
    ".github/dependabot.yml",
    ".github/ISSUE_TEMPLATE/bug.yml",
    ".github/ISSUE_TEMPLATE/config.yml",
    ".github/ISSUE_TEMPLATE/content-proposal.yml",
    ".github/ISSUE_TEMPLATE/multiplayer-desync.yml",
    ".github/workflows/repository-checks.yml",
    ".agents/skills/develop-convergence-raids/SKILL.md",
    ".agents/skills/develop-convergence-raids/agents/openai.yaml",
    ".agents/skills/research-tmodloader-sources/SKILL.md",
    ".agents/skills/research-tmodloader-sources/agents/openai.yaml",
    "AGENTS.md",
    "ConvergenceMod.csproj",
    "ConvergenceMod.cs",
    "build.txt",
    "description.txt",
    "global.json",
    "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj",
    "Tests/Convergence.DomainTests/Program.cs",
    "tools/requirements-ci.txt",
    "tools/validate_yaml.py",
    "README.md",
    "CHANGELOG.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "Assets/ATTRIBUTION.md",
    "docs/ARCHITECTURE.md",
    "docs/DEVELOPMENT.md",
    "docs/IP_PROVENANCE.md",
    "docs/RELEASE_PROCESS.md",
    "docs/VERSION_MATRIX.md",
)

IGNORED_DIRECTORY_NAMES = {
    ".git",
    ".idea",
    ".vs",
    ".vscode",
    "__pycache__",
    "bin",
    "obj",
    "TestResults",
    "Logs",
    "ModAssemblies",
    "Mods",
    "ci-dependencies",
    "compile_temp",
    "coverage",
    "tml-save",
}

FORBIDDEN_SUFFIXES = {
    ".tmod",
    ".pdb",
    ".dmp",
    ".dll",
    ".exe",
    ".nupkg",
    ".rar",
    ".7z",
    ".zip",
    ".user",
    ".suo",
    ".plr",
    ".pyc",
    ".pyo",
    ".tplr",
    ".twld",
    ".wld",
    ".log",
    ".trace",
    ".binlog",
    ".bak",
    ".userprefs",
    ".pem",
    ".key",
    ".pfx",
    ".p12",
    # Editable art/audio project sources belong in the approved external source store.
    ".als",
    ".ase",
    ".aseprite",
    ".aup3",
    ".blend",
    ".flp",
    ".kra",
    ".logicx",
    ".mid",
    ".midi",
    ".musicxml",
    ".mxl",
    ".psd",
    ".ptx",
    ".rpp",
}

FORBIDDEN_NAME_ENDINGS = (
    ".deps.json",
    ".runtimeconfig.json",
    ".tmod.sha256",
)

TEXT_SUFFIXES = {
    ".cs",
    ".csproj",
    ".editorconfig",
    ".gitattributes",
    ".gitignore",
    ".hjson",
    ".json",
    ".md",
    ".props",
    ".py",
    ".targets",
    ".txt",
    ".yaml",
    ".yml",
}

DISTRIBUTABLE_ASSET_SUFFIXES = {
    ".gif",
    ".jpeg",
    ".jpg",
    ".mp3",
    ".ogg",
    ".otf",
    ".png",
    ".ttf",
    ".wav",
    ".xnb",
}

MAX_REPOSITORY_FILE_BYTES = 25 * 1024 * 1024
MARKDOWN_LINK = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
CONFLICT_MARKER = re.compile(r"^(?:<{7} .+|\|{7} .+|>{7} .+)$", re.MULTILINE)
ATTRIBUTION_ENTRY = re.compile(r"^- Runtime file: `([^`]+)`$", re.MULTILINE)
ATTRIBUTION_REQUIRED_FIELDS = (
    "Asset ID",
    "Asset type",
    "Creator",
    "Creation/acquisition date",
    "Source type",
    "Source work and URL",
    "Tool/model/version",
    "Human modifications",
    "License and redistribution terms",
    "Required attribution",
    "Reviewer and review date",
)
ATTRIBUTION_SOURCE_TYPES = {
    "original",
    "generated",
    "commissioned",
    "licensed",
    "public-domain",
}
SENSITIVE_FILE_NAMES = {
    "id_dsa",
    "id_ecdsa",
    "id_ed25519",
    "id_rsa",
}
SENSITIVE_FILE_PATTERN = re.compile(
    r"^(?:\.env(?:\..+)?|.*(?:credentials|secrets).*\.json)$",
    re.IGNORECASE,
)
FORBIDDEN_JUNK_FILE_NAMES = {".ds_store", "desktop.ini", "thumbs.db"}
FORBIDDEN_PATH_PREFIXES = (
    "Assets/Concept/",
    "Assets/Source/",
    "Assets/Textures/Concept/",
    "Assets/Music/Source/",
    "Assets/Sounds/Source/",
)

SKILL_NAME_PATTERN = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")

REQUIRED_BUILD_IGNORE_MASKS = {
    "*.tmod",
    "*.pdb",
    "*.dll",
    "*.exe",
    "*.zip",
    "*.nupkg",
    "*.rar",
    "*.7z",
    "*.log",
    "*.trace",
    "*.dmp",
    "*.plr",
    "*.wld",
    "*.pyc",
    "*.env*",
    "*credentials*.json",
    "*secrets*.json",
    "*.pem",
    "*.key",
    "*.pfx",
    "*.p12",
    "*.als",
    "*.ase",
    "*.aseprite",
    "*.aup3",
    "*.blend",
    "*.flp",
    "*.kra",
    "*.logicx",
    "*.mid",
    "*.midi",
    "*.musicxml",
    "*.mxl",
    "*.psd",
    "*.ptx",
    "*.rpp",
    "bin\\*",
    "obj\\*",
    ".git\\*",
    ".github\\*",
    ".agents\\*",
    "*\\__pycache__\\*",
    "docs\\*",
    "tools\\*",
    "Mods\\*",
    "Logs\\*",
    "tml-save\\*",
    "ci-dependencies\\*",
    "compile_temp\\*",
    "ModAssemblies\\*",
    "TestResults\\*",
    "Tests\\*",
    "coverage\\*",
    "Assets\\Concept\\*",
    "Assets\\Source\\*",
    "Assets\\Textures\\Concept\\*",
    "Assets\\Music\\Source\\*",
    "Assets\\Sounds\\Source\\*",
    "README.md",
    "AGENTS.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "Directory.Build.props",
    "global.json",
    ".gitattributes",
    ".gitignore",
}


def iter_repository_files() -> list[Path]:
    if (ROOT / ".git").exists():
        result = subprocess.run(
            [
                "git",
                "-C",
                str(ROOT),
                "ls-files",
                "--cached",
                "--others",
                "--exclude-standard",
                "-z",
            ],
            check=True,
            capture_output=True,
            text=True,
        )
        tracked = [ROOT / item for item in result.stdout.split("\0") if item]
        return sorted(tracked, key=lambda item: item.as_posix().casefold())

    files: list[Path] = []
    for path in ROOT.rglob("*"):
        if not path.is_file() and not path.is_symlink():
            continue
        if any(part in IGNORED_DIRECTORY_NAMES for part in path.relative_to(ROOT).parts):
            continue
        files.append(path)
    return sorted(files, key=lambda item: item.as_posix().casefold())


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def is_text_file(path: Path) -> bool:
    text_names = {
        ".editorconfig",
        ".gitattributes",
        ".gitignore",
        "build.txt",
        "description.txt",
    }
    return not path.is_symlink() and (path.name in text_names or path.suffix.lower() in TEXT_SUFFIXES)


def check_required_paths(errors: list[str]) -> None:
    for required in REQUIRED_PATHS:
        if not (ROOT / required).is_file():
            errors.append(f"missing required file: {required}")


def check_paths(files: list[Path], errors: list[str]) -> None:
    normalized: dict[str, str] = {}
    for path in files:
        name = relative(path)
        if path.is_symlink():
            errors.append(f"repository symlinks are forbidden: {name}")
            continue
        folded = name.casefold()
        previous = normalized.get(folded)
        if previous is not None and previous != name:
            errors.append(f"case-insensitive path collision: {previous} <-> {name}")
        normalized[folded] = name

        if path.suffix.lower() in FORBIDDEN_SUFFIXES:
            errors.append(f"forbidden generated or local file: {name}")
        if path.name.casefold() in SENSITIVE_FILE_NAMES or SENSITIVE_FILE_PATTERN.match(path.name):
            errors.append(f"potential credential file is forbidden: {name}")
        if path.name.casefold() in FORBIDDEN_JUNK_FILE_NAMES:
            errors.append(f"operating-system junk file is forbidden: {name}")
        if path.name.casefold().endswith(FORBIDDEN_NAME_ENDINGS):
            errors.append(f"forbidden generated or local file: {name}")
        if any(name.startswith(prefix) for prefix in FORBIDDEN_PATH_PREFIXES):
            errors.append(f"source/concept asset must not be tracked: {name}")
        if path.stat().st_size > MAX_REPOSITORY_FILE_BYTES:
            errors.append(f"file exceeds 25 MiB; use an approved asset source workflow: {name}")


def read_utf8(path: Path, errors: list[str]) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        errors.append(f"text file is not valid UTF-8: {relative(path)}")
        return None


def check_text(files: list[Path], errors: list[str]) -> None:
    for path in files:
        if not is_text_file(path):
            continue
        content = read_utf8(path, errors)
        if content is None:
            continue
        name = relative(path)
        if content and not content.endswith("\n"):
            errors.append(f"text file has no final newline: {name}")
        if "\r\n" in content or "\r" in content:
            errors.append(f"text file must use LF line endings: {name}")
        if CONFLICT_MARKER.search(content):
            errors.append(f"unresolved merge marker in {name}")


def link_target(raw_target: str) -> str | None:
    target = raw_target.strip().strip("<>")
    if not target or target.startswith(("#", "mailto:", "https://", "http://")):
        return None
    # Optional Markdown link titles are not repository paths.
    if " \"" in target:
        target = target.split(" \"", 1)[0]
    target = target.split("#", 1)[0].split("?", 1)[0]
    return unquote(target)


def check_markdown_links(files: list[Path], errors: list[str]) -> None:
    for path in files:
        if path.is_symlink() or path.suffix.lower() != ".md":
            continue
        content = read_utf8(path, errors)
        if content is None:
            continue
        for match in MARKDOWN_LINK.finditer(content):
            target = link_target(match.group(1))
            if target is None:
                continue
            resolved = (path.parent / target).resolve()
            try:
                resolved.relative_to(ROOT.resolve())
            except ValueError:
                errors.append(f"Markdown link escapes repository in {relative(path)}: {target}")
                continue
            if not resolved.exists():
                errors.append(f"broken Markdown link in {relative(path)}: {target}")


def check_structured_text(files: list[Path], errors: list[str]) -> None:
    for path in files:
        if path.is_symlink():
            continue
        suffix = path.suffix.lower()
        if suffix == ".json":
            try:
                with path.open(encoding="utf-8") as stream:
                    json.load(stream)
            except (json.JSONDecodeError, OSError) as exc:
                errors.append(f"invalid JSON in {relative(path)}: {exc}")
        elif suffix in {".csproj", ".props", ".targets"}:
            try:
                ElementTree.parse(path)
            except (ElementTree.ParseError, OSError) as exc:
                errors.append(f"invalid XML in {relative(path)}: {exc}")


def check_test_source_isolation(errors: list[str]) -> None:
    project_path = ROOT / "ConvergenceMod.csproj"
    if not project_path.is_file():
        return

    try:
        project = ElementTree.parse(project_path)
    except (ElementTree.ParseError, OSError):
        return

    removed_sources = {
        (compile_node.get("Remove") or "").replace("\\", "/")
        for compile_node in project.findall(".//Compile")
        if compile_node.get("Remove")
    }
    if "Tests/**/*.cs" not in removed_sources:
        errors.append(
            "ConvergenceMod.csproj must remove Tests/**/*.cs from SDK compilation"
        )


def check_test_project_links(errors: list[str]) -> None:
    for project_path in sorted((ROOT / "Tests").rglob("*.csproj")):
        try:
            project = ElementTree.parse(project_path)
        except (ElementTree.ParseError, OSError):
            continue

        for compile_node in project.findall(".//Compile"):
            include = compile_node.get("Include")
            if not include:
                continue

            resolved = (project_path.parent / include).resolve()
            try:
                resolved.relative_to(ROOT.resolve())
            except ValueError:
                errors.append(
                    f"test project Compile link escapes repository: "
                    f"{relative(project_path)} -> {include}"
                )
                continue

            if not resolved.is_file():
                errors.append(
                    f"test project Compile link is missing: "
                    f"{relative(project_path)} -> {include}"
                )


def check_tmod_identity(files: list[Path], errors: list[str]) -> None:
    project_path = ROOT / "ConvergenceMod.csproj"
    if not project_path.is_file():
        return
    try:
        project = ElementTree.parse(project_path)
    except (ElementTree.ParseError, OSError):
        return

    assembly_node = project.find(".//AssemblyName")
    namespace_node = project.find(".//RootNamespace")
    assembly_name = (assembly_node.text or "").strip() if assembly_node is not None else ""
    root_namespace = (namespace_node.text or "").strip() if namespace_node is not None else ""

    if not assembly_name or not root_namespace:
        errors.append("csproj must declare non-empty AssemblyName and RootNamespace")
        return
    if assembly_name != root_namespace:
        errors.append(
            f"tModLoader identity mismatch: AssemblyName={assembly_name}, RootNamespace={root_namespace}"
        )

    namespace_pattern = re.compile(rf"^namespace\s+{re.escape(assembly_name)}(?:[.;])", re.MULTILINE)
    if not any(
        path.suffix.lower() == ".cs"
        and (content := read_utf8(path, errors)) is not None
        and namespace_pattern.search(content)
        for path in files
    ):
        errors.append(f"no C# namespace begins with the internal Mod name '{assembly_name}'")


def check_build_ignore(errors: list[str]) -> None:
    build_path = ROOT / "build.txt"
    if not build_path.is_file():
        return
    content = read_utf8(build_path, errors)
    if content is None:
        return

    build_ignore_lines = [
        line.split("=", 1)[1]
        for line in content.splitlines()
        if line.strip().startswith("buildIgnore") and "=" in line
    ]
    if len(build_ignore_lines) != 1:
        errors.append("build.txt must contain exactly one buildIgnore property")
        return

    masks = {mask.strip() for mask in build_ignore_lines[0].split(",") if mask.strip()}
    for missing in sorted(REQUIRED_BUILD_IGNORE_MASKS - masks):
        errors.append(f"buildIgnore is missing required packaging mask: {missing}")

    include_source = next(
        (
            line.split("=", 1)[1].strip().casefold()
            for line in content.splitlines()
            if line.strip().startswith("includeSource") and "=" in line
        ),
        None,
    )
    has_source_license = any(
        (ROOT / filename).is_file()
        for filename in ("LICENSE", "LICENSE.md", "LICENSE.txt")
    )
    if not has_source_license and include_source != "false":
        errors.append("includeSource must remain false until a source license exists")


def check_repository_skills(errors: list[str]) -> None:
    skills_root = ROOT / ".agents" / "skills"
    if not skills_root.is_dir():
        errors.append("missing repository Skills directory: .agents/skills")
        return

    skill_directories = sorted(
        (path for path in skills_root.iterdir() if path.is_dir()),
        key=lambda path: path.name.casefold(),
    )
    if not skill_directories:
        errors.append(".agents/skills must contain at least one Skill")
        return

    for directory in skill_directories:
        relative_directory = relative(directory)
        if not SKILL_NAME_PATTERN.fullmatch(directory.name):
            errors.append(f"invalid Skill directory name: {relative_directory}")

        skill_path = directory / "SKILL.md"
        if not skill_path.is_file():
            errors.append(f"Skill is missing SKILL.md: {relative_directory}")
            continue

        content = read_utf8(skill_path, errors)
        if content is None:
            continue
        if not content.startswith("---\n") or "\n---\n" not in content[4:]:
            errors.append(f"Skill has invalid YAML frontmatter delimiters: {relative(skill_path)}")
            continue

        frontmatter = content[4:].split("\n---\n", 1)[0]
        fields: dict[str, str] = {}
        for line in frontmatter.splitlines():
            if not line.strip() or ":" not in line:
                continue
            key, value = line.split(":", 1)
            fields[key.strip()] = value.strip()

        if fields.get("name") != directory.name:
            errors.append(
                f"Skill frontmatter name must match its directory in {relative(skill_path)}"
            )
        if not fields.get("description"):
            errors.append(f"Skill frontmatter requires a description: {relative(skill_path)}")
        if not (directory / "agents" / "openai.yaml").is_file():
            errors.append(f"Skill is missing agents/openai.yaml: {relative_directory}")


def check_architecture_boundaries(files: list[Path], errors: list[str]) -> None:
    forbidden_references = {
        "Common": (
            "using Convergence.Content",
            "using Convergence.Client",
            "global::Convergence.Content",
            "global::Convergence.Client",
        ),
        "Content": ("using Convergence.Client", "global::Convergence.Client"),
    }
    for path in files:
        if path.is_symlink() or path.suffix.lower() != ".cs":
            continue
        parts = path.relative_to(ROOT).parts
        if not parts:
            continue
        layer = parts[0]
        content = read_utf8(path, errors)
        if content is None:
            continue

        for reference in forbidden_references.get(layer, ()):
            if reference in content:
                errors.append(
                    f"dependency direction violation in {relative(path)}: {layer} cannot reference {reference}"
                )

        if len(parts) >= 2 and parts[0] == "Common" and parts[1] == "Foundation":
            if re.search(r"^\s*using\s+(?:Terraria|CalamityMod)(?:[.;])", content, re.MULTILINE):
                errors.append(f"Foundation cannot import game/mod APIs: {relative(path)}")
            if "global::Terraria" in content or "global::CalamityMod" in content:
                errors.append(f"Foundation cannot reference game/mod APIs: {relative(path)}")

        encounter_abstractions_prefix = ("Common", "Encounters", "Abstractions")
        if parts[:3] == encounter_abstractions_prefix:
            forbidden_abstraction_references = (
                "using Terraria",
                "using CalamityMod",
                "using Convergence.Common.Encounters.Runtime",
                "using Convergence.Common.Networking",
                "global::Terraria",
                "global::CalamityMod",
                "global::Convergence.Common.Encounters.Runtime",
                "global::Convergence.Common.Networking",
            )
            for reference in forbidden_abstraction_references:
                if reference in content:
                    errors.append(
                        f"Encounter abstractions cannot reference implementation APIs in "
                        f"{relative(path)}: {reference}"
                    )

        encounter_runtime_prefix = ("Common", "Encounters", "Runtime")
        if parts[:3] == encounter_runtime_prefix and (
            "using Convergence.Common.Networking" in content
            or "global::Convergence.Common.Networking" in content
        ):
            errors.append(f"Encounter Runtime cannot depend on Networking: {relative(path)}")

        calamity_adapter_prefix = ("Common", "Compatibility", "Calamity")
        is_calamity_adapter = parts[:3] == calamity_adapter_prefix
        if not is_calamity_adapter and (
            re.search(r"^\s*using\s+CalamityMod(?:[.;])", content, re.MULTILINE)
            or "global::CalamityMod" in content
        ):
            errors.append(f"Calamity references must stay in the compatibility adapter: {relative(path)}")


def check_asset_attribution(files: list[Path], errors: list[str]) -> None:
    manifest_path = ROOT / "Assets" / "ATTRIBUTION.md"
    if not manifest_path.is_file():
        return
    manifest = read_utf8(manifest_path, errors)
    if manifest is None:
        return

    records_marker = "## Records"
    if records_marker not in manifest:
        errors.append("Assets/ATTRIBUTION.md has no Records section")
        return
    records_section = manifest.split(records_marker, 1)[1]
    if "\n## " in records_section:
        records_section = records_section.split("\n## ", 1)[0]

    entry_matches = list(ATTRIBUTION_ENTRY.finditer(records_section))
    listed_assets = [match.group(1) for match in entry_matches]
    if len(listed_assets) != len(set(listed_assets)):
        errors.append("Assets/ATTRIBUTION.md contains duplicate runtime file entries")

    actual_assets = {
        relative(path)
        for path in files
        if not path.is_symlink() and path.suffix.lower() in DISTRIBUTABLE_ASSET_SUFFIXES
    }
    listed_asset_set = set(listed_assets)

    asset_ids: set[str] = set()
    for index, match in enumerate(entry_matches):
        asset_path = match.group(1)
        path_parts = PurePosixPath(asset_path).parts
        if not asset_path or asset_path.startswith("/") or ".." in path_parts or "\\" in asset_path:
            errors.append(f"ATTRIBUTION.md has an invalid runtime path: {asset_path}")

        block_end = entry_matches[index + 1].start() if index + 1 < len(entry_matches) else len(records_section)
        block = records_section[match.end():block_end]
        fields: dict[str, str] = {}
        for field in ATTRIBUTION_REQUIRED_FIELDS:
            field_match = re.search(
                rf"^- {re.escape(field)}:\s*(\S.*)$",
                block,
                re.MULTILINE,
            )
            if field_match is None:
                errors.append(f"ATTRIBUTION.md record is missing '{field}': {asset_path}")
                continue
            fields[field] = field_match.group(1).strip()

        asset_id = fields.get("Asset ID")
        if asset_id:
            if asset_id in asset_ids:
                errors.append(f"ATTRIBUTION.md contains duplicate Asset ID: {asset_id}")
            asset_ids.add(asset_id)

        source_type = fields.get("Source type")
        if source_type and source_type not in ATTRIBUTION_SOURCE_TYPES:
            errors.append(f"ATTRIBUTION.md has invalid Source type for {asset_path}: {source_type}")

    for asset_path in sorted(actual_assets - listed_asset_set):
        errors.append(f"distributable asset has no exact ATTRIBUTION.md record: {asset_path}")
    for asset_path in sorted(listed_asset_set - actual_assets):
        errors.append(f"ATTRIBUTION.md references a missing runtime asset: {asset_path}")


def main() -> int:
    errors: list[str] = []
    files = iter_repository_files()
    check_required_paths(errors)
    check_paths(files, errors)
    check_text(files, errors)
    check_markdown_links(files, errors)
    check_structured_text(files, errors)
    check_test_source_isolation(errors)
    check_test_project_links(errors)
    check_tmod_identity(files, errors)
    check_build_ignore(errors)
    check_repository_skills(errors)
    check_architecture_boundaries(files, errors)
    check_asset_attribution(files, errors)

    if errors:
        print("Repository checks failed:")
        for error in sorted(set(errors)):
            print(f"  - {error}")
        return 1

    print(f"Repository checks passed ({len(files)} files inspected).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
