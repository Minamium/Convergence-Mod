---
doc_id: project.milestones
document_type: plan
status: accepted
owners:
  - project
last_reviewed: 2026-10-04
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
  - project.brief
  - project.status
  - policy.release-process
---

# Milestones

This file holds the project-level gates and their order. [Status](STATUS.md) owns what is implemented and verified, each feature spec owns its behavior, and each feature plan owns that feature's own work order. The [Project Brief](PROJECT_BRIEF.md) owns the product direction and the encounter roster.

The September 2026 list (Milestones 0–9, written around the Doll Raid's first slice) is retired. Its foundation items are done and the rest is folded into the gates below; the old text stays in Git history.

## Where the project is (2026-10-04, 0.3.92)

- **Foundation, done:** modular source boundaries, server-authoritative encounters with definition-routed transport, the shared Foundation Core pedestal and 160×70-tile field, Ready, native Down with instant revival, the Windows build with Build + Reload, and the Dedicated Server/two-client baseline. Still open from it: the source/asset license decision.
- **Five encounters in development, Stage A (Calamity addon):** every encounter in the [roster](PROJECT_BRIEF.md#encounter-roster) can be started and fought to an ending in a development build. Balance is provisional, and Scarlet, Cathedral and Ebon still cap every incoming hit at 1 for rehearsal (`DebugOneDamagePlaytest`).
- **Current focus, since 2026-10-01:** presentation (motion, VFX, sound effects, music handling) and rewards for all five encounters in parallel, one owner-approved change per PR.
- **Open risk:** owner playtests lag behind merges. [Next change](STATUS.md#next-change) lists many owner checks still `not_run`, several of them for builds that later changes already replaced.
- **Public packages:** the latest release notes are for [0.3.1](releases/0.3.1.md); main is far ahead of them.

## Gates ahead

> **Proposed order, not yet chosen by the owner.** The owner picks the next main goal. Until then each thread finishes the presentation work the owner already approved.

1. **Playtest catch-up.** The owner plays the pending checks in Next change, newest first per encounter. Each finding becomes a fix or an accepted note, and superseded checks are dropped. Exit: Next change holds at most the latest unplayed change per encounter.
2. **Encounter acceptance (per encounter).** Owner-approved presentation and rewards, real damage with the one-damage rehearsal turned off, and a balance pass with representative Calamity endgame loadouts, accepted by the owner in Single Player and Host & Play. Exit: the encounter's spec names the accepted build.
3. **Multiplayer acceptance.** Host & Play and Dedicated Server with 2–4 players and at the admission limit; latency and loss, disconnect and rejoin, host and non-host, cleanup and retry, as the [Release Process](RELEASE_PROCESS.md) requires.
4. **Release candidate (Stage A addon).** The release matrix, a rights recheck of every third-party asset, packaging, release notes and a Workshop/GitHub update. An encounter that has not passed gates 2–3 ships only if the notes mark it experimental.
5. **Progression.** Normal recipes for the setup items (Foundation Core, Theater Doll and Resuscitation Kit have none), progression gates after Exo Mechs and Supreme Calamitas, and recipes that connect the rewards.
6. **New content.** More Raids and Bosses, world content and utilities, each with its own spec and a roster entry.
7. **Calamity independence (Stage B, then C).** Per [ADR-0006](adr/0006-staged-calamity-independence.md): project-owned progression, class and balance ports first; the hard dependency is removed only after its own build/load/multiplayer/release gate.

Gates 1–3 can run per encounter and in parallel across encounters.

## Keeping this current

Update this file in the same change when a gate is passed or redefined, or when the owner chooses the next goal. A feature's own step order stays in its plan.

## Commit discipline

- one concern per commit where practical;
- version/compatibility changes separate from encounter behavior;
- rename separate from plan simplification, and both separate from first live activation;
- documentation/ADR and tests accompany their owning change;
- generated/local assets, binaries, logs, and credentials never enter commits.
