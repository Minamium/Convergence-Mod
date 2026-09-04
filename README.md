# Convergence Mod

A multiplayer-first Calamity addon for Terraria, with a staged long-term path toward a standalone Calamity-scale Content Mod. The first target is a 2–4-player post-Exo Mechs/Supreme Calamitas Raid named **First Severance**.

`Convergence` is the provisional internal Mod/assembly/root-namespace identity; `ConvergenceMod` is the entry class/project filename. Public branding and most story proper nouns remain provisional.

> Current status: architecture bootstrap, not a playable Mod. The source still uses the legacy feature name `ThirdSeverance`, the world adapter is inert, and activation is deliberately rejected. See [Project Status](docs/STATUS.md).

## Start here

- New developer or coding agent: [Documentation Home](docs/README.md)
- Moving to the Windows desktop: [Windows Handoff](docs/handoff/WINDOWS.md)
- Exact implementation inventory: [Project Status](docs/STATUS.md)
- Current Raid loop: [First Severance Specification](docs/encounters/first-severance/ENCOUNTER_SPEC.md)
- Safe work sequence: [First Severance Implementation Plan](docs/encounters/first-severance/IMPLEMENTATION_PLAN.md)
- Topic/alias/source search: [Documentation Search Index](docs/INDEX.md)

## First Raid direction

```text
Activation -> Boss spawn -> Pylon DPS -> Stack -> Spread -> Core exposure
                                                   ^               |
                                                   |--- HP > 0 -----|
```

The accepted Boss boundary is one simple NPC/body and life pool. A central Core/body, one broken ring, and two side arms are only the provisional placeholder silhouette. Damage is accepted only during Core exposure. Raid participants enter a recoverable Downed state on eligible lethal damage; allies use a dedicated item and server-validated channel to revive them.

Part Break, Targeted Line, Personal Effigies, Split Reality, Last Stand, multipart production art, rewards, and final tuning are deferred.

## Target environment

- Terraria `1.4.4.9` — confirmed
- tModLoader stable `v2026.07.3.0` — confirmed
- Calamity Mod `2.2.4` plus official Music Mod `2.1` — confirmed
- .NET SDK `8.0.424` / tModLoader-owned .NET 8 and C# 12 baseline

This Windows baseline passed command build, Build + Reload, Single Player, Dedicated Server, and two-client smoke at commit `b34adbc`. See the [Version Matrix](docs/VERSION_MATRIX.md) and [sanitized evidence](docs/evidence/2026-09-05-windows-baseline.json).

Windows is the primary implementation and runtime-verification workstation. macOS is supported as a secondary Git/docs/review environment and can perform Mod work only when the same pinned runtime is actually installed and tested.

## Architecture at a glance

- One tModLoader assembly with enforced modular-monolith boundaries.
- `Common` owns stable foundations, authority runtime, networking, Raid domains, and compatibility boundaries.
- `Content/Encounters/<Feature>` owns each Boss/Raid vertically.
- `Client` consumes read-only state for UI, VFX, audio, and accessibility.
- Server/Single Player authority decides every gameplay result; clients submit bounded intent.
- Active fights are ephemeral, exact-Fight owned, and cleanup is idempotent.
- Calamity is isolated behind an adapter and is not the permanent owner of the Raid architecture.

Read [Architecture](docs/ARCHITECTURE.md), [Network Architecture](docs/NETWORK_ARCHITECTURE.md), and [ADRs](docs/adr/README.md) before changing authority code.

## Repository map

```text
Common/                         shared foundation, authority, networking, compatibility
Content/Encounters/             feature-first Boss/Raid modules
Client/                         client-only presentation
Assets/                         reviewed runtime exports only
docs/                           specs, status, handoff, runbooks, ADRs, evidence, research
.agents/skills/                 repository-local Raid and source-research workflows
Tests/Convergence.DomainTests/  tModLoader-free authoritative domain harness
tools/                          repository, documentation-catalog, and YAML checks
```

The current `Content/Encounters/ThirdSeverance` module contains an inert arena/Boss plan and disconnected boundary around the pure Downed/Revive service. Its old multipart plan is no longer the active product specification. The first Windows code change will rename and simplify it while keeping activation denied.

## Local verification

```bash
python3 -m pip install --requirement tools/requirements-ci.txt
python3 tools/docs_catalog.py --check
python3 tools/repository_checks.py
python3 tools/validate_yaml.py
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release
```

The domain harness does not compile/load tModLoader. For real verification, clone as `ModSources/Convergence` and follow [Windows Development](docs/runbooks/WINDOWS_DEVELOPMENT.md).

## Repository Skills

Two project-local Skills already exist:

- `.agents/skills/develop-convergence-raids`: authority, cleanup, replication, and multiplayer workflow for Raid changes.
- `.agents/skills/research-tmodloader-sources`: exact-version, source- and license-aware tModLoader/public-Mod research.

They are development tooling and excluded from `.tmod` packaging. Extend them when repeated workflows emerge; do not create a new Skill for a one-off step.

## Contribution and licensing status

The source and asset licenses have not been selected. Repository visibility grants no reuse permission, and unsolicited code/assets are not accepted yet. See [Contributing](CONTRIBUTING.md), [Security Policy](SECURITY.md), and [IP Provenance](docs/IP_PROVENANCE.md).
