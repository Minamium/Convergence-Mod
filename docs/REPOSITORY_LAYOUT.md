# Repository Layout

```text
/
├─ ConvergenceMod.cs              # Mod entry point only
├─ ConvergenceMod.csproj          # tModLoader build entry
├─ build.txt / description.txt    # tML metadata
├─ Common/
│  ├─ Foundation/                 # Terraria-independent value objects
│  ├─ Encounters/
│  │  ├─ Abstractions/            # implementation/tML-independent contracts
│  │  └─ Runtime/                 # authority coordinator, lifecycle, cleanup
│  ├─ Raids/                      # Raid-only services, added incrementally
│  ├─ Networking/                 # envelope, router, replication, transport
│  ├─ Compatibility/              # one adapter directory per external Mod
│  ├─ Diagnostics/                # structured logs and debug inspection
│  └─ Players/                    # per-player adapter/cache, never global truth
├─ Content/
│  ├─ Encounters/<Feature>/       # vertical feature modules
│  ├─ WorldEvents/                # separate lifecycle from Raids
│  └─ Shared/                     # truly shared player-facing content
├─ Client/
│  ├─ Encounters/<Feature>/       # feature cue -> presentation adapters
│  ├─ UI/
│  ├─ Rendering/
│  ├─ Audio/
│  └─ Accessibility/
├─ Assets/                        # reviewed runtime exports only
├─ Localization/
├─ docs/
│  └─ adr/                        # immutable decision history
├─ .agents/skills/                # repository-scoped development workflows
├─ Tests/                         # standalone tModLoader-free domain harnesses
├─ tools/                         # dependency-free repository checks
└─ .github/                       # review, issue, ownership, and CI policy
```

The repository root is the tModLoader Mod Source root. Do not move the Mod into `src/`; tModLoader expects `build.txt` and the project under the source directory imported through `../tModLoader.targets`.

## Naming

- Internal Mod/assembly name: `Convergence`.
- Root namespace: `Convergence`.
- Entry class/project filename: `ConvergenceMod` / `ConvergenceMod.csproj`.
- First feature module: `Content/Encounters/ThirdSeverance`.
- Public title remains provisional and can change without renaming every namespace.

tModLoader source folders should be checked out as `ModSources/Convergence`, even though the GitHub repository is currently named `tmod`. tModLoader verifies that at least one loaded type starts with the internal Mod namespace, so the folder, assembly, and namespace identity must remain aligned.

## Type rules

- Prefer one meaningful type per C# file.
- tModLoader types use role suffixes: `NPC`, `Item`, `Tile`, `TileEntity`, `System`, `UIState`.
- Domain types use descriptive nouns without tML suffixes.
- Avoid vague `Manager`, `Helper`, `Utils`, and `Data` names.
- Protocol enum values are explicit and never renumbered.
- Stable IDs and failure codes are English machine strings; localization happens at the client boundary.

## Packaging

`buildIgnore` excludes repository governance, `.agents` Skills, standalone `Tests`, docs, tools, concept/raw assets, and working files from tModLoader compilation/packaging. `ConvergenceMod.csproj` separately removes `Tests/**/*.cs` from SDK compilation, while the domain-test project explicitly links only the production files it exercises. Calamity `.tmod` files, source mirrors, logs, local settings, and dependency binaries never enter this repository.

## Repository Skills

- `develop-convergence-raids` routes Raid implementation through server authority, bounded replication, idempotent cleanup, and the required multiplayer matrix.
- `research-tmodloader-sources` standardizes exact-source, versioned, license-aware API and public-Mod investigation.

`AGENTS.md` remains the concise always-on contract. Skills hold procedures that are only useful for a matching task, and link back to the repository documents as the source of truth.
