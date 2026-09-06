---
doc_id: decision.untimed-recovery-simultaneous-prism
document_type: adr
status: accepted
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-07
source_of_truth_for:
  - architecture.untimed_raid_recovery
  - architecture.simultaneous_prism_replication
aliases:
  - ADR-0021
related_code:
  - Common/Raids/Revive
  - Content/Encounters/FirstSeverance
related_docs:
  - encounter.first-severance.revive
  - project.network-architecture
  - project.status
---

# ADR-0021: Untimed Down and simultaneous bounded Prism assignments

- Decision: explicit user request, 2026-09-07.
- Supersedes: only the 30-second Down expiry retained by [ADR-0011](0011-instant-revival-and-recipient-lockout.md) for active First Severance, and the single-focus Prism assignment in [ADR-0012](0012-pattern-sequences-and-defeat-death.md).

## Recovery

First Severance has only Alive and Downed participant combat states. Downed does not expire into Eliminated. Recipient lockout remains 3,600 ticks after a successful instant revival; a second Down retains that deadline, and an Alive ally can rescue the recipient at or after equality even after a long wait. All participants Downed still causes immediate Defeat on the authority commit. There is no self-revival, token or item consumption.

The feature chooses reusable settings with `DownedTimeoutTicks = 0`. The generic service interprets zero as no deadline, projects zero in `DownedDeadlineTick`, and skips expiry checks for that sentinel. It contains no FirstSeverance branch. Legacy timed/channel/token configurations, enum/event numeric IDs and their tests remain available; they do not govern active First Severance. Its combat projection rejects Eliminated. Ordinary death/disconnect still aborts this experiment before generic reconnect timeout; no production rejoin/death hook is added.

Authority, stable request batches, exact-Fight cleanup and recipient deadline ownership remain unchanged. A client cannot revive, erase lockout, choose a deadline or preserve Down into a new encounter. HUD labels no longer suggest that Down has an expiry.

## Simultaneous Prism and protocol16

Each of eight Phase-I Prism steps locks one predicted ray for every currently standing, connected roster member. The exact-Fight runtime samples all targets on the same tick. The existing immutable volley carries one serial/start tick and 1–4 world rays; TargetSlot is a representative pose focus only, not a restriction to one targeted participant. Body charges/Stillness keep their existing shape and target rules. Intersections share one per-player hit cap per step.

Protocol16 retains all field layouts, packet IDs and client request shapes, but extends Prism ray count from one to at most the bounded roster count (four), and gives zero Down deadline its new meaning. Unknown/invalid states, ray counts, nonfinite geometry, phase/deadline mismatches and rays exceeding roster size are rejected before replica application. Both peers require the same version because flood growth, two-turn blade movement and accelerating Final geometry are derived from shared code.

All new timing/geometry remains feature-local under [ADR-0019](0019-phase-scores-and-terminal-survival.md). No world entities, persistent state, global coordinator switch, music-clock authority or dependency is introduced. The user owns live audiovisual and Host & Play confirmation; [Status](../STATUS.md) records checks, not this decision.
