---
doc_id: decision.roster-health-core-salvos
document_type: adr
status: accepted
owners:
  - engineering
  - networking
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.roster_health_core_salvos
aliases:
  - ADR-0018
related_code:
  - Content/Encounters/FirstSeverance/FirstSeverancePartyScaling.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceGridVolley.cs
  - Content/Encounters/FirstSeverance/Actors
related_docs:
  - decision.boss-stages-lattice
  - encounter.first-severance.spec
  - project.status
---

# ADR-0018: Frozen-roster health and bounded Core salvos

## Decision

Extend ADR-0017's grid descriptor and supersede its v11 wire format with **protocol v12**. Retain the feature-owned phase plan, exact-Fight authority and existing packet IDs/requests. One constructor-validated, immutable 0–4 collection adds locked Core laser directions to a grid volley. Origin, dimensions and lifetime derive from existing shared feature state/constants; clients cannot choose targets or submit hits. The authority creates directions from its connected Alive roster at warning start. Grid and Core rays share the same per-volley hit ledger, which clears on volley/window/stage exit and cleanup. No Common dependency on the feature, spawned projectile population or save migration is introduced.

The combat runtime freezes a typed HP table from the accepted roster. All actor spawns, loop thresholds and passive damage-window pools consume that one value; survivor count never rescales HP. A five-byte ModNPC extra-AI record (roster count and current life) is validated completely before assigning client NPC maximum/current health. Including current life avoids interpreting an implicit full-health SyncNPC with the old local default maximum. This is server-to-client actor replication, not a client health command. Pinned API observations are in [the research note](../research/ENERGY_AUDIO_SKY_APIS.md); exact values/calibration belong to [the encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md).

Eclosion and the terminal Victory rig are disposable client presentation, not new lifecycle states. Only an accepted matching Victory starts the retained ending; attack actors, confinement and flight end immediately. Cancel/failure/lease expiry cannot invent Victory, and a new Fight or unload clears the residue. Audio/graphics remain absent on Dedicated Server. Existing shake/Reduced Effects settings apply; no persistent UI/control flag is added.

## Verification boundary

Check scaled threshold/geometry/immutability, strict wire bounds and actual package compilation once for this changed slice. Human visual/audio acceptance and live multiplayer NPC HP synchronization require the user's focused playtest; pure curves and a codec round trip are not that evidence. [Status](../STATUS.md) owns results and pending work.
