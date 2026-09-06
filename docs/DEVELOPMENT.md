---
doc_id: development.general
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-06
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

Use the Windows desktop as the primary tModLoader/Calamity build, Host & Play, and Dedicated Server environment. Read the relevant [Windows Development Runbook](runbooks/WINDOWS_DEVELOPMENT.md) procedure when preparing or using that environment. The point-in-time [Windows Handoff](handoff/WINDOWS.md) is historical context, not an edit prerequisite.

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

Install `tools/requirements-ci.txt` once per Python environment. Routine verification uses the [Verification Matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md):

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

Add `--write-catalog` after indexed-doc edits, `--with-domain` for affected pure/linked domain code, and `--with-dotnet` for C# compiled into the Mod. Combine applicable flags in one invocation. The standalone domain harness is not a Mod build.

## Real build/load gates

```bash
dotnet build ConvergenceMod.csproj
```

The command build can instead run through the wrapper's `--with-dotnet`; do not run both command paths for unchanged inputs. Use the Verification Matrix to select matching Build + Reload, affected-behavior smoke, and multiplayer cases. Full build/load/server/two-client baseline confirmation is required for runtime/dependency changes; it is not the default loop for every edit. User-owned GUI checks remain explicitly `not_run` until observed.

## Evidence

For runtime/baseline evidence, use [the build-record template](evidence/build-record.example.json) as ignored `build-record.local.json`. Record exact commit, OS/architecture, runtime versions, Calamity binary checksum without the binary, each applicable gate result, participant count, and network conditions. Link unchanged environment evidence instead of copying it into each document. Text-only work needs the static-check result, not a runtime record. Sanitized records follow [Evidence](evidence/README.md).

## Feature activation gates

Use [Status](STATUS.md) and the active feature specification/ADRs for enabled development paths and remaining adapter gates. Historical bootstrap instructions do not disable an accepted experiment. Enabling a new path still requires that path's authority, ownership, replication, and cleanup contract plus its declared verification.
