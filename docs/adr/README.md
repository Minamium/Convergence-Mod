---
doc_id: decisions.adr-index
document_type: index
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-05
source_of_truth_for:
  - architecture.adr_index
aliases:
  - ADR index
  - architecture decisions
related_code: []
related_docs:
  - project.decisions
  - project.status
---

# Architecture Decision Records

ADRs record decisions that are expensive to reverse: authority, dependency direction, protocol compatibility, persistence, external dependencies, release safety, asset rights, and accepted scope boundaries that invalidate an existing implementation plan.

| ADR | Status | Decision |
|---|---|---|
| [0001](0001-modular-monolith.md) | Accepted | Modular monolith with feature modules |
| [0002](0002-server-authoritative-encounters.md) | Accepted | Server-authoritative encounter state |
| [0003](0003-in-world-logical-arena.md) | Accepted | Normal World with logical Barrier |
| [0004](0004-calamity-compatibility-boundary.md) | Accepted; version floor superseded by 0008 | Isolated Calamity compatibility adapter |
| [0005](0005-server-authoritative-downed-revive.md) | Accepted; adapter gated | Server-authoritative Raid Downed/Revive domain |
| [0006](0006-staged-calamity-independence.md) | Accepted | Staged path from Calamity addon to Standalone Mod |
| [0007](0007-first-severance-vertical-slice.md) | Accepted | First Severance name, simple repeated loop/visual, and Revive in the first playable slice |
| [0008](0008-confirmed-2026-07-runtime-baseline.md) | Accepted | Windows-verified 2026.07 runtime baseline and Calamity 2.2.4 floor |
| [0009](0009-development-combat-experiment.md) | Accepted for development experiment | Ready-to-combat experiment, Raid-owned HP damage and recovery before production gates |
| [0010](0010-giant-boss-observation-lances.md) | Accepted for development experiment | Giant original Boss/VFX and bounded aimed lances; protocol v3; extends 0009 and the visual/attack scope of 0007 |

Accepted ADRs are not rewritten to hide later changes. Add a new ADR and mark the old record superseded. Current implementation status remains in [`../STATUS.md`](../STATUS.md), not in this index.
