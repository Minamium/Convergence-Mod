---
doc_id: decisions.adr-index
document_type: index
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-13
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
| [0005](0005-server-authoritative-downed-revive.md) | Accepted foundation; First Severance channel/token policy superseded by 0011; active Down expiry superseded by 0021 | Server-authoritative Raid Downed/Revive domain; ordinary lethal-hook adapter remains gated |
| [0006](0006-staged-calamity-independence.md) | Accepted | Staged path from Calamity addon to Standalone Mod |
| [0007](0007-first-severance-vertical-slice.md) | Accepted; visual/single-cycle scope extended by 0010, 0017 and 0019 | First Severance name, simple repeated loop/visual, and Revive in the first playable slice |
| [0008](0008-confirmed-2026-07-runtime-baseline.md) | Accepted | Windows-verified 2026.07 runtime baseline and Calamity 2.2.4 floor |
| [0009](0009-development-combat-experiment.md) | Accepted; recovery/Defeat/admission extended by 0011, 0012, 0020 and 0021 | Ready-to-combat experiment, Raid-owned HP damage and recovery before production gates |
| [0010](0010-giant-boss-observation-lances.md) | Accepted for development experiment | Giant original Boss/VFX and bounded aimed lances; protocol v3; extends 0009 and the visual/attack scope of 0007 |
| [0011](0011-instant-revival-and-recipient-lockout.md) | Accepted; Down expiry superseded by 0021 | Instant reusable revival and recipient-only lockout; supersedes active First Severance channel/token policy; protocol v4 |
| [0012](0012-pattern-sequences-and-defeat-death.md) | Accepted; Prism targeting extended by 0021 | Independent-origin spatial sequences and terminal-ordered normal participant death; protocol v5; supersedes earlier Boss-only attack and nonlethal Defeat cleanup choices |
| [0013](0013-energy-charge-and-client-feedback.md) | Accepted; live-steering/audio/art choices superseded by 0014 | Exact-Fight sampled energy motion and client-only feedback; protocol v6 |
| [0014](0014-deliberate-anticipation-and-solemn-presentation.md) | Accepted for development experiment | Harmless prelaunch lock, deliberate anticipation, solemn high-resolution art/original score; protocol v7 |
| [0015](0015-console-only-single-pull-assist.md) | Accepted for development experiment | Dedicated-console-only, exact-Fight auxiliary invulnerability for two real local clients; protocol v8 |
| [0016](0016-ground-containment-and-continuous-emission.md) | Accepted; field dimensions superseded by 0017 | Ground-based foundation, participant confinement/infinite flight and continuous emission; protocol v10 |
| [0017](0017-boss-stages-fixed-stack-and-lattice.md) | Accepted; wire extended by 0018; immediate exits/grid-only score superseded by 0019 | Feature-local phase plan, protected half-HP rupture, fixed Stack, reduced field and bounded grid |
| [0018](0018-roster-health-and-core-salvos.md) | Accepted; HP-zero victory superseded by 0019; result HUD extended by 0020 | Frozen-roster actor health, bounded Core salvos sharing the grid hit ledger, disposable Victory presentation; protocol v12 |
| [0019](0019-phase-scores-and-terminal-survival.md) | Accepted; supersedes immediate transitions/death and grid-only cycling in 0017/0018 | Ordered action scores, stage HP floors, remote third phase and HP-zero Final survival; protocol v13 |
| [0020](0020-development-solo-admission-and-terminal-hud.md) | Admission gate superseded by0025; terminal presentation retained | Historical build-gated solo, normal all-Down defeat, bounded success/failure HUD cinematics; protocol v14 |

| [0021](0021-untimed-recovery-and-simultaneous-prism.md) | Accepted for development; supersedes active timed Down and single-focus Prism | Untimed recoverable Down and bounded simultaneous Prism; protocol v16 |

| [0022](0022-definition-routed-encounter-transport.md) | Accepted; replaces feature-global packet registration/outbox consumption | Definition-routed common transport, shared operation IDs and generic snapshot repair; protocol v17 |

Accepted ADRs are not rewritten to hide later changes. Add a new ADR and mark the old record superseded. Current implementation status remains in [`../STATUS.md`](../STATUS.md), not in this index.

| [0023](0023-ghost-samurai-native-wave-damage.md) | Accepted; narrowly supersedes0002 for Ghost Samurai SlashWave player hits | Native local-player immunity/dodge hooks; server-owned spawning, geometry, schedule and cleanup; protocol36 |
| [0024](0024-native-raid-hurt-and-downed.md) | Accepted for development; partially supersedes0002/0005/0009 for Doll HP calculation/reporting | Native receiving-owner Hurt with health floor; authority-owned hit intents, Down/revival and terminal settlement; protocol39 |
| [0025](0025-public-solo-admission.md) | Accepted; replaces0020's build/release admission gate | Multiplayer recommended, solo always admitted; actual package checks for GUI/native/public parity |
| [0026](0026-crimson-score-and-native-projectiles.md) | Accepted for the Crimson prototype only | Server-owned score/Ready, scoped native hostile Projectile hits, sample-loop client audio and local recording boundary; protocol42 |

| [0027](0027-oboro-authoritative-weapon.md) | Accepted for Oboro | Session-independent server-owned weapon swings, per-wielder wounds and bounded presentation transport |

| [0028](0028-azure-cathedral-native-actors.md) | Accepted for Azure Cathedral only | Separate shared-pedestal worm/girl runtime, bounded native actor/hazard damage and exact-Fight cleanup; protocol58 |
