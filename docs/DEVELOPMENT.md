---
doc_id: development.general
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-05
source_of_truth_for:
  - development.general_policy
aliases:
  - development setup
  - build policy
related_code:
  - ConvergenceMod.csproj
  - global.json
  - build.txt
related_docs:
  - development.windows
  - verification.evidence
  - project.status
---

# Development Setup

## Primary workstation

Use the Windows desktop as the primary tModLoader/Calamity build, Host & Play, and Dedicated Server environment. Follow [Windows Development Runbook](runbooks/WINDOWS_DEVELOPMENT.md) and the point-in-time [Windows Handoff](handoff/WINDOWS.md).

macOS can support tModLoader development when its runtime is installed, but the audited MacBook did not contain Terraria, tModLoader, .NET SDK, Calamity, or a valid `ModSources` checkout. It remains useful for documentation, Git, review, and platform-independent work. Never transfer an unverified Mac result into the version matrix as a successful Mod build.

## Confirmed environment

Use [Version Matrix](VERSION_MATRIX.md) as the compatibility source:

- Terraria 1.4.4.9;
- tModLoader stable `v2026.07.3.0`;
- Calamity Mod `2.2.4` plus official Music Mod `2.1`;
- .NET SDK `8.0.424`, with .NET 8/C# 12 owned by tModLoader targets.

These pins were confirmed by the Windows build/load/server baseline on 2026-09-05. Do not silently upgrade one dependency; re-run the complete compatibility gate for any future runtime change.

## Checkout invariant

Clone the repository directly as the internal Mod directory:

```text
<tModLoader user data>/ModSources/Convergence/
  build.txt
  ConvergenceMod.csproj
  ../tModLoader.targets
```

The remote repository name may remain `tmod`; the local directory, assembly, and root namespace must align with `Convergence`. A clone elsewhere cannot resolve `../tModLoader.targets` and is not a valid real-build environment.

## Repository checks

```bash
python3 -m pip install --requirement tools/requirements-ci.txt
python3 tools/docs_catalog.py --check
python3 tools/repository_checks.py
python3 tools/validate_yaml.py
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release
```

The standalone harness exercises linked production domain sources without Terraria. It is not a Mod build.

## Real build/load gates

```bash
dotnet build ConvergenceMod.csproj
```

Then run tModLoader Build + Reload, enter/exit Single Player, load Dedicated Server, and join with two clients using identical Mods. Both command build and Build + Reload are required. C# changes also run feature-specific 2/3/4-player cases when relevant.

## Evidence

Copy [the build-record template](evidence/build-record.example.json) to ignored `build-record.local.json`. Record exact commit, OS/architecture, runtime versions, Calamity binary checksum without the binary, each gate result, participant count, and network conditions. Sanitized records must follow [Evidence](evidence/README.md).

## Current safety gate

`FirstSeveranceAvailabilityPolicy` currently rejects activation and `InertFirstSeveranceWorldAdapter` cannot mutate the World. Do not remove those gates until the implementation slice owning Core/Arena/roster/transport, actor ownership/replication, and Downed adapter evidence has passed its declared exit criteria.
