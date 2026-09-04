---
doc_id: project.milestones
document_type: plan
status: accepted
owners:
  - project
last_reviewed: 2026-09-05
source_of_truth_for:
  - project.roadmap
aliases:
  - milestones
  - roadmap
related_code:
  - Common
  - Content
  - Tests
related_docs:
  - project.status
  - encounter.first-severance.plan
---

# Milestones

The implementation sequence within the first Raid is defined in the [First Severance plan](encounters/first-severance/IMPLEMENTATION_PLAN.md). This file describes project-level gates. A checked bootstrap item is not proof that the corresponding playable integration exists; [Status](STATUS.md) owns that distinction.

## Milestone 0 — Repository and compatibility bootstrap

- [x] Repository policy, contribution/security/release/provenance documents.
- [x] Minimal tModLoader source skeleton and Calamity Stage A reference.
- [x] Modular source boundaries, feature registration, and compatibility adapter boundary.
- [x] Generic authority lifecycle, runtime/factory/update boundary, exact-Fight cleanup/retry.
- [x] Encounter Sequence, bounded terminal-priority snapshots, replica/tombstone bootstrap.
- [x] Versioned packet envelope parser and direction guard.
- [x] Pure Downed/Revive domain and dependency-free harness.
- [x] Inert legacy arena/Boss plan and activation fail-closed policy.
- [x] Documentation index/catalog, First Severance decision/specs, Windows handoff/runbook.
- [ ] Source/asset license decision.
- [x] Windows command build and Build + Reload.
- [x] Dedicated Server/two-client baseline.
- [x] Candidate versions promoted to Confirmed.

Exit: clean pinned client/server baseline with sanitized evidence.

## Milestone 1 — First Severance identity and arena preparation

- atomic legacy `ThirdSeverance` → `FirstSeverance` source/key/failure-prefix rename while inert;
- Foundation Core Item/Tile/Tile Entity;
- server-resolved Core/anchor and side/range/nonce/rate validation;
- Calamity progression adapter for Exo Mechs and Supreme Calamitas;
- pure 320x140 Arena validation and structured issues;
- frozen 2–4 participant roster, Join/Ready/cancel/timeout;
- logical Barrier presentation plus server correction/outsider policy;
- repeated cancel/Foundation-Core-break/unexpected-TE-loss/disconnect/unload cleanup diagnostics.

Exit: preparation cycles are deterministic and leave no stale state on Dedicated Server; combat activation remains gated if actor/replication work is incomplete.

## Milestone 2 — Multiplayer feature state

- typed activation/Ready/cancel/snapshot handlers;
- full bounded snapshot and revisioned state/delta dispatch;
- stable participant binding/connection epoch and rejoin policy;
- feature clock, substate, assignment, actor-handle, and outcome replication;
- stale/reordered/duplicate recovery and rate-limited diagnostics;
- packet fault/latency injection.

Exit: clients recover from stale/reordered state without deciding or changing authority outcomes.

## Milestone 3 — Basic repeated encounter loop

- replace obsolete multipart immutable plan/tests;
- one simple Boss NPC and the provisional 2/3/4-Pylon prototype layout;
- authoritative damage gate and persistent Boss life;
- Spawn → Pylon → Stack → Spread → Core exposure → Reset loop;
- early Pylon success and the provisional failed-Pylon Overload/pulse/short-exposure/third-Overload-Defeat policy;
- server position resolution and client-only telegraphs;
- exact-Fight actor registration and cleanup.

Exit: 2–4 players can reach Victory or Defeat through the intended loop with identical state on server/clients. This is not yet first-playable acceptance without recovery.

## Milestone 4 — Downed and Revive integration

Foundation already implemented: pure authority state, stable binding/epoch, channel lease/nonce, 1/2/3 shared tokens, same-tick wipe commit, reconnect grace, projections, snapshots, cleanup, and domain tests.

Remaining:

- pinned tModLoader/Calamity lethal-hook instrumentation in Single Player, Host & Play, Dedicated Server;
- accepted coexistence/body-normalization policy;
- authority-only death interception and ModPlayer control/targeting projection;
- non-consumable revival item and held-use/range/movement adapter;
- typed start/cancel transport and nested feature snapshot;
- server life restore, immunity, weakness, and defensive resets;
- host/non-host, simultaneous death, damage/movement cancel, disconnect/rejoin/slot reuse matrix.

Exit: every tested lethal/revive/cleanup path resolves once without duplicated resources, ordinary-death conflicts, or permanent player state.

## Milestone 5 — First playable Raid acceptance

- integrated Core/Arena/Barrier/Ready/loop/Downed/Revive;
- candidate timings/HP/damage tuned from representative Calamity loadouts;
- Single Player diagnostics, Host & Play, Dedicated Server 2/3/4 players;
- latency/loss/reorder, join/rejoin/disconnect, host/non-host, cleanup fault matrix;
- placeholder visuals with accessible Pylon/Stack/Spread/Core cues;
- no release-critical warnings or stale state.

Exit: First Severance satisfies its Definition of Done. Only here may it be called playable.

## Milestone 6 — Encounter expansion decisions

Re-evaluate deferred mechanics only from playtest needs:

- Part Break/route choice;
- Targeted Line/Bait;
- Personal Effigies;
- Split Reality;
- Last Stand;
- more complex Boss forms/attacks.

Each promoted item needs its own spec, authority/replication/cleanup model, and test evidence. It is valid to reject all of them.

## Milestone 7 — Presentation

- production sprites and animations;
- primitive trails/shaders and reduced-VFX mode;
- warning typography, localization, accessibility;
- custom SFX, score, transitions, and rights records.

Exit: presentation never obscures gameplay state and every asset has provenance.

## Milestone 8 — Rewards and addon release QA

- class-neutral viable reward set and progression recipes;
- lore/trophy/relic/vanity as approved;
- multiplayer and Dedicated Server soak;
- Windows x64 release matrix plus a pinned macOS client/build smoke and one mixed supported-platform topology when hardware is available;
- compatibility/release matrix, packaging, attribution, notes.

Exit: the Calamity-addon release candidate builds reproducibly and passes the mandatory matrix.

## Milestone 9 — Standalone preparation and migration

- inventory all Calamity progression/class/item/recipe/balance coupling;
- introduce project-owned ports and dependency-free consumers;
- design original progression/materials/equipment/World content;
- decide optional Calamity coexistence and packaging;
- remove the hard dependency only after a separate Standalone build/load/multiplayer/release gate.

Exit: the portable core is independent and the remaining original-content migration is fully enumerated. This milestone alone does not declare the Mod standalone.

## Commit discipline

- one concern per commit where practical;
- version/compatibility changes separate from encounter behavior;
- rename separate from plan simplification, and both separate from first live activation;
- documentation/ADR and tests accompany their owning change;
- generated/local assets, binaries, logs, and credentials never enter commits.
