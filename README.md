# Convergence Mod

A multiplayer-first Calamity addon for Terraria. The first vertical slice is a 2-4 player Raid after Exo Mechs and Supreme Calamitas; the long-term architecture supports additional bosses, Raids, world content, items, and presentation systems.

`Convergence` is the provisional internal identity and root namespace; `ConvergenceMod` is only the entry class/project filename. The public title, story terms, and release branding remain changeable.

> Development status: architecture bootstrap. The source skeleton exists, but no playable build or completed boss exists yet.

## Target environment

- Terraria 1.4.4.9
- tModLoader 1.4.4 stable `v2026.06.3.6`
- Calamity Mod `2.2.2` and its official Music dependency
- .NET 8 / C# 12, supplied by the pinned tModLoader targets

These remain candidate pins until Build + Reload and Dedicated Server smoke tests pass in the real ModSources environment. See [Version Matrix](docs/VERSION_MATRIX.md).

## Architecture at a glance

- One tModLoader assembly, split into enforced modules.
- `Common` owns stable foundation, runtime, networking, and compatibility boundaries.
- `Content/Encounters/<Feature>` owns each Boss or Raid vertically.
- `Common/Networking/Replication` owns the read-only replica; `Client` will consume it for UI, VFX, audio, and accessibility.
- Single Player/server decides every gameplay result.
- Active fights are ephemeral and cleanup is idempotent.
- Calamity code, binaries, images, and audio are not vendored.

Start with [Architecture](docs/ARCHITECTURE.md), [Repository Layout](docs/REPOSITORY_LAYOUT.md), and [Development Setup](docs/DEVELOPMENT.md).

## Repository map

```text
Common/                         shared foundation and runtime
Content/Encounters/             feature-first Boss/Raid modules
Client/                         client-only presentation
Assets/                         reviewed runtime exports
docs/adr/                       architectural decision history
tools/repository_checks.py      local and CI policy checks
```

The first module is `Content/Encounters/ThirdSeverance`. It currently registers metadata and an inert, cleanup-safe bootstrap runtime. A feature policy deliberately rejects activation until Milestone 1 supplies server-resolved Core/Arena/progression/roster validation; packets, Arena objects, NPCs, phases, and rewards are not implemented yet.

## Local verification

```bash
python3 tools/repository_checks.py
python3 -m pip install --requirement tools/requirements-ci.txt
python3 tools/validate_yaml.py
```

For a real build, clone the repository as `ModSources/Convergence` under the pinned tModLoader installation, then run:

```bash
dotnet build ConvergenceMod.csproj
```

Full instructions and limitations are in [Development Setup](docs/DEVELOPMENT.md). GitHub Actions currently validates repository policy only; it does not pretend to compile without pinned tModLoader and private dependency binaries.

## Documentation

### Product and encounter

- [Project Brief](docs/PROJECT_BRIEF.md)
- [Encounter Specification](docs/ENCOUNTER_SPEC.md)
- [Arena Infrastructure](docs/ARENA_INFRASTRUCTURE.md)
- [Milestones](docs/MILESTONES.md)

### Engineering

- [Architecture](docs/ARCHITECTURE.md)
- [Repository Layout](docs/REPOSITORY_LAYOUT.md)
- [Development Setup](docs/DEVELOPMENT.md)
- [Coding Standards](docs/CODING_STANDARDS.md)
- [Content Authoring](docs/CONTENT_AUTHORING.md)
- [Network Architecture](docs/NETWORK_ARCHITECTURE.md)
- [Test Plan](docs/TEST_PLAN.md)
- [Release Process](docs/RELEASE_PROCESS.md)
- [Architecture Decisions](docs/adr/README.md)
- [Version Matrix](docs/VERSION_MATRIX.md)
- [Research Sources](docs/SOURCES.md)

### Art, audio, and rights

- [Art Direction](docs/ART_DIRECTION.md)
- [Asset and Audio Pipeline](docs/ASSET_PIPELINE.md)
- [Audio Cue Sheet](docs/AUDIO_CUE_SHEET.md)
- [IP and Asset Provenance](docs/IP_PROVENANCE.md)
- [Asset Attribution Register](Assets/ATTRIBUTION.md)

## Contribution and licensing status

The source and asset licenses have not been selected. No permission is granted merely by repository visibility, and unsolicited code/assets are not accepted yet. See [Contributing](CONTRIBUTING.md) and [Security Policy](SECURITY.md).
