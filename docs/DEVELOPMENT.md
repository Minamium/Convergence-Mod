---
doc_id: development.general
document_type: runbook
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-12
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

## Workstation and baseline

The [Version Matrix](VERSION_MATRIX.md) owns the confirmed runtime/dependency baseline. Windows is the primary runtime-verification environment. Other platforms can perform Git/docs/review and platform-independent checks; Mod build/load claims require the pinned runtime to be installed and observed on that platform. A past workstation inventory is not a restriction on another contributor's machine.

Use the relevant [Windows runbook](runbooks/WINDOWS_DEVELOPMENT.md) procedure for setup, compilation and load checks. The [Windows handoff](handoff/WINDOWS.md) describes the maintainer workstation and completed checkpoints; it is not a prerequisite for every edit.

## Checkout and local configuration

Clone `Minamium/Convergence-Mod` into a directory named `Convergence`. The GitHub name and Mod source identity are independent. Existing clones use the [remote update procedure](../CONTRIBUTING.md#repository-name-and-existing-clones); no source, assembly or ModSources rename is needed.

Each developer identifies their checkout and build destination through ignored local props. Follow [shared development](../CONTRIBUTING.md#shared-development) for branches/worktrees, integration and separate feature-build profiles. The [Windows source setup](runbooks/WINDOWS_DEVELOPMENT.md#one-canonical-source-and-local-setup) owns actual path resolution. Preserve historical copies; do not use them to overwrite current work.

`python tools/dev.py doctor` resolves the local environment read-only. Package builds record source, dirty delta and output identity; another contributor's manifest cannot identify your local package.

## Repository checks

Install `tools/requirements-ci.txt` once per Python environment. Select checks using the [Verification Matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md):

```sh
python .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

Add `--write-catalog` after an indexed-document edit batch, `--with-domain` for affected linked domain code, and `--with-codec` for packet contracts. Combine the applicable flags. The standalone harness does not compile or load the Mod.

## Package build and load

Use the [recorded native build](runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build) for the current installed Calamity references:

```sh
python tools/dev.py build --native
```

Run it against the output/profile selected for the task. Bare `dotnet build` and the wrapper's `--with-dotnet` are alternatives only when the required local assembly references are configured; they are not an extra mandatory compilation after a successful native build.

Reload or restart peers with the resulting package. Manual load/playtesting belongs to the named task owner unless GUI assistance is requested. Record unobserved applicable checks as `not_run`. Full load/server/two-client compatibility confirmation applies to dependency/runtime changes and declared acceptance gates, not every edit.

## Evidence and completion

[AGENTS.md](../AGENTS.md#verification) owns the completion contract. [Evidence](evidence/README.md) defines sanitized runtime records, including exact source/dirty identity, versions, artifact and topology. Link unchanged environment evidence rather than copying it into every document. Text-only work needs its static-check result, not a runtime record.

Use [Status](STATUS.md) and active feature specs/ADRs for enabled development paths and remaining adapter gates. New activation paths retain their required authority, ownership, replication, cleanup and verification contracts.
