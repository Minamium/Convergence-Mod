---
doc_id: project.repository-layout
document_type: governance
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-07
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
├─ tools/                        local build/provenance, static/codec/tool checks
└─ .github/                      review, issue, ownership, and CI policy
```

The repository root is the tModLoader Mod Source root, named `Convergence`; do not add a `src/` nesting layer. The actual checkout may live outside ModSources, whose entry can be a junction to it. Targets resolve through explicit/ignored local configuration or the traditional parent-targets fallback; [Windows setup](runbooks/WINDOWS_DEVELOPMENT.md#one-canonical-source-and-local-setup) owns the procedure. A worktree is a separate, explicitly selected build source, not a recurring copy to merge by hand.

## Feature-name transition

- Current first feature: `Content/Encounters/FirstSeverance`, an active development encounter; current adapters and deferred production seams are listed in [Status](STATUS.md).
- The directory/files/types/namespaces/key/failure prefixes/tests were renamed together in one isolated commit.
- Do not add compatibility aliases for the unpublished legacy identifier or globally rewrite historical ADR/research/changelog content.

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
