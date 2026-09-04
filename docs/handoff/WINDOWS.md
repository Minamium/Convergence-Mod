---
doc_id: handoff.windows
document_type: handoff
status: accepted
owners:
  - project
last_reviewed: 2026-09-04
source_of_truth_for:
  - handoff.windows.2026-09-04
aliases:
  - Windows handoff
  - desktop migration
related_code:
  - Content/Encounters/FirstSeverance
  - Common/Raids/Revive
  - Tests/Convergence.DomainTests
related_docs:
  - project.status
  - development.windows
  - encounter.first-severance.plan
---

# Windows Handoff — 2026-09-04

This checkpoint transfers Convergence from planning/bootstrap work on the MacBook to primary implementation on minami's Windows desktop.

## Repository checkpoint

- Remote: `git@github.com:Minamium/tmod.git`
- Branch: `main`
- Local source directory required by tModLoader: `ModSources\Convergence`
- Base code/policy commit before this documentation handoff: `d5f8758` (`build: add domain test CI and staged dependency policy`)
- Expected state after transfer: `git pull --ff-only origin main`, then clean `git status --short`

Use `git log -1 --oneline` to record the exact handoff commit after pulling. Do not hard-reset an existing Windows checkout with uncommitted work.

## Read order

1. [`README.md`](../../README.md)
2. [Documentation Home](../README.md)
3. [Project Status](../STATUS.md)
4. [First Severance overview](../encounters/first-severance/README.md)
5. [First Severance implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md)
6. [Windows development runbook](../runbooks/WINDOWS_DEVELOPMENT.md)
7. [Version matrix](../VERSION_MATRIX.md)
8. [Architecture](../ARCHITECTURE.md) and [Network Architecture](../NETWORK_ARCHITECTURE.md)
9. [ADR-0002](../adr/0002-server-authoritative-encounters.md), [ADR-0003](../adr/0003-in-world-logical-arena.md), [ADR-0005](../adr/0005-server-authoritative-downed-revive.md), [ADR-0006](../adr/0006-staged-calamity-independence.md), [ADR-0007](../adr/0007-first-severance-vertical-slice.md)

## Decisions transferred

- Start from the first encounter, named `First Severance` / `第一断絶`, rather than designing around a “third” event.
- Keep the first Boss visually simple: one Boss NPC/body and one life pool, with no separately damageable presentation parts.
- The only first-slice combat loop is Pylon DPS check → Stack → Spread → Core exposure → repeat while HP remains.
- Boss damage is accepted only during exposure. HP persists; exposure has no separate damage-budget failure.
- Stack is a server-owned head-split damage pool rather than only an “everyone inside” check.
- Raid lethal damage becomes Downed; another player uses a dedicated item and channels a server-validated revive. Shared token defaults are 1/2/3 for 2/3/4 players.
- Server/SP owns all gameplay outcomes. Clients send bounded intent and render replicas.
- Windows is primary for tModLoader, Calamity, Host & Play, and Dedicated Server validation. The MacBook remains a valid secondary docs/review machine.
- The initial product is a Calamity addon, with the accepted staged route toward removing the hard dependency later.

Boss/lore/facility/item proper names other than `First Severance` are provisional. The loop order, simple one-body constraint, multiplayer authority, and ally-item recovery direction are accepted. The central Core + broken ring + two arms placeholder, one-Pylon-per-roster layout, missed-Pylon pulse/short exposure, three-Overload threshold, Stack `2 / 2 / 3` shares, revive token counts, damage, HP, radii, timings, arena details, and eight-exposure cap are deterministic prototype policy, not final user-approved balance or art.

The first vertical slice targets roughly 2–4 minutes. The broader 5–12-minute goal belongs to an eventual expanded Raid. Loop-cap exhaustion currently commits `Defeat(LoopCapExceeded)` directly; do not retain the legacy hard-enrage/Last Stand edge.

The network design assumes cooperative play with unmodified clients. No Convergence packet can directly declare DPS, position-check success, life, revive completion, or victory, but this is not anti-cheat against a client modifying Terraria's ordinary movement/combat replication.

## Current implementation truth

There is no playable Boss. The current source uses the legacy name `ThirdSeverance` and deliberately rejects activation. It contains:

- a robust generic encounter/runtime/cleanup and replica bootstrap;
- an inert Core-anchored arena/Boss plan;
- a pure, substantially tested Downed/Revive domain;
- repository checks and a dependency-free domain harness.

It does not contain the Core Tile/TE, Boss/Pylon NPCs, phase executor, Stack/Spread resolution, live transport/snapshots, revive item, death/control adapter, production presentation, rewards, or runtime evidence. See [Status](../STATUS.md) for the exact inventory.

## MacBook environment audit

Observed before transfer:

- MacBook Air with Apple Silicon M4, 16 GB RAM, macOS 26.6.2;
- Steam, VS Code 1.131.0, Git 2.50.1, GitHub CLI 2.97.0, Homebrew, Python 3.13.2, and Xcode Command Line Tools present;
- Terraria, tModLoader, .NET SDK, Calamity, C# Dev Kit/Rider, and Aseprite not found;
- repository checkout was outside a tModLoader `ModSources/Convergence` tree, so `..\tModLoader.targets` was absent;
- repository/YAML checks could run, but no honest Mod build/load/server claim could be made.

tModLoader can be developed on macOS, but this machine was not prepared for it and the project needs Windows/Dedicated Server evidence anyway. Do not spend time duplicating the full runtime on the Mac before the Windows baseline is stable.

## First Windows session

1. Install/confirm the tools and candidate versions in the [runbook](../runbooks/WINDOWS_DEVELOPMENT.md).
2. Clone/pull as `ModSources\Convergence`.
3. Run catalog, repository, YAML, and domain checks.
4. Run command-line build, Build + Reload, Single Player, Dedicated Server, and two-client baseline.
5. Fill `build-record.local.json` from the template; update the version matrix only with observed evidence.
6. Perform the isolated `ThirdSeverance` → `FirstSeverance` rename while activation remains denied.
7. Replace the obsolete immutable plan with the simple repeated loop and update domain tests.
8. Before enabling Boss/Pylon damage, record the normal-hit pipeline for host/non-host and Dedicated Server as required by the implementation/test plans.
9. Continue slices in the [implementation plan](../encounters/first-severance/IMPLEMENTATION_PLAN.md).

## Blocking runtime research

Before enabling Boss/Pylon damage, instrument representative Terraria/tModLoader/Calamity hits in Single Player, Host & Play, and Dedicated Server. Prove the server/SP-observed seam that validates exact-Fight actor ownership, participant eligibility, phase damage gate, committed life/death, and sync. Do not add a custom damage-report packet or call ordinary Terraria replication cheat-proof.

Instrument pinned tModLoader/Calamity lethal handling in Single Player, Host & Play, and Dedicated Server. Establish exactly how `PreKill`, Calamity personal revive mechanics, life/cooldown changes, sync, and duplicate callbacks interact. Until an explicit adapter policy and tests exist, keep death interception disconnected and activation fail-closed.

## Do not accidentally implement

Part Break, Targeted Line/Bait, Personal Effigies, Split Reality, hard-enrage/Last Stand phases, multipart Crown/Wings/Heart Casing, final art/music/rewards, Solo, or Standalone progression are outside the first slice. Their ideas remain in the [backlog](../encounters/first-severance/BACKLOG.md).

This exclusion narrows implementation order only; it does not cap the eventual Calamity-scale Raid or standalone Mod. Promote a deferred idea later only with a fresh player-facing contract, authority/replication/cleanup model, tests, and explicit scope decision.

## Handoff completion check

The transfer is complete when the Windows checkout is clean, the exact commit/version evidence is recorded, the candidate toolchain either passes or has one reproducible blocker, and the next task begins from Slice 1 without relying on this chat history.
