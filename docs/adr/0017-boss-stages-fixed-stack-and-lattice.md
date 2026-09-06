---
doc_id: decision.boss-stages-lattice
document_type: adr
status: accepted
owners:
  - engineering
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.boss_stages_lattice
aliases:
  - ADR-0017
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceBossPhases.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceGridVolley.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceStageVisuals.cs
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.visual
  - project.status
---

# ADR-0017: Feature-owned Boss stages, fixed Stack and bounded lattice

## Decision

Extension notice: [ADR-0018](0018-roster-health-and-core-salvos.md) supersedes this record's v11 grid wire format with v12, and adds frozen-roster actor-health replication. The phase/lifecycle decision below is retained as the original decision, not silently rewritten.

The user expands the one-loop slice into multiple Boss phases. This supersedes ADR-0007's single-cycle-only scope, the player-carried Stack assignment of ADR-0009, and only the 320x140 dimensions of ADR-0016. The one-NPC health pool, ground-center semantics, physical containment/flight capability, authority and exact-Fight recovery are retained. The [feature spec](../encounters/first-severance/ENCOUNTER_SPEC.md) owns all current tuning.

An ordered immutable `FirstSeveranceBossPhasePlan` selects the phase threshold, transition duration and active module. The existing six-substate plan remains the Phase-I cycle; `PhaseTransition` and `Lattice` append substate byte values 7 and 8. They do not alter the generic encounter lifecycle, terminal descriptor schema or coordinator. Future phases require another feature definition/module and its bounded presentation projection, not switches in Common. There is no claimed arbitrary-data plug-in phase editor.

The owning server/SP loop caps accepted Phase-I damage at the next threshold. `ModNPC.CheckDead` prevents the exact owned NPC from disappearing before that threshold can be observed; an accepted client stage allows the corresponding local death-suppression mirror but never chooses a transition. The runtime then shields the NPC, clears old attacks and enters the transition at one authority tick. No new NPC life pool, heal-on-phase, general damage hook or persistence record is introduced. Version-pinned API observations and limits are in [the API note](../research/ENERGY_AUDIO_SKY_APIS.md).

Protocol **v11** replaces Stack target slot with two finite world coordinates and appends Boss phase, phase-start tick and one optional grid descriptor (serial, start tick, four-value pattern). One shared geometry constructor expands the descriptor into at most 32 rays inside the common field. Both server damage and client exact danger rails use those rays; no client damage/attendance report, spawned grid projectile collection or audio-driven clock exists. Existing packet IDs, request fields, full-parse validation and generic tombstones remain. Mixed v10/v11 peers must update together.

Grid assignment, serial scheduling, once-per-participant hit ledger and passive diagnostics belong to the exact-Fight combat runtime. Substate/window exit and idempotent cleanup clear them. Clients own only smooth shell/fragments, short harmless beam tails, finite sound handles, an opaque exterior mask and a conditional camera/HUD layer. A fresh Fight, accepted cleanup, world unload or local lease expiry removes them. Graphics/textures and music/sound are never initialized by Dedicated Server. Camera/HUD suppression writes no persistent control/UI flag; there is no extra saved field, terrain mutation, dependency or mod-list change.

## Evidence and remaining work

Threshold/overkill, deadline, grid geometry, fixed-anchor and packet bounds can be checked without a game session. Those checks do not prove multiplayer NPC death suppression, camera/zoom alignment, perceived animation or live audio balance. [Status](../STATUS.md) records the actual build checks and the one user-owned focused smoke; no release or broad compatibility claim follows from this ADR.
