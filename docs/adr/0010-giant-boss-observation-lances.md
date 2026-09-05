---
doc_id: decision.giant-boss-observation-lances
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.observation_lance_snapshot
aliases:
  - ADR-0010
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceLance.cs
  - Common/Networking/Protocol/EncounterProtocol.cs
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.visual
  - project.network-architecture
---

# ADR-0010: Giant original Boss pass and bounded observation lances

- Status: Accepted for development experiment
- Date: 2026-09-06
- Decider: Minamium (explicit requests for giant Boss/VFX, large Spread and repeated predicted beams)
- Extends: [ADR-0009](0009-development-combat-experiment.md)
- Supersedes: only the small-placeholder/no-beam scope of [ADR-0007](0007-first-severance-vertical-slice.md) for this development pass

## Decision

Implement original giant Null Cantor art with client-only animated restraints, seals, atmosphere and readable damage states. Retain exactly one Boss NPC/life pool; giant presentation is not multipart collision. Wrath of the Gods is the user's spectacle benchmark, not a source of copied assets/code. Film-like energy-beam intensity is a broad reference, not an instruction to reproduce a film's designs or recordings. Release/licensing and Calamity dependencies do not change.

The First Severance runtime owns a bounded observation-lance volley during Pylon/Exposure. It locks server-observed Alive target positions at warning start, samples finite ray rectangles, hits each participant once per volley and uses the existing experimental HP/Down adapter. There are at most two rays and four ledger entries. This logical hazard allocates no NPC/Projectile slots or terrain resources. Phase exit and the registered runtime cleanup discard all assignment/ledger state.

Protocol version becomes 3. Append an optional serial/start-tick/one-or-two-ray DTO to combat snapshots. Existing numeric packet IDs and client request layouts remain unchanged; old protocol peers reject rather than misparse the extension. Full snapshots repair the read-only visual state. The client does not calculate authoritative aim or damage. All rays have validated finite origins and normalized directions; counts are bounded before allocation. Timing/geometry tuning is version-matched code.

New visual/attack numbers are reversible prototype choices in the encounter spec. Ordinary lethal hooks, Barrier/rejoin integration, rewards, release art and the broad production matrix are not implicitly authorized or completed. The requested iteration uses focused tests/build and a user-operated two-player playtest.

## Consequences

Both host and friend must reload `0.2.0`. Aimed telegraphs require immediate snapshot publication and live latency/readability checking. The large Boss has a conspicuously framed smaller hitbox; this avoids visual mass becoming an invisible contact hazard. Reduced decoration never removes mechanic information. Dedicated Server paths remain graphics/audio-free.
