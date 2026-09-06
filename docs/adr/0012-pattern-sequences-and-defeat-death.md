---
doc_id: decision.pattern-sequences-defeat-death
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-06
source_of_truth_for:
  - architecture.pattern_sequences_defeat_death
aliases:
  - ADR-0012
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceAttackPatterns.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceClientStateSystem.cs
related_docs:
  - encounter.first-severance.spec
  - project.network-architecture
  - project.status
---

# ADR-0012: Bounded spatial sequences and ordinary death on Raid Defeat

Decision: explicit user request and two supplied attack drawings, 2026-09-06. Extends ADR-0010's attack descriptor; supersedes its Boss-only origins and the nonlethal Defeat cleanup choice of ADR-0009. ADR-0011's reusable instant revival remains intact.

## Decision

The exact-Fight runtime schedules independent-origin attack sequences and selects Alive targets. One immutable descriptor with at most two rays is shared by server collision and client rendering, including deterministic live sweep motion. Protocol v5 adds bounded kind, step, target and dimensions without new packet IDs. No new world actor ownership or client-selected hit decisions. Phase exit and all terminal cleanup clear the descriptor, hit ledger and sequence state. The encounter spec owns pattern timing and Stack/Spread tuning.

A committed Defeat now means normal Terraria death for every connected frozen-roster participant. The accepted terminal acts as the order; the owning client clears Raid incapacitation/protection and invokes `Player.KillMe` once for the exact preceding Fight/sequence. Normal engine death/respawn and character-difficulty penalties apply. Server cleanup validates each binding and records ordered recipients; owner logs record actual death. A stale terminal, an outsider or a non-Defeat ending cannot request it. The coordinator and generic packet router gain no First Severance switch.

This uses Terraria's inherited cooperative owner-death synchronization, not a hostile-client enforcement protocol. External `PreKill` cancellation is logged, not bypassed. Ordinary gameplay lethal-hit interception and rejoin remain outside this change; actual two-client death/respawn and installed-Mod compatibility must be observed before claiming them verified.

The intro is client-only: a temporary first interface layer draws the designation and stops that frame's later UI drawing. Persistent UI flags, controls and camera zoom are untouched; cancellation/unload naturally restores the normal draw path. Public API evidence is appended to [the focused API note](../research/INSTANT_REVIVAL_CORE_APIS.md); no engine or reference artwork is copied.
