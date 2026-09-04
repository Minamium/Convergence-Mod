---
doc_id: project.repository-layout
document_type: governance
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-04
source_of_truth_for:
  - architecture.repository_layout
aliases:
  - repository layout
  - directory structure
related_code:
  - Common
  - Content
  - Client
  - tools
related_docs:
  - docs.system
  - project.architecture
---

# Repository Layout

```text
/
├─ ConvergenceMod.cs / .csproj   Mod entry and tModLoader build entry
├─ build.txt / description.txt   tModLoader metadata
├─ Common/
│  ├─ Foundation/                Terraria-independent value objects
│  ├─ Encounters/
│  │  ├─ Abstractions/           implementation/tML-independent contracts
│  │  └─ Runtime/                authority coordinator/lifecycle/cleanup
│  ├─ Raids/                     reusable Raid-only domains such as Revive
│  ├─ Networking/                envelope/router/replication/transport
│  ├─ Compatibility/             one isolated adapter per external Mod
│  ├─ Diagnostics/
│  └─ Players/                   player adapters/projections, never global truth
├─ Content/
│  ├─ Encounters/<Feature>/      vertical feature modules
│  ├─ WorldEvents/               lifecycle separate from Raids
│  └─ Shared/                    proven shared player-facing content
├─ Client/
│  ├─ Encounters/<Feature>/      feature cue-to-presentation adapters
│  ├─ UI/ Rendering/ Audio/
│  └─ Accessibility/
├─ Assets/                       reviewed runtime exports only
├─ Localization/
├─ docs/
│  ├─ README.md / INDEX.md       entry and curated search map
│  ├─ STATUS.md / GLOSSARY.md    implementation truth and names
│  ├─ encounters/<feature>/      active feature spec/plan/visual/revive/backlog
│  ├─ runbooks/ / handoff/       repeatable procedure vs point-in-time transfer
│  ├─ evidence/ / catalog/       verification format and generated metadata index
│  ├─ adr/ / research/           decisions and supporting observations
│  └─ stable root documents      project-wide policies kept at existing paths
├─ .agents/skills/               repository-local repeatable workflows
├─ Tests/                        tModLoader-free domain harnesses
├─ tools/                        repository/catalog/YAML checks
└─ .github/                      review, issue, ownership, and CI policy
```

The repository root is the tModLoader Mod Source root. Do not move code into `src/`; tModLoader expects `build.txt` and imports `../tModLoader.targets` from a checkout named `ModSources/Convergence`.

## Feature-name transition

- Target first feature: `Content/Encounters/FirstSeverance`.
- Current source: `Content/Encounters/ThirdSeverance`, an inert legacy bootstrap.
- Rename directory/files/types/namespaces/key/failure prefixes/tests together in an isolated commit.
- Do not add empty target directories early, preserve compatibility aliases for unpublished identifiers, or globally rewrite historical ADR/research/changelog content.

## Naming and type rules

- Internal Mod/assembly/root namespace: `Convergence`.
- Entry class/project: `ConvergenceMod` / `ConvergenceMod.csproj`.
- tModLoader types use role suffixes: `NPC`, `Item`, `Tile`, `TileEntity`, `System`, `UIState`.
- Domain types use descriptive nouns; avoid vague `Manager`, `Helper`, `Utils`, `Data`.
- Stable IDs/failure codes are lowercase English machine strings; localization is client-side.
- Packet enum values are explicit and never renumbered.

## Packaging

`buildIgnore` excludes governance, Skills, tests, docs, tools, local/build output, raw/editable assets, logs, worlds/players, and dependency binaries. `ConvergenceMod.csproj` excludes standalone test source while the test project links only production files it exercises. Calamity `.tmod` binaries and source mirrors never enter this repository.

## Documentation storage

Markdown/front matter is canonical. `docs/catalog/documents.yml` is generated and validated, while any SQLite/embedding index is ignored local cache. See [Documentation System](DOCUMENTATION_SYSTEM.md).
